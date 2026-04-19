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
using SmartMeal.Views;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class ProfileView : UserControl
    {
        public ProfileView()
        {
            InitializeComponent();
            LoadProfile();
        }
        private void LoadProfile()
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            if (mainWindow.CurrentUser == null)
            {
                MessageBox.Show("No user logged in.");
                return;
            }
            var user = mainWindow.CurrentUser;
            NameBlock.Text = user.Name;
            EmailBlock.Text = user.Email;
            RoleBlock.Text = user.Role;
            CreatedAtBlock.Text = user.CreatedAt.ToString("MMMM dd, yyyy");
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
        private void Meals_Click(object sender, RoutedEventArgs e)
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
