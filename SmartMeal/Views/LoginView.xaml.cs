// LoginView is the sign-in screen.
// The user enters their email and password, clicks Login, and if the credentials are correct
// they are taken to the Dashboard. If not, an error message is shown and they stay here.
//
// This view is shown:
//   - When the app first starts (after RegisterView)
//   - After the user logs out from the Dashboard

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class LoginView : UserControl
    {
        // AuthService handles the actual sign-in logic and remembers who is logged in.
        // We pull it from MainWindow because that's where all services live.
        private readonly AuthService _authService;

        public LoginView()
        {
            InitializeComponent();
            _authService = ((MainWindow)Application.Current.MainWindow).AuthService;
        }

        // Fires when the user clicks the Login button.
        // Sends the email and password to Supabase Auth for verification.
        // On success: navigates to the Dashboard so the user can start using the app.
        // On failure: shows the error message (e.g. "Invalid login credentials") and stays here.
        public async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await _authService.LoginAsync(EmailTextBox.Text, PasswordBox.Password);

            // If login failed, show why and stop — the user needs to correct their input.
            if (!result.Success)
            {
                MessageBox.Show(result.Message);
                return;
            }

            // Login succeeded — AuthService.CurrentUser is now set with the user's profile.
            // Navigate to the Dashboard, which will load and display the user's data.
            ((MainWindow)Application.Current.MainWindow).Navigate(new DashboardView());
        }

        // Fires when the user clicks the "Don't have an account? Register" link.
        // Takes them to the registration form.
        public void RegisterText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Navigate(new RegisterView());
        }
    }
}
