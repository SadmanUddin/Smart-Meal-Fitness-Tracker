// AddMealView is the form where the user logs a new meal entry.
//
// The user:
//   1. Types a food name into the search box — USDA FoodData Central is queried
//      after a 400ms debounce, returning up to 20 matching foods.
//   2. Clicks a result to select it (shown as a chip below the search box).
//   3. Enters how many grams they ate.
//   4. Picks a meal type (Breakfast, Lunch, Dinner, Snack).
//
// On submit, the selected USDA food is upserted into food_items (insert if new,
// reuse if already cached) and a row is inserted into meal_logs.
// On success, the user is returned to the Dashboard where the new meal appears.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SmartMeal.Helpers;
using SmartMeal.core.Models;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class AddMealView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly MealService _mealService;
        private readonly FoodService _foodService;
        private readonly FoodSearchService _foodSearchService;
        private readonly AuthService _authService;

        private List<MealType> _mealTypes = new();

        // The food the user picked from the search results.
        private FoodSearchResult? _selectedFood;
        private bool _isApplyingSelection;

        // Debounce timer — fires the API call 400ms after the user stops typing.
        private readonly DispatcherTimer _searchDebounce;

        public AddMealView()
        {
            InitializeComponent();

            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
                throw new InvalidOperationException("Main window is not available.");

            _mainWindow = mainWindow;
            _mealService = _mainWindow.MealService;
            _foodService = _mainWindow.FoodService;
            _foodSearchService = _mainWindow.FoodSearchService;
            _authService = _mainWindow.AuthService;

            _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _searchDebounce.Tick += SearchDebounce_Tick;

            Loaded += AddMealView_Loaded;
        }

        private async void AddMealView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= AddMealView_Loaded;
            try
            {
                await LoadMealTypesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load meal types: {ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _mainWindow.Navigate(new DashboardView());
            }
        }

        private async Task LoadMealTypesAsync()
        {
            _mealTypes = await _foodService.GetMealTypesAsync();
            MealTypeComboBox.ItemsSource = _mealTypes;
            MealTypeComboBox.DisplayMemberPath = "Name";
        }

        // --- Food search ---

        // Fires on every keystroke. Resets selection, stops any pending search,
        // and restarts the debounce timer if the query is long enough.
        private void FoodSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isApplyingSelection)
                return;

            _selectedFood = null;
            SelectedFoodBorder.Visibility = Visibility.Collapsed;
            FoodResultsBorder.Visibility = Visibility.Collapsed;
            _searchDebounce.Stop();

            if (FoodSearchTextBox.Text.Trim().Length < 2)
                return;

            SearchingLabel.Visibility = Visibility.Visible;
            _searchDebounce.Start();
        }

        private async void SearchDebounce_Tick(object? sender, EventArgs e)
        {
            _searchDebounce.Stop();
            await RunSearchAsync(FoodSearchTextBox.Text.Trim());
        }

        private async Task RunSearchAsync(string query)
        {
            SearchingLabel.Visibility = Visibility.Collapsed;
            if (string.IsNullOrWhiteSpace(query)) return;

            try
            {
                var results = await _foodSearchService.SearchAsync(query);
                FoodResultsListBox.ItemsSource = results;
                FoodResultsBorder.Visibility = results.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                FoodResultsBorder.Visibility = Visibility.Collapsed;
                MessageBox.Show(
                    $"Food search failed: {ex.Message}",
                    "Search Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // Fires when the user clicks a result in the list.
        private void FoodResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FoodResultsListBox.SelectedItem is not FoodSearchResult result) return;

            _isApplyingSelection = true;
            try
            {
                _selectedFood = result;
                FoodResultsBorder.Visibility = Visibility.Collapsed;
                FoodSearchTextBox.Text = string.Empty;

                SelectedFoodLabel.Text = $"{result.Name} — {result.CaloriesPer100g:F0} kcal per 100g";
                SelectedFoodBorder.Visibility = Visibility.Visible;
            }
            finally
            {
                _isApplyingSelection = false;
            }

            FoodResultsListBox.SelectedItem = null;
        }

        private void ClearSelectedFood_Click(object sender, RoutedEventArgs e)
        {
            _selectedFood = null;
            SelectedFoodBorder.Visibility = Visibility.Collapsed;
            FoodSearchTextBox.Text = string.Empty;
            FoodSearchTextBox.Focus();
        }

        // --- Save ---

        // Validates all inputs, upserts the USDA food into food_items to get a stable
        // food_id FK, then inserts the meal_log row.
        private async void AddMeal_Click(object sender, RoutedEventArgs e)
        {
            var selectedFood = _selectedFood;
            if (selectedFood == null && FoodResultsListBox.SelectedItem is FoodSearchResult pendingSelection)
                selectedFood = pendingSelection;

            if (selectedFood == null)
            {
                MessageBox.Show("Please search and select a food item.");
                return;
            }
            if (MealTypeComboBox.SelectedItem is not MealType mealType)
            {
                MessageBox.Show("Please select a meal type.");
                return;
            }
            if (!decimal.TryParse(GramsTextBox.Text, out decimal grams) || grams <= 0)
            {
                MessageBox.Show("Please enter a valid amount of grams.");
                return;
            }

            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
            {
                MessageBox.Show("Session expired. Please log in again.");
                _mainWindow.Navigate(new LoginView());
                return;
            }

            try
            {
                // Cache the USDA food in food_items so the FK in meal_logs is valid
                // and calorie calculations on the dashboard continue to work.
                var foodItem = await _foodService.UpsertSearchedFoodAsync(selectedFood, userId);
                await _mealService.AddMealLogAsync(userId, foodItem.FoodId, grams, mealType.MealTypeId);
                MessageBox.Show("Meal logged successfully!");
                _mainWindow.Navigate(new DashboardView());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not save meal: {ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) =>
            _mainWindow.Navigate(new DashboardView());

        private void Dashboard_Click(object sender, RoutedEventArgs e) =>
            _mainWindow.Navigate(new DashboardView());

        private void Meals_Click(object sender, RoutedEventArgs e) =>
            _mainWindow.Navigate(new MealsView());

        private void Activities_Click(object sender, RoutedEventArgs e) =>
            _mainWindow.Navigate(new AddActivityView());

        private void History_Click(object sender, RoutedEventArgs e) =>
            _mainWindow.Navigate(new WeightHistoryView());

        private void Profile_Click(object sender, RoutedEventArgs e) =>
            _mainWindow.Navigate(new ProfileView());
    }
}
