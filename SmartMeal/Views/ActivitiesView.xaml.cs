using System.Windows;
using System.Windows.Controls;
using SmartMeal.Helpers;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class ActivitiesView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly ActService _activityService;
        private readonly AuthService _authService;
        private Dictionary<string,int> _weeklyActivityCalories;
        public int TotalBurned { get; set; }
        public int AvgBurned { get; set; }
        public int MaxBurned { get; set; }

        public ActivitiesView()
        {
            InitializeComponent();

            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _activityService = _mainWindow.ActService;
            _authService = _mainWindow.AuthService;

            Loaded += ActivitiesView_Loaded;
        }

        private async void ActivitiesView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ActivitiesView_Loaded;
            await LoadActivitiesAsync();
            await LoadActivityTrendAsync();
        }

        private async Task LoadActivitiesAsync()
        {
            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
            {
                MessageBox.Show("No user logged in.");
                return;
            }

            try
            {
                var activities = await _activityService.GetActivitiesByUserAsync(userId);

                ActivitiesDataGrid.ItemsSource = activities;

                int totalCalories = 0;
                int totalMinutes = 0;

                foreach (var activity in activities)
                {
                    totalCalories += activity.CaloriesBurned;
                    totalMinutes += activity.DurationMinutes;
                }

                //ActivitiesCountBlock.Text = activities.Count.ToString();
                //CaloriesBurnedBlock.Text = totalCalories.ToString();
                //DurationBlock.Text = totalMinutes.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load activities: {ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private async Task LoadActivityTrendAsync()
        {
            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
                return;

            try
            {
                var data = await _activityService.GetLast7DaysBurnedCaloriesAsync(userId);

                ActivityTrendItemsControl.ItemsSource = data
                    .OrderBy(x => x.Key)
                    .Select(x => new ActivityTrendRow
                    {
                        Date = x.Key,
                        Calories = x.Value
                    })
                    .ToList();

                TotalBurned = data.Sum(x => x.Value);
                AvgBurned = data.Count > 0 ? (int)data.Average(x => x.Value) : 0;
                MaxBurned = data.Count > 0 ? data.Max(x => x.Value) : 0;
                this.DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load activity trend: {ex.Message}");
            }
        }

        private async void DeleteActivity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is long activityId)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete this activity?",
                    "Confirm Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _activityService.DeleteActivityAsync(activityId);
                        await LoadActivitiesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Could not delete activity: {ex.Message}",
                            "Delete Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }
        private sealed class ActivityTrendRow
        {
            public string Date { get; set; }
            public int Calories { get; set; }
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
            _mainWindow.Navigate(new ActivitiesView());
        }

        private void AddActivity_Click(object sender, RoutedEventArgs e)
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
    }
}