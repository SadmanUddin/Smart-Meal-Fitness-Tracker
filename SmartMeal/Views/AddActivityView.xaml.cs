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
using SmartMeal.core.Models;


namespace SmartMeal.Views
{
    public partial class AddActivityView : UserControl
    {
        private readonly ActService activityService;
        public AddActivityView()
        {
            InitializeComponent();
            activityService = ((MainWindow)Application.Current.MainWindow).ActService;
        }
        private void AddActivity_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(CaloriesBurnedTextBox.Text, out int caloriesBurned))
            {
                MessageBox.Show("Please enter a valid number for calories burned.");
                return;
            }
            if (!int.TryParse(DurationTextBox.Text, out int durationMinutes))
            {
                MessageBox.Show("Please enter a valid number for duration.");
                return;
            }
            var activity = new Activity
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Empty, 
                Name = ActivityNameTextBox.Text,
                CaloriesBurned = caloriesBurned,
                Date = DateTime.Now
            };
            activityService.AddActivity(activity);
            MessageBox.Show("Activity added");
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new DashboardView());
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new DashboardView());
        }
    }
}
