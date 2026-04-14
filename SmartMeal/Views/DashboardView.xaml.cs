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
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly MealService mealService;
        private readonly ActService activityService;
        private readonly GoalService goalService;
        private void LoadMeals() //This methods load the meals and calculate the calories...also display recent meals in dashboard...
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            if (mainWindow.CurrentUser == null)
            {
                MessageBox.Show("No user logged in.");
                return;
            }
            var userId = mainWindow.CurrentUser.Id;
            var meals = mealService.GetMealsByUser(userId);
            MealsCountBlock.Text = meals.Count.ToString();
            int totalCalories = 0;
            foreach (var i in meals)
            {
                totalCalories += i.Calories;
            }
            CaloriesConsumedBlock.Text = totalCalories.ToString();
            if (meals.Count > 0)
            {
                var latestMeal = meals[meals.Count - 1];
                RecentMealsTextBlock.Text = $"{latestMeal.Name} - {latestMeal.Calories} cal";

            }
            else
            {
                RecentMealsTextBlock.Text = "No meals added yet.";
            }
        }
        private void LoadActivities() //This method load the activities and calculate the calories burned....also display recent activity in dashboard...
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            if (mainWindow.CurrentUser == null)
            {
                MessageBox.Show("No user logged in.");
                return;
            }
            var userId = mainWindow.CurrentUser.Id;
            var meals = mealService.GetMealsByUser(userId);
            var activities = activityService.GetActivitiesByUser(userId);
            var goal = goalService.GetGoal(userId);

            int totalCaloriesBurned = 0;
            int totalMealCalories = 0;

            foreach (var i in meals)
            {
                totalMealCalories += i.Calories;
            }
            foreach (var i in activities)
            {
                totalCaloriesBurned += i.CaloriesBurned;
            }
            int dailyGoal = 0;
            if (goal != null)
            {
                dailyGoal = goal.DailyCalorieGoal;
            }

            int balance = dailyGoal - totalMealCalories+ totalCaloriesBurned; // Balance calculation
            ActivitiesCountBlock.Text = activities.Count.ToString();
            CaloriesBurnedBlock.Text = totalCaloriesBurned.ToString();
            BalanceBlock.Text = balance.ToString();
            if (activities.Count > 0)
            {
                var latestActivity = activities[activities.Count - 1];
                RecentActivitiesTextBlock.Text = $"{latestActivity.Name} - {latestActivity.CaloriesBurned} cal burned";
            }
            else
            {
                RecentActivitiesTextBlock.Text = "No activities added yet.";
            }

            CaloriesGoalBlock.Text = dailyGoal.ToString(); // displying the daily goals of users
        }
        public void LoadGoal()
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            if (mainWindow.CurrentUser == null)
            {
                MessageBox.Show("No user logged in.");
                return;
            }
            var userId = mainWindow.CurrentUser.Id;
            var goal = goalService.GetGoal(userId);
            int dailyGoal = 0;
            if (goal != null)
            {
                dailyGoal = goal.DailyCalorieGoal;
            }
            CaloriesGoalBlock.Text = dailyGoal.ToString(); // displying the daily goals of users
        }
        public DashboardView() //constructor
        {
            InitializeComponent();
            mealService = ((MainWindow)Application.Current.MainWindow).MealService;
            activityService = ((MainWindow)Application.Current.MainWindow).ActService;
            goalService = ((MainWindow)Application.Current.MainWindow).GoalService;
            LoadMeals();//calling the methond to load the meals when the dashboard
            LoadActivities();//calling the method to load the activities when the dashboard is loaded
            LoadGoal();//calling the method to load the goal when the dashboard is loaded
        }
        private void AddMeal_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new AddMealView());
        }
        private void AddActivity_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new AddActivityView());
        }
        private void SetGoal_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new SetGoalView());
        }
        private void BackToLog_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.CurrentUser = null; // Clear the current user
            mainWindow.Navigate(new LoginView()); // Navigate back to login view
        }
    }
}
