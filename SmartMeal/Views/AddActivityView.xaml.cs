// AddActivityView is the form where the user logs a physical activity (e.g. a workout).
//
// The user enters:
//   - Activity name (free text, e.g. "Running", "Cycling")
//   - Calories burned (whole number)
//   - Duration in minutes
//
// Activities are persisted to Supabase (public.activities), scoped by the logged-in user.
// Data remains available across app restarts and user sessions.
//
// On success the user is returned to the Dashboard, where the activity count
// and calories-burned total update from the latest database data.

using System.Windows;
using System.Windows.Controls;
using SmartMeal.Helpers;
using SmartMeal.core.Models;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class AddActivityView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly ActService _activityService; // Supabase-backed activity service
        private readonly AuthService _authService;    // needed to stamp the activity with the user's ID

        public AddActivityView()
        {
            InitializeComponent();
            // No async setup needed — there are no DB calls on load. The form is ready immediately.
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _activityService = _mainWindow.ActService;
            _authService = _mainWindow.AuthService;
        }

        // Fires when the user clicks "Add Activity".
        // Validates all three fields, builds an Activity object, and saves it to Supabase.
        private async void AddActivity_Click(object sender, RoutedEventArgs e)
        {
            // Activity name must not be blank.
            if (string.IsNullOrWhiteSpace(ActivityNameTextBox.Text))
            {
                MessageBox.Show("Please enter an activity name.");
                return;
            }
            // Calories burned must be a non-negative integer (0 is valid — e.g. stretching).
            if (!int.TryParse(CaloriesBurnedTextBox.Text, out int caloriesBurned) || caloriesBurned < 0)
            {
                MessageBox.Show("Please enter a valid number for calories burned.");
                return;
            }
            // Duration must be a positive integer — zero minutes doesn't make sense.
            if (!int.TryParse(DurationTextBox.Text, out int durationMinutes) || durationMinutes <= 0)
            {
                MessageBox.Show("Please enter a valid number for duration.");
                return;
            }

            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
            {
                // No session — shouldn't happen in normal flow but handled defensively.
                MessageBox.Show("No user logged in.");
                return;
            }

            // Build the activity. activity_id is DB-generated (identity), so we do not set it here.
            // LoggedAt is set to UTC now for consistent ordering.
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
                // Insert the row into public.activities via the ActService.
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

        // Discard the form and return to the Dashboard without adding an activity.
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new DashboardView());
        }

        // Sidebar navigation — consistent links across all views.
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new DashboardView());
        }

        private void Meals_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new MealsView());
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
    }
}
