using System.Windows;
using System.Windows.Controls;
using SmartMeal.Helpers;
using SmartMeal.core.Services;
using SmartMeal.core.Models;

namespace SmartMeal.Views
{
    public partial class DashboardView : UserControl
    {
        //All services are pulled from MainWindow because that's the single source of truth for app state and data access.
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

            // Fetch all foods accessible to this user (public seeded + their own USDA-cached foods)
            // so calorie calculations work for meals logged via the USDA search.
            var foods = await _foodService.GetAccessibleFoodsAsync(userId);
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

            //fixed calories burned from all time to daily (sadman)
            var allActivities = await _activityService.GetActivitiesByUserAsync(userId);
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            int totalCaloriesBurned = 0;
            int todayActivityCount = 0;
            Activity? latestActivity = null;

            foreach (var activity in allActivities)
            {
                if (activity.LoggedAt >= today && activity.LoggedAt < tomorrow)
                {
                    totalCaloriesBurned += activity.CaloriesBurned;
                    todayActivityCount++;

                    latestActivity = activity;
                }
            }

            ActivitiesCountBlock.Text = todayActivityCount.ToString();
            CaloriesBurnedBlock.Text = totalCaloriesBurned.ToString();

            if (latestActivity != null)
            {
                RecentActivitiesTextBlock.Text =
                    $"{latestActivity.Name} - {latestActivity.CaloriesBurned} cal burned";
            }
            else
            {
                RecentActivitiesTextBlock.Text = "No activities added today.";
            }

            // --- Goal & Balance (from Supabase) ---
            // Each user has at most one row in the goals table.
            // If the user has never set a goal, GetGoalAsync returns null and we default to 0.
            var goal = await _goalService.GetGoalAsync(userId);
            decimal dailyGoal = 0m;
            if (goal != null && goal.CalorieGoal.HasValue)
                dailyGoal = goal.CalorieGoal.Value;
            CaloriesGoalBlock.Text = Math.Round(dailyGoal).ToString();

            // Update indicator now that we have the actual goal value.
            UpdateCaloriesIndicator(totalCaloriesConsumed, dailyGoal);

            if (goal?.TargetWeightKg.HasValue == true)
                TargetWeightGoalBlock.Text = $"Target weight: {goal.TargetWeightKg.Value:F1} kg";
            else
                TargetWeightGoalBlock.Text = "";

            // --- BMI ---
            // Uses profile weight and height from CurrentUser (set at registration / updated in ProfileView).
            // BMI = kg / m^2. Shown as "—" when either value is missing.
            var currentUser = _authService.CurrentUser;
            if (currentUser?.WeightKg > 0 && currentUser?.HeightCm > 0)
                  {
                double heightM = (double)currentUser.HeightCm!.Value / 100.0;
                double bmi = (double)currentUser.WeightKg!.Value / (heightM * heightM);
                BmiValueBlock.Text = bmi.ToString("F1");
                (BmiCategoryBlock.Text, BmiValueBlock.Foreground) = bmi switch
                {
                    < 18.5 => ("Underweight", new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(245, 158, 11))),  // amber
                    < 25.0 => ("Normal weight", new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(16, 185, 129))),  // green
                    < 30.0 => ("Overweight", new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(245, 158, 11))),  // amber
                    _      => ("Obese", new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(239, 68, 68)))    // red
                };
            }
            else
            {
                BmiValueBlock.Text = "—";
                BmiCategoryBlock.Text = "Set height & weight in Profile";
            }

            // Balance = goal − consumed + burned
            // Positive: user has calories remaining for the day.
            // Negative: user has exceeded their goal (before exercise credit).
            var balance = dailyGoal - totalCaloriesConsumed + totalCaloriesBurned;
            BalanceBlock.Text = Math.Round(balance).ToString();
        }

        // Updates the progress bar and status label on the Calories Consumed card.
        //
        // Three states:
        //   - No goal set (goal == 0)   → grey bar at 0, neutral label
        //   - Under 80% consumed        → green
        //   - 80–99% consumed           → amber (approaching limit)
        //   - 100%+ consumed            → red (goal reached or exceeded)
        private void UpdateCaloriesIndicator(decimal consumed, decimal goal)
        {
            if (goal <= 0)
            {
                CaloriesProgressBar.Value = 0;
                CaloriesProgressBar.Foreground =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(229, 231, 235));
                CaloriesStatusBlock.Text = "No goal set";
                CaloriesConsumedBlock.Foreground =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(17, 24, 39));
                return;
            }

            double pct = Math.Min((double)(consumed / goal) * 100.0, 100.0);
            CaloriesProgressBar.Value = pct;

            System.Windows.Media.Color color;
            string status;

            if (consumed >= goal)
            {
                color = System.Windows.Media.Color.FromRgb(239, 68, 68);   // red
                var over = (int)Math.Round(consumed - goal);
                status = over == 0 ? "Goal reached!" : $"Over by {over} kcal";
            }
            else if (pct >= 80)
            {
                color = System.Windows.Media.Color.FromRgb(245, 158, 11);  // amber
                status = $"{(int)Math.Round(consumed)} kcal consumed";
            }
            else
            {
                color = System.Windows.Media.Color.FromRgb(16, 185, 129);  // green
                status = $"{(int)Math.Round(consumed)} kcal consumed";
            }

            var brush = new System.Windows.Media.SolidColorBrush(color);
            CaloriesProgressBar.Foreground = brush;
            CaloriesConsumedBlock.Foreground = brush;
            CaloriesStatusBlock.Text = status;
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
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new DashboardView());
        }       
        private void Activities_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new ActivitiesView());
        }


        // Navigate to MealsView to see the full history of all logged meals.

        private void Meals_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new MealsView());
        }
        //private void History_Click(object sender, RoutedEventArgs e)
        //{
        //    var mainWindow = (MainWindow)Application.Current.MainWindow;
        //    mainWindow.Navigate(new HistoryView());
        //}
        //private void Profile_Click(object sender, RoutedEventArgs e)
        //{
        //    var mainWindow = (MainWindow)Application.Current.MainWindow;
        //    mainWindow.Navigate(new ProfileView());
        //}

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

        private void Recommendations_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new RecommendationsView());
        }

    }
}
