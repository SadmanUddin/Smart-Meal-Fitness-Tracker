using System.Windows;
using System.Windows.Controls;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class MealsView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly MealService _mealService;
        private readonly FoodService _foodService;
        private readonly AuthService _authService;

        public MealsView()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _mealService = _mainWindow.MealService;
            _foodService = _mainWindow.FoodService;
            _authService = _mainWindow.AuthService;
            Loaded += MealsView_Loaded;
        }

        private async void MealsView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MealsView_Loaded;
            await LoadMealsAsync();
        }

        private async Task LoadMealsAsync()
        {
            var userId = _authService.CurrentUser?.Id;
            if (string.IsNullOrWhiteSpace(userId))
            {
                MessageBox.Show("No user logged in.");
                return;
            }

            try
            {
                var logs = await _mealService.GetAllLogsAsync(userId);

                var foodsTask = _foodService.GetPublicFoodsAsync();
                var mealTypesTask = _foodService.GetMealTypesAsync();
                await Task.WhenAll(foodsTask, mealTypesTask);

                var foodNameById = foodsTask.Result.ToDictionary(f => f.FoodId, f => f.Name);
                var mealTypeNameById = mealTypesTask.Result.ToDictionary(t => t.MealTypeId, t => t.Name);

                var rows = logs.Select(log => new MealViewRow
                {
                    MealLogId = log.MealLogId,
                    FoodName = foodNameById.TryGetValue(log.FoodId, out var foodName)
                        ? foodName
                        : $"Food ID {log.FoodId}",
                    Grams = log.Grams,
                    MealTypeName = mealTypeNameById.TryGetValue(log.MealTypeId, out var mealTypeName)
                        ? mealTypeName
                        : $"Type {log.MealTypeId}",
                    LogDate = log.LogDate
                }).ToList();

                MealsDataGrid.ItemsSource = rows;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load meals: {ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void DeleteMeal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long mealLogId)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete this meal?",
                    "Confirm Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _mealService.DeleteMealLogAsync(mealLogId);
                        await LoadMealsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Could not delete meal: {ex.Message}",
                            "Delete Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new DashboardView());
        }

        private void AddMeal_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new AddMealView());
        }

        private void Activities_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new AddActivityView());
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

        private sealed class MealViewRow
        {
            public long MealLogId { get; set; }
            public string FoodName { get; set; } = string.Empty;
            public decimal Grams { get; set; }
            public string MealTypeName { get; set; } = string.Empty;
            public string LogDate { get; set; } = string.Empty;
        }
    }
}
