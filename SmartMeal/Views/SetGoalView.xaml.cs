using System.Windows;
using System.Windows.Controls;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class SetGoalView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly GoalService _goalService;
        private readonly AuthService _authService;

        public SetGoalView()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _goalService = _mainWindow.GoalService;
            _authService = _mainWindow.AuthService;
        }

        private async void SetGoal_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(DailyGoalTextBox.Text, out int calorieGoal) || calorieGoal <= 0)
            {
                MessageBox.Show("Please enter a valid number for calorie goal.");
                return;
            }

            var userId = _authService.CurrentUser?.Id;
            if (string.IsNullOrWhiteSpace(userId))
            {
                MessageBox.Show("No user logged in.");
                return;
            }

            try
            {
                await _goalService.UpsertGoalAsync(userId, calorieGoal);
                MessageBox.Show("Goal set successfully!");
                _mainWindow.Navigate(new DashboardView());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not save goal: {ex.Message}",
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
    }
}
