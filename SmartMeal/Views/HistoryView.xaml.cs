using SmartMeal.core.Models;
using SmartMeal.core.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;


namespace SmartMeal.Views
{
    public partial class HistoryView : UserControl
    {
        private readonly MealService mealService;
        private readonly ActService activityService;
        private readonly GoalService goalService;

        public ISeries[] MealSeries { get; set; } = new ISeries[0];
        public Axis[] MealXAxes { get; set; } = new Axis[0];

        public ISeries[] ActivitySeries { get; set; } = new ISeries[0];
        public Axis[] ActivityXAxes { get; set; } = new Axis[0];

        public HistoryView()
        {
            InitializeComponent();

            mealService = ((MainWindow)Application.Current.MainWindow).MealService;
            activityService = ((MainWindow)Application.Current.MainWindow).ActService;
            goalService = ((MainWindow)Application.Current.MainWindow).GoalService;

            DataContext = this;

            LoadHistory();
        }

        private void LoadHistory()
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

            var historyItems = new List<HistoryItem>();

            int totalConsumed = 0;
            int totalBurned = 0;
            int dailyGoal = 0;

            foreach (var meal in meals)
            {
                totalConsumed += meal.Calories;

                historyItems.Add(new HistoryItem
                {
                    Type = "Meal",
                    Name = meal.Name,
                    Details = meal.Category,
                    Calories = meal.Calories,
                    Date = meal.Date
                });
            }

            foreach (var activity in activities)
            {
                totalBurned += activity.CaloriesBurned;

                historyItems.Add(new HistoryItem
                {
                    Type = "Activity",
                    Name = activity.Name,
                    Details = $"{activity.Duration} min",
                    Calories = activity.CaloriesBurned,
                    Date = activity.Date
                });
            }

            if (goal != null)
            {
                dailyGoal = goal.DailyCalorieGoal;
            }

            int balance = dailyGoal - totalConsumed + totalBurned;

            MealsCountBlock.Text = meals.Count.ToString();
            ActivitiesCountBlock.Text = activities.Count.ToString();
            ConsumedBlock.Text = totalConsumed.ToString();
            BurnedBlock.Text = totalBurned.ToString();
            BalanceBlock.Text = balance.ToString();

            for (int i = 0; i < historyItems.Count - 1; i++)
            {
                for (int j = i + 1; j < historyItems.Count; j++)
                {
                    if (historyItems[j].Date > historyItems[i].Date)
                    {
                        var temp = historyItems[i];
                        historyItems[i] = historyItems[j];
                        historyItems[j] = temp;
                    }
                }
            }

            HistoryDataGrid.ItemsSource = historyItems;

            LoadCharts(meals, activities);
        }

        private void LoadCharts(List<Meal> meals, List<Activity> activities)
        {
            var mealValues = new List<double>();
            var mealLabels = new List<string>();

            foreach (var meal in meals)
            {
                mealValues.Add(meal.Calories);
                mealLabels.Add(meal.Name);
            }

            MealSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = mealValues
                }
            };

            MealXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = mealLabels
                }
            };

            var activityValues = new List<double>();
            var activityLabels = new List<string>();

            foreach (var activity in activities)
            {
                activityValues.Add(activity.CaloriesBurned);
                activityLabels.Add(activity.Name);
            }

            ActivitySeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = activityValues
                }
            };

            ActivityXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = activityLabels
                }
            };
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new DashboardView());
        }

        private void Activities_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new AddActivityView());
        }

        private void AddMeal_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new AddMealView());
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new HistoryView());
        }

        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new ProfileView());
        }
    }
}