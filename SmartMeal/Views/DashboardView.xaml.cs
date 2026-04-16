using System.Windows;
using System.Windows.Controls;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly MealService _mealService;
        private readonly FoodService _foodService;
        private readonly AuthService _authService;
        private readonly ActService _activityService;
        private readonly GoalService _goalService;

        public DashboardView()
        {
            InitializeComponent();

            _mainWindow = Application.Current.MainWindow as MainWindow
                ?? throw new InvalidOperationException("Main window is not available.");
            _mealService = _mainWindow.MealService;
            _foodService = _mainWindow.FoodService;
            _authService = _mainWindow.AuthService;
            _activityService = _mainWindow.ActService;
            _goalService = _mainWindow.GoalService;
            Loaded += DashboardView_Loaded;
        }

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

        private async Task LoadDashboardAsync()
        {
            var userId = _authService.CurrentUser?.Id;
            if (string.IsNullOrWhiteSpace(userId))
            {
                MealsCountBlock.Text = "0";
                CaloriesConsumedBlock.Text = "0";
                ActivitiesCountBlock.Text = "0";
                CaloriesBurnedBlock.Text = "0";
                BalanceBlock.Text = "0";
                RecentMealsTextBlock.Text = "No meals logged today.";
                RecentActivitiesTextBlock.Text = "No activities added yet.";
                return;
            }

            var logs = await _mealService.GetTodayLogsAsync(userId);
            MealsCountBlock.Text = logs.Count.ToString();

            var totalCaloriesConsumed = await _mealService.CalculateCaloriesConsumedAsync(logs);

            var roundedConsumedCalories = (int)Math.Round(totalCaloriesConsumed);
            CaloriesConsumedBlock.Text = roundedConsumedCalories.ToString();

            if (logs.Count > 0)
            {
                var latestLog = logs[^1];
                var foods = await _foodService.GetPublicFoodsAsync();
                var foodNameById = foods.ToDictionary(f => f.FoodId, f => f.Name);
                var latestFoodLabel = foodNameById.TryGetValue(latestLog.FoodId, out var foodName)
                    ? foodName
                    : $"Food ID {latestLog.FoodId}";

                RecentMealsTextBlock.Text = $"{latestFoodLabel} — {latestLog.Grams}g";
            }
            else
            {
                RecentMealsTextBlock.Text = "No meals logged today.";
            }

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

            var goal = await _goalService.GetGoalAsync(userId);
            var dailyGoal = goal?.CalorieGoal ?? 0m;
            CaloriesGoalBlock.Text = Math.Round(dailyGoal).ToString();

            var balance = dailyGoal - totalCaloriesConsumed + totalCaloriesBurned;
            BalanceBlock.Text = Math.Round(balance).ToString();
        }

        private void AddMeal_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new AddMealView());
        }

        private void AddActivity_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new AddActivityView());
        }

        private void SetGoal_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new SetGoalView());
        }
        private async void BackToLog_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
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

        private void Meals_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new MealsView());
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Weight history view is not implemented yet.",
                "Coming Soon",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Profile view is not implemented yet.",
                "Coming Soon",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
