using System.Windows;
using System.Windows.Controls;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class RecommendationsView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly AuthService _authService;
        private readonly GoalService _goalService;
        private readonly GeminiService _geminiService;

        public RecommendationsView()
        {
            InitializeComponent();
            _mainWindow   = (MainWindow)Application.Current.MainWindow;
            _authService  = _mainWindow.AuthService;
            _goalService  = _mainWindow.GoalService;
            _geminiService = _mainWindow.GeminiService;
            Loaded += RecommendationsView_Loaded;
        }

        private async void RecommendationsView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= RecommendationsView_Loaded;
            await LoadSummaryAsync();
        }

        private async Task LoadSummaryAsync()
        {
            var user = _authService.CurrentUser;
            if (user == null) return;

            var goal = await _goalService.GetGoalAsync(user.Id);
            GoalSummaryBlock.Text = goal?.CalorieGoal.HasValue == true
                ? $"{goal.CalorieGoal:F0} kcal"
                : "Not set";

            PreferencesSummaryBlock.Text = string.IsNullOrWhiteSpace(user.FoodPreferences)
                ? "None set"
                : user.FoodPreferences;

            AllergiesSummaryBlock.Text = string.IsNullOrWhiteSpace(user.Allergies)
                ? "None set"
                : user.Allergies;
        }

        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            var user = _authService.CurrentUser;
            if (user == null) return;

            GenerateButton.IsEnabled = false;
            StatusBlock.Text = "Generating your personalised plan...";
            ClearLists();

            try
            {
                var goal = await _goalService.GetGoalAsync(user.Id);

                var plan = await _geminiService.GenerateMealPlanAsync(new MealPlanRequest
                {
                    CalorieGoal     = (int)(goal?.CalorieGoal ?? 2000),
                    Age             = user.Age,
                    Gender          = user.Gender,
                    WeightKg        = user.WeightKg,
                    HeightCm        = user.HeightCm,
                    FoodPreferences = user.FoodPreferences,
                    Allergies       = user.Allergies
                });

                if (plan == null)
                {
                    StatusBlock.Text = "Could not generate a plan. Please try again.";
                    return;
                }

                BreakfastList.ItemsSource = plan.Breakfast;
                LunchList.ItemsSource     = plan.Lunch;
                DinnerList.ItemsSource    = plan.Dinner;
                SnacksList.ItemsSource    = plan.Snacks;
                StatusBlock.Text = string.Empty;
            }
            catch (Exception ex)
            {
                StatusBlock.Text = $"Error: {ex.Message}";
            }
            finally
            {
                GenerateButton.IsEnabled = true;
            }
        }

        private void ClearLists()
        {
            BreakfastList.ItemsSource = null;
            LunchList.ItemsSource     = null;
            DinnerList.ItemsSource    = null;
            SnacksList.ItemsSource    = null;
        }

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

        private async void BackToLog_Click(object sender, RoutedEventArgs e)
        {
            await _authService.SignOutAsync();
            _mainWindow.Navigate(new LoginView());
        }
    }
}
