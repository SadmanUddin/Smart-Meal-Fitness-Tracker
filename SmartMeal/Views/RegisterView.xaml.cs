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
    public partial class RegisterView : UserControl
    {
        private readonly AuthService _authService;
        public RegisterView()
        {
            InitializeComponent();
            _authService = ((MainWindow)Application.Current.MainWindow).AuthService;
        }
        public void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var result = _authService.RegisterUser(FullNameTextBox.Text, EmailTextBox.Text, PasswordBox.Password, ConfirmPasswordBox.Password);
            MessageBox.Show(result.Message); 

            if(result.Success)
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.Navigate(new LoginView());
            }
        }
        public void LoginText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Navigate to LoginView
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new LoginView());
        }
    }
}
