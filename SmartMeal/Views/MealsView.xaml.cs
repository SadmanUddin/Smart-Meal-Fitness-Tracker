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
    public partial class MealsView : UserControl
    {
        private readonly MealService mealService;
        public MealsView()
        {
            InitializeComponent();
            mealService = ((MainWindow)Application.Current.MainWindow).MealService;
            LoadMeals();
        }
        private void LoadMeals()
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            if (mainWindow.CurrentUser == null)
            {
                MessageBox.Show("No user logged in.");
                return;
            }
            var userMeals = mealService.GetMealsByUser(mainWindow.CurrentUser.Id);
            MealsDataGrid.ItemsSource = null; // Clearing existing items
            MealsDataGrid.ItemsSource = userMeals;
        }
        private void DeleteMeal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid mealId)
            {
                var result = MessageBox.Show("Are you sure you want to delete this meal?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    mealService.DeleteMeal(mealId);
                    LoadMeals(); // Refresh the meal list after deleting the specific meal
                }
            }
        }
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new DashboardView());
        }
        private void AddMeal_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new AddMealView());
        }
        private void Activities_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new AddActivityView());
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
