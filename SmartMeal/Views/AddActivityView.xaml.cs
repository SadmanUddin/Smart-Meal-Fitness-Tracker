using System.Windows;
using System.Windows.Controls;
using SmartMeal.core.Models;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class AddActivityView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly ActService _activityService;
        private readonly AuthService _authService;

        public AddActivityView()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _activityService = _mainWindow.ActService;
            _authService = _mainWindow.AuthService;
        }

        private async void AddActivity_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ActivityNameTextBox.Text))
            {
                MessageBox.Show("Please enter an activity name.");
                return;
            }
            if (!int.TryParse(CaloriesBurnedTextBox.Text, out int caloriesBurned) || caloriesBurned < 0)
            {
                MessageBox.Show("Please enter a valid number for calories burned.");
                return;
            }
            if (!int.TryParse(DurationTextBox.Text, out int durationMinutes) || durationMinutes <= 0)
            {
                MessageBox.Show("Please enter a valid number for duration.");
                return;
            }

            var userId = _authService.CurrentUser?.Id;
            if (string.IsNullOrWhiteSpace(userId))
            {
                MessageBox.Show("No user logged in.");
                return;
            }

            var activity = new Activity
            {
                UserId = userId,
                Name = ActivityNameTextBox.Text.Trim(),
                CaloriesBurned = caloriesBurned,
                DurationMinutes = durationMinutes,
                LoggedAt = DateTime.UtcNow
            };

            try
            {
                await _activityService.AddActivityAsync(activity);
                MessageBox.Show("Activity added successfully!");
                _mainWindow.Navigate(new DashboardView());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not save activity: {ex.Message}",
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
