using SmartMeal.core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SmartMeal.Views
{
    public partial class RecommendationView : UserControl
    {
        private readonly MealService mealService;
        private readonly ActService activityService;
        private readonly GoalService goalService;
        private readonly RecommendationService aiRecommendationService;

        public RecommendationView()
        {
            InitializeComponent();

            mealService = ((MainWindow)Application.Current.MainWindow).MealService;
            activityService = ((MainWindow)Application.Current.MainWindow).ActService;
            goalService = ((MainWindow)Application.Current.MainWindow).GoalService;
            aiRecommendationService = ((MainWindow)Application.Current.MainWindow).RecommendationService;

            LoadSummary();
        }

        private void LoadSummary()
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;

            if (mainWindow.CurrentUser == null)
            {
                MessageBox.Show("No logged in user found.");
                return;
            }

            var userId = mainWindow.CurrentUser.Id;

            var meals = mealService.GetMealsByUser(userId);
            var activities = activityService.GetActivitiesByUser(userId);
            var goal = goalService.GetGoal(userId);

            int consumed = 0;
            int burned = 0;
            int calorieGoal = 0;

            foreach (var meal in meals)
            {
                consumed += meal.Calories;
            }

            foreach (var activity in activities)
            {
                burned += activity.CaloriesBurned;
            }

            if (goal != null)
            {
                calorieGoal = (int)goal.DailyCalorieGoal;
            }

            int balance = calorieGoal - consumed + burned;

            GoalBlock.Text = calorieGoal.ToString();
            ConsumedBlock.Text = consumed.ToString();
            BurnedBlock.Text = burned.ToString();
            BalanceBlock.Text = balance.ToString();
        }

        private async void GenerateRecommendation_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;

            if (mainWindow.CurrentUser == null)
            {
                RecommendationTextBlock.Text = "No logged in user found.";
                return;
            }

            var userId = mainWindow.CurrentUser.Id;

            var meals = mealService.GetMealsByUser(userId);
            var activities = activityService.GetActivitiesByUser(userId);
            var goal = goalService.GetGoal(userId);

            int consumed = 0;
            int burned = 0;
            int calorieGoal = 0;

            foreach (var meal in meals)
            {
                consumed += meal.Calories;
            }

            foreach (var activity in activities)
            {
                burned += activity.CaloriesBurned;
            }

            if (goal != null)
            {
                calorieGoal = (int)goal.DailyCalorieGoal;
            }

            try
            {
                RecommendationTextBlock.Text = "Generating recommendation...";

                string recommendation = await aiRecommendationService.GetRecommendationAsync(
                    calorieGoal,
                    consumed,
                    burned,
                    meals.Count,
                    activities.Count);

                RecommendationTextBlock.Text = recommendation;
            }
            catch (Exception ex)
            {
                RecommendationTextBlock.Text = "Gemini failed: " + ex.Message;
            }
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Navigate(new DashboardView());
        }

        private void AddMeal_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Navigate(new MealsView());
        }

        private void Activities_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Navigate(new AddActivityView());
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Navigate(new HistoryView());
        }

        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Navigate(new ProfileView());
        }
    }
}
