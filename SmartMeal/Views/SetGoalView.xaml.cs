// SetGoalView is the form where the user sets (or updates) their daily calorie target.
//
// The user enters a single number — their calorie goal for the day.
// On submit, GoalService upserts the value into the goals table in Supabase:
//   - If the user already has a goal row → it is UPDATED.
//   - If they have never set a goal → a new row is INSERTED.
//
// This upsert behaviour is safe because the goals table has a UNIQUE constraint on
// user_id, so there can only ever be one goal row per user.
//
// After saving, the user is returned to the Dashboard where the new goal is reflected
// immediately in the "Goal" stat block and the Balance calculation.

using System.Windows;
using System.Windows.Controls;
using SmartMeal.Helpers;
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
            // No async setup needed — the form has a single text field and a save button.
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _goalService = _mainWindow.GoalService;
            _authService = _mainWindow.AuthService;
        }

        // Fires when the user clicks "Set Goal".
        // Validates the input, then upserts the calorie goal via GoalService.
        private async void SetGoal_Click(object sender, RoutedEventArgs e)
        {
            // int.TryParse safely converts the text to an integer without throwing.
            // We reject 0 or negative values — a goal of zero calories makes no sense.
            if (!int.TryParse(DailyGoalTextBox.Text, out int calorieGoal) || calorieGoal <= 0)
            {
                MessageBox.Show("Please enter a valid number for calorie goal.");
                return;
            }

            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
            {
                // Defensive check — shouldn't happen in normal flow since the user must
                // be logged in to reach this view.
                MessageBox.Show("No user logged in.");
                return;
            }

            try
            {
                // UpsertGoalAsync checks whether a goal row already exists for this user.
                // If it does, it updates the CalorieGoal column. If not, it inserts a new row.
                // Either way, the result in the DB is exactly one goal row for this user.
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

        // Discard the form and return to the Dashboard without saving.
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

        private void Activities_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new AddActivityView());
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new WeightHistoryView());
        }

        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new ProfileView());
        }
    }
}
