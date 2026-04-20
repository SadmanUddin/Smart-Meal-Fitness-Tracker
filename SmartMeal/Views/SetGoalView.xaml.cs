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
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _goalService = _mainWindow.GoalService;
            _authService = _mainWindow.AuthService;
            Loaded += SetGoalView_Loaded;
        }

        // Pre-populate both fields from the user's existing goal row so they see
        // their current values before making changes.
        private async void SetGoalView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= SetGoalView_Loaded;

            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
                return;

            try
            {
                var existing = await _goalService.GetGoalAsync(userId);
                if (existing == null) return;

                if (existing.CalorieGoal.HasValue)
                    DailyGoalTextBox.Text = existing.CalorieGoal.Value.ToString();

                if (existing.TargetWeightKg.HasValue)
                    TargetWeightTextBox.Text = existing.TargetWeightKg.Value.ToString("F1");
            }
            catch
            {
                // Pre-population failing silently is acceptable; user can still type new values.
            }
        }

        // Fires when the user clicks "Save Goal".
        // Validates calorie input (required), parses optional target weight, then upserts via GoalService.
        private async void SetGoal_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(DailyGoalTextBox.Text, out int calorieGoal) || calorieGoal <= 0)
            {
                MessageBox.Show("Please enter a valid number for calorie goal.");
                return;
            }

            decimal? targetWeightKg = null;
            var targetText = TargetWeightTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(targetText))
            {
                if (!decimal.TryParse(targetText, out decimal tw) || tw <= 0)
                {
                    MessageBox.Show("Please enter a valid target weight in kg, or leave it blank.");
                    return;
                }
                targetWeightKg = tw;
            }

            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
            {
                MessageBox.Show("No user logged in.");
                return;
            }

            try
            {
                await _goalService.UpsertGoalAsync(userId, calorieGoal, targetWeightKg);
                MessageBox.Show("Goal saved successfully!");
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

        private void Recommendations_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new RecommendationsView());
        }

        private async void BackToLog_Click(object sender, RoutedEventArgs e)
        {
            await _authService.SignOutAsync();
            _mainWindow.Navigate(new LoginView());
        }
        //private void Dashboard_Click(object sender, RoutedEventArgs e)
        //{
        //    var mainWindow = (MainWindow)Application.Current.MainWindow;
        //    mainWindow.Navigate(new DashboardView());
        //}
        //private void Activities_Click(object sender, RoutedEventArgs e)
        //{
        //    var mainWindow = (MainWindow)Application.Current.MainWindow;
        //    mainWindow.Navigate(new AddActivityView());
        //}
        //private void AddMeal_Click(object sender, RoutedEventArgs e)
        //{
        //    var mainWindow = (MainWindow)Application.Current.MainWindow;
        //    mainWindow.Navigate(new AddMealView());
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
