using System.Windows;
using System.Windows.Controls;
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
        private List<FoodItem> _foods = new();
        private List<MealType> _mealTypes = new();

        public AddMealView()
        {
            InitializeComponent();

            _mainWindow = Application.Current.MainWindow as MainWindow
                ?? throw new InvalidOperationException("Main window is not available.");
            _mealService = _mainWindow.MealService;
            _foodService = _mainWindow.FoodService;
            _authService = _mainWindow.AuthService;
            Loaded += AddMealView_Loaded;
        }

        private async void AddMealView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= AddMealView_Loaded;

            try
            {
                await LoadDropdownsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load meal options: {ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _mainWindow.Navigate(new DashboardView());
            }
        }

        private async Task LoadDropdownsAsync()
        {
            _foods = await _foodService.GetPublicFoodsAsync();
            FoodComboBox.ItemsSource = _foods;
            FoodComboBox.DisplayMemberPath = "Name";

            _mealTypes = await _foodService.GetMealTypesAsync();
            MealTypeComboBox.ItemsSource = _mealTypes;
            MealTypeComboBox.DisplayMemberPath = "Name";
        }

        private async void AddMeal_Click(object sender, RoutedEventArgs e)
        {
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
            if (!decimal.TryParse(GramsTextBox.Text, out decimal grams) || grams <= 0)
            {
                MessageBox.Show("Please enter a valid amount of grams.");
                return;
            }
            var userId = _authService.CurrentUser?.Id;
            if (userId == null)
            {
                MessageBox.Show("Session expired. Please log in again.");
                _mainWindow.Navigate(new LoginView());
                return;
            }

            try
            {
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

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new DashboardView());
        }

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
