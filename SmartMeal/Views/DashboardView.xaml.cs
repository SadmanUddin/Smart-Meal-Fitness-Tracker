// DashboardView is the main hub the user lands on after logging in.
// It shows a live summary of four stats:
//   - Meals logged today (count)
//   - Calories consumed today (calculated from grams × per-100g data for each food)
//   - Activities logged by this user (count, persisted in Supabase)
//   - Calories burned from those activities
//
// It also shows:
//   - The user's daily calorie goal (stored in the Supabase goals table)
//   - A "Balance" figure: goal − consumed + burned
//   - A one-line preview of the most recent meal and most recent activity
//
// Navigation buttons: Add Meal, Add Activity, Set Goal, Meals, Logout, History, Profile.

using System.Windows;
using System.Windows.Controls;
using SmartMeal.Helpers;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class DashboardView : UserControl
    {
        // All services are pulled from MainWindow, which is the single shared service host.
        // No service is created here — they all live for the lifetime of the app process.
        private readonly MainWindow _mainWindow;
        private readonly MealService _mealService;
        private readonly FoodService _foodService;
        private readonly AuthService _authService;
        private readonly ActService _activityService;
        private readonly GoalService _goalService;

        public DashboardView()
        {
            InitializeComponent();

            // Cast is safe because this UserControl is always hosted inside MainWindow.
            // The null-coalescing throw gives a clear error instead of a silent NPE later.
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
                throw new InvalidOperationException("Main window is not available.");

            _mainWindow = mainWindow;
            _mealService = _mainWindow.MealService;
            _foodService = _mainWindow.FoodService;
            _authService = _mainWindow.AuthService;
            _activityService = _mainWindow.ActService;
            _goalService = _mainWindow.GoalService;

            // Data loading is async, so we can't call it in the constructor.
            // Subscribing to Loaded here defers it until the control is on screen.
            Loaded += DashboardView_Loaded;
        }

        // Fires once when the view is rendered and attached to the visual tree.
        // Immediately unsubscribes to prevent accidental double-loading if the view
        // is ever detached and re-attached.
        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= DashboardView_Loaded;

            try
            {
                await LoadDashboardAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load dashboard data: {ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Queries three data sources and populates every stat block on the dashboard.
        //
        // Data sources used:
        //   1. meal_logs table (Supabase)  → today's meals for this user
        //   2. foods table (Supabase)      → food names + nutrition data for calorie calculation
        //   3. activities table (Supabase) → activities logged by this user
        //   4. goals table (Supabase)      → this user's daily calorie goal
        private async Task LoadDashboardAsync()
        {
            // Guard: if no user is logged in, zero out every stat and return early.
            // This shouldn't happen in normal flow — LoginView always sets CurrentUser —
            // but it's a safe fallback to avoid showing stale or garbage data.
            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
            {
                MealsCountBlock.Text = "0";
                CaloriesConsumedBlock.Text = "0";
                ActivitiesCountBlock.Text = "0";
                CaloriesBurnedBlock.Text = "0";
                CaloriesGoalBlock.Text = "0";
                BalanceBlock.Text = "0";
                RecentMealsTextBlock.Text = "No meals logged today.";
                RecentActivitiesTextBlock.Text = "No activities added yet.";
                return;
            }

            // --- Meals & Calories Consumed (from Supabase) ---
            // GetTodayLogsAsync returns only meal_log rows where log_date = today, for this user.
            var logs = await _mealService.GetTodayLogsAsync(userId);
            MealsCountBlock.Text = logs.Count.ToString();

            // Fetch foods once and reuse for both calorie math and latest-meal label.
            // This avoids two DB round-trips on every dashboard load.
            var foods = await _foodService.GetPublicFoodsAsync();
            var foodById = foods.ToDictionary(f => f.FoodId);

            decimal totalCaloriesConsumed = 0m;
            foreach (var log in logs)
            {
                if (foodById.TryGetValue(log.FoodId, out var food))
                    totalCaloriesConsumed += (log.Grams / 100m) * food.CaloriesPer100g;
            }
            var roundedConsumedCalories = (int)Math.Round(totalCaloriesConsumed);
            CaloriesConsumedBlock.Text = roundedConsumedCalories.ToString();

            // Show the most recently logged food by name (resolved via a foods lookup dictionary).
            // logs[^1] is C# index-from-end syntax — equivalent to logs[logs.Count - 1].
            if (logs.Count > 0)
            {
                var latestLog = logs[^1];
                string latestFoodLabel;
                if (foodById.TryGetValue(latestLog.FoodId, out var food))
                    latestFoodLabel = food.Name;
                else
                    latestFoodLabel = $"Food ID {latestLog.FoodId}";

                RecentMealsTextBlock.Text = $"{latestFoodLabel} — {latestLog.Grams}g";
            }
            else
            {
                RecentMealsTextBlock.Text = "No meals logged today.";
            }

            // --- Activities & Calories Burned (from Supabase) ---
            // GetActivitiesByUserAsync queries public.activities and filters by userId,
            // so only this user's activities are counted.
            var activities = await _activityService.GetActivitiesByUserAsync(userId);
            ActivitiesCountBlock.Text = activities.Count.ToString();

            var totalCaloriesBurned = activities.Sum(activity => activity.CaloriesBurned);
            CaloriesBurnedBlock.Text = totalCaloriesBurned.ToString();

            if (activities.Count > 0)
            {
                var latestActivity = activities[^1];
                RecentActivitiesTextBlock.Text =
                    $"{latestActivity.Name} - {latestActivity.CaloriesBurned} cal burned";
            }
            else
            {
                RecentActivitiesTextBlock.Text = "No activities added yet.";
            }

            // --- Goal & Balance (from Supabase) ---
            // Each user has at most one row in the goals table.
            // If the user has never set a goal, GetGoalAsync returns null and we default to 0.
            var goal = await _goalService.GetGoalAsync(userId);
            decimal dailyGoal = 0m;
            if (goal != null && goal.CalorieGoal.HasValue)
                dailyGoal = goal.CalorieGoal.Value;
            CaloriesGoalBlock.Text = Math.Round(dailyGoal).ToString();

            if (goal?.TargetWeightKg.HasValue == true)
                TargetWeightGoalBlock.Text = $"Target weight: {goal.TargetWeightKg.Value:F1} kg";
            else
                TargetWeightGoalBlock.Text = "";

            // Balance = goal − consumed + burned
            // Positive: user has calories remaining for the day.
            // Negative: user has exceeded their goal (before exercise credit).
            var balance = dailyGoal - totalCaloriesConsumed + totalCaloriesBurned;
            BalanceBlock.Text = Math.Round(balance).ToString();
        }

        // Navigate to AddMealView to log a new meal entry.
        private void AddMeal_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new AddMealView());
        }

        // Navigate to AddActivityView to log a workout or physical activity.
        private void AddActivity_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new AddActivityView());
        }

        // Navigate to SetGoalView to update the daily calorie target.
        private void SetGoal_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new SetGoalView());
        }

        // Confirms logout, calls Supabase Auth to revoke the session server-side,
        // clears CurrentUser, then navigates back to the login screen.
        private async void BackToLog_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // SignOutAsync revokes the JWT server-side and nulls CurrentUser locally.
                    // After this call, any protected DB query will be rejected by Supabase.
                    await _mainWindow.AuthService.SignOutAsync();
                    _mainWindow.Navigate(new LoginView());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Could not sign out right now: {ex.Message}",
                        "Logout Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        // Navigate to MealsView to see the full history of all logged meals.
        private void Meals_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new MealsView());
        }

        // Navigate to WeightHistoryView to see the weight graph and log a new weigh-in.
        private void History_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new WeightHistoryView());
        }

        // Navigate to ProfileView to edit user details (name, age, height, weight, gender).
        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new ProfileView());
        }

    }
}
