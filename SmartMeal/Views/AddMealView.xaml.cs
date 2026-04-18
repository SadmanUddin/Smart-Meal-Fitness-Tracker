// AddMealView is the form where the user logs a new meal entry.
//
// The user picks:
//   1. A food item from a dropdown (e.g. Chicken Breast, Rice, Banana)
//   2. A meal type (Breakfast, Lunch, Dinner, Snack)
//   3. How many grams they ate
//
// On submit, a row is inserted into the meal_logs table in Supabase.
// On success, the user is returned to the Dashboard where the new meal appears in the stats.
//
// Both dropdowns are populated from the database when the view loads.
// If the DB call fails, the user is sent back to the Dashboard with an error message
// rather than being left on a half-loaded form.

using System.Windows;
using System.Windows.Controls;
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
        private readonly AuthService _authService;

        // Cached dropdown data — loaded once from the DB when the view opens.
        // Stored as fields so AddMeal_Click can read the selected items' IDs.
        private List<FoodItem> _foods = new();
        private List<MealType> _mealTypes = new();

        public AddMealView()
        {
            InitializeComponent();

            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
                throw new InvalidOperationException("Main window is not available.");

            _mainWindow = mainWindow;
            _mealService = _mainWindow.MealService;
            _foodService = _mainWindow.FoodService;
            _authService = _mainWindow.AuthService;

            // Dropdown data requires async DB calls, so defer to the Loaded event.
            Loaded += AddMealView_Loaded;
        }

        // Fires once when the view appears. Immediately unsubscribes to prevent double-loading.
        private async void AddMealView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= AddMealView_Loaded;

            try
            {
                await LoadDropdownsAsync();
            }
            catch (Exception ex)
            {
                // If we can't load foods or meal types, the form is unusable.
                // Show the error and go back to the dashboard instead of leaving the user stuck.
                MessageBox.Show(
                    $"Could not load meal options: {ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _mainWindow.Navigate(new DashboardView());
            }
        }

        // Fetches both dropdown lists from the DB in sequence and binds them to the ComboBoxes.
        // DisplayMemberPath tells WPF which property on each list item to show as the label.
        private async Task LoadDropdownsAsync()
        {
            // GetPublicFoodsAsync returns all active, public food items sorted A–Z.
            // These are the ~44 seeded foods available to all users.
            _foods = await _foodService.GetPublicFoodsAsync();
            FoodComboBox.ItemsSource = _foods;
            FoodComboBox.DisplayMemberPath = "Name";

            // GetMealTypesAsync returns Breakfast, Lunch, Dinner, Snack in display order.
            _mealTypes = await _foodService.GetMealTypesAsync();
            MealTypeComboBox.ItemsSource = _mealTypes;
            MealTypeComboBox.DisplayMemberPath = "Name";
        }

        // Fires when the user clicks "Add Meal".
        // Validates all three inputs, then inserts a row into meal_logs via MealService.
        private async void AddMeal_Click(object sender, RoutedEventArgs e)
        {
            // Pattern-matching cast: "is not FoodItem food" both checks and extracts the selected value.
            // If nothing is selected, SelectedItem is null and this cast fails, showing the message.
            if (FoodComboBox.SelectedItem is not FoodItem food)
            {
                MessageBox.Show("Please select a food item.");
                return;
            }
            if (MealTypeComboBox.SelectedItem is not MealType mealType)
            {
                MessageBox.Show("Please select a meal type.");
                return;
            }
            // decimal.TryParse handles both "150" and "150.5". We reject 0 or negative grams.
            if (!decimal.TryParse(GramsTextBox.Text, out decimal grams) || grams <= 0)
            {
                MessageBox.Show("Please enter a valid amount of grams.");
                return;
            }

            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
            {
                // Session expired (CurrentUser was cleared) — send back to login.
                MessageBox.Show("Session expired. Please log in again.");
                _mainWindow.Navigate(new LoginView());
                return;
            }

            try
            {
                // AddMealLogAsync inserts a row into meal_logs with today's date as log_date.
                // Arguments: the user's Supabase Auth UUID, the selected food's PK,
                // the grams amount, and the selected meal type's PK.
                await _mealService.AddMealLogAsync(userId, food.FoodId, grams, mealType.MealTypeId);
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

        // Discard the form and go back to the Dashboard without saving.
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new DashboardView());
        }

        // Sidebar / bottom navigation links — shared across views for consistent navigation.
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new DashboardView());
        }

        private void Meals_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new MealsView());
        }

        private void Activities_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new AddActivityView());
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

    }
}
