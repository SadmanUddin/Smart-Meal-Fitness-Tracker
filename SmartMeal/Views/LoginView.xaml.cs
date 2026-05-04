// LoginView is the sign-in screen.
// The user enters their email and password, clicks Login, and if the credentials are correct
// they are taken to the appropriate view — AdminDashboardView for admins, DashboardView for
// regular users. Banned users are shown an error and their session is immediately ended.
//
// This view is shown:
//   - When the app first starts (after RegisterView)
//   - After the user logs out from any view

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
        //
        // After a successful sign-in, three things happen before navigating:
        //   1. Ban check — banned users are rejected immediately and their session is ended.
        //   2. Role check — admins go to AdminDashboardView; regular users go to DashboardView.
        public async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await _authService.LoginAsync(EmailTextBox.Text, PasswordBox.Password);

                // If login failed, show why and stop — the user needs to correct their input.
                if (!result.Success)
                {
                    MessageBox.Show("Invalid email or password. Please try again.");
                    return;
                }

                var mainWindow = (MainWindow)Application.Current.MainWindow;
                var currentUser = _authService.CurrentUser;

                // Ban check — happens before role routing so banned admins are also blocked.
                // SignOutAsync clears the session so the user cannot stay signed in.
                // Wrapped in its own try/catch: a network failure during sign-out must not
                // leave the user stuck on a blank screen — we still show the suspend message.
                if (currentUser?.IsBanned == true)
                {
                    try
                    {
                        await _authService.SignOutAsync();
                    }
                    catch
                    {
                        // Session invalidation failed (network/server issue).
                        // The user is still blocked in the app; the session will expire naturally.
                    }

                    MessageBox.Show(
                        "Your account has been suspended. Please contact support.",
                        "Account Suspended",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Route to the appropriate home screen based on the user's role.
                if (currentUser?.Role == "admin")
                    mainWindow.Navigate(new AdminDashboardView());
                else
                    mainWindow.Navigate(new DashboardView());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred during login",
                    "Login Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Fires when the user clicks the "Don't have an account? Register" link.
        // Takes them to the registration form.
        public void RegisterText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Navigate(new RegisterView());
        }
    }
}
