using SmartMeal.core.Models;
using SmartMeal.core.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;


namespace SmartMeal.Views
{
    public partial class HistoryView : UserControl
    {
        private readonly MealService mealService;
        private readonly ActService activityService;

        public HistoryView()
        {
            InitializeComponent();

            mealService = ((MainWindow)Application.Current.MainWindow).MealService;
            activityService = ((MainWindow)Application.Current.MainWindow).ActService;

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

            var historyItems = new List<HistoryItem>();

            foreach (var meal in meals)
            {
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
                historyItems.Add(new HistoryItem
                {
                    Type = "Activity",
                    Name = activity.Name,
                    Details = $"{activity.Duration} min",
                    Calories = activity.CaloriesBurned,
                    Date = activity.Date
                });
            }

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