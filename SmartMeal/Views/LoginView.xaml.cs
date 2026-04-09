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
    public partial class LoginView : UserControl
    {
        private readonly AuthService _authService;

        public LoginView() // constructor to initialize auth
        {
            InitializeComponent();
            _authService = ((MainWindow)Application.Current.MainWindow).AuthService;
        }
        public void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var result = _authService.LoginUser(EmailTextBox.Text, PasswordBox.Password);
            MessageBox.Show(result.Message);
        }
        public void RegisterText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new RegisterView());
        }
    }
}
