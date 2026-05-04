// MealsView shows the user's full history of logged meals — not just today's.
//
// It fetches all meal_log rows for the current user from Supabase, then enriches them
// by resolving the food ID and meal type ID to human-readable names using lookup
// dictionaries built from the foods and meal_types tables.
//
// The DataGrid is bound to a list of MealViewRow objects — a small private class
// defined at the bottom of this file that holds display-ready strings rather than
// raw integer IDs. This keeps the XAML bindings simple and the grid readable.
//
// Rows can be deleted individually. Deletion removes the row from the DB and
// immediately refreshes the grid.
//
// Navigation buttons: Dashboard, Add Meal, Activities, History, Profile.

using System.Windows;
using System.Windows.Controls;
using SmartMeal.Helpers;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class MealsView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly MealService _mealService;
        private readonly FoodService _foodService; // used to resolve food names and meal type names
        private readonly AuthService _authService;
        private Dictionary<string, int> _weeklyCalories;
        public int TotalCalories { get; set; }
        public int AvgCalories { get; set; }
        public int MaxCalories { get; set; }


        public MealsView()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _mealService = _mainWindow.MealService;
            _foodService = _mainWindow.FoodService;
            _authService = _mainWindow.AuthService;

            // Defer the async DB load to the Loaded event — can't await in a constructor.
            Loaded += MealsView_Loaded;
        }

        // Fires once when the view is rendered. Unsubscribes immediately to prevent
        // accidental double-loading if the control is ever removed and re-added.
        private async void MealsView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MealsView_Loaded;
            await LoadMealsAsync();
            await LoadMealTrendAsync();
        }
        private async Task LoadMealTrendAsync()
        {
            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
                return;

            try
            {
                var data = await _mealService.GetLast7DaysCaloriesAsync(userId);

                MealTrendItemsControl.ItemsSource = data
                    .OrderBy(x => x.Key)
                    .Select(x => new MealTrendRow
                    {
                        Date = x.Key,
                        Calories = x.Value
                    })
                    .ToList();
                TotalCalories = data.Sum(x => x.Value);
                AvgCalories = (int)data.Average(x => x.Value);
                MaxCalories = data.Max(x => x.Value);
                this.DataContext = this; // Update bindings for TotalCalories, AvgCalories, MaxCalories
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load meal trend: {ex.Message}");
            }
        }


        // Fetches all meal logs for the current user, resolves IDs to names,
        // and binds the result to the DataGrid.
        //
        // Three DB calls are made:
        //   1. GetAllLogsAsync      — all meal_log rows for this user (any date)
        //   2. GetPublicFoodsAsync  — all food items (to resolve FoodId → Name)
        //   3. GetMealTypesAsync    — all meal types (to resolve MealTypeId → Name)
        //
        // Calls 2 and 3 are fired in parallel with Task.WhenAll because neither
        // depends on the other, which halves the wait time for those two queries.
        private async Task LoadMealsAsync()
        {
            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
            {
                MessageBox.Show("No user logged in.");
                return;
            }

            try
            {
                var logs = await _mealService.GetAllLogsAsync(userId);

                // Fetch the lookup tables in parallel to avoid sequential round-trips.
                var foodsTask = _foodService.GetAccessibleFoodsAsync(userId);
                var mealTypesTask = _foodService.GetMealTypesAsync();
                await Task.WhenAll(foodsTask, mealTypesTask);

                // Build O(1) lookup dictionaries so the projection below doesn't
                // do a linear search per row.
                var foodNameById = foodsTask.Result.ToDictionary(f => f.FoodId, f => f.Name);
                var mealTypeNameById = mealTypesTask.Result.ToDictionary(t => t.MealTypeId, t => t.Name);

                // Project each raw MealLog into a MealViewRow with display-ready strings.
                // If a food or meal type ID isn't found in the lookup (shouldn't happen with seeded data
                // but is handled gracefully), we fall back to showing the raw ID.
                var rows = logs.Select(log => new MealViewRow
                {
                    MealLogId = log.MealLogId,
                    FoodName = ResolveFoodName(foodNameById, log.FoodId),
                    Grams = log.Grams,
                    MealTypeName = ResolveMealTypeName(mealTypeNameById, log.MealTypeId),
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

        // Fires when the user clicks the Delete button on a row.
        //
        // The Delete button in XAML has Tag="{Binding MealLogId}" so we can identify
        // which row to delete without needing to track the selected row separately.
        // MealLogId is a long (int64) — the auto-incrementing PK from the meal_logs table.
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
                        // Delete the row from the DB, then reload the grid to reflect the change.
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

        // Sidebar navigation — consistent links across all views.
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
            _mainWindow.Navigate(new ActivitiesView());
        }

        // Navigate to WeightHistoryView to see the weight graph and log a new weigh-in.
        private void History_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new WeightHistoryView());
        }

        // Navigate to ProfileView to edit user details.
        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new ProfileView());
        }

        private void Recommendations_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new RecommendationsView());
        }

        private async void BackToLog_Click(object sender, RoutedEventArgs e)
        {
            await _authService.SignOutAsync();
            _mainWindow.Navigate(new LoginView());
        }

        // A flat, display-only record used as the DataGrid row model.
        // We project from MealLog (which has raw integer IDs) into this type
        // so the DataGrid columns can bind directly to human-readable strings
        // without needing converters or multi-binding in XAML.
        private sealed class MealViewRow
        {
            public long MealLogId { get; set; }       // PK — used by the Delete button's Tag binding
            public string FoodName { get; set; } = string.Empty;
            public decimal Grams { get; set; }
            public string MealTypeName { get; set; } = string.Empty;
            public string LogDate { get; set; } = string.Empty; // "yyyy-MM-dd" string from DB
        }

        private static string ResolveFoodName(Dictionary<long, string> foodNameById, long foodId)
        {
            if (foodNameById.TryGetValue(foodId, out var foodName))
                return foodName;

            return $"Food ID {foodId}";
        }

        private static string ResolveMealTypeName(Dictionary<short, string> mealTypeNameById, short mealTypeId)
        {
            if (mealTypeNameById.TryGetValue(mealTypeId, out var mealTypeName))
                return mealTypeName;

            return $"Type {mealTypeId}";
        }

        private sealed class MealTrendRow
        {
            public string Date { get; set; }
            public int Calories { get; set; }
        }
        //private void Activities_Click(object sender, RoutedEventArgs e)
        //{
        //    var mainWindow = (MainWindow)Application.Current.MainWindow;
        //    mainWindow.Navigate(new AddActivityView());
        //}
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
    }
}
