// RegisterView is the account creation screen — the first thing a new user sees.
// The user fills in their name, email, and password, then clicks Register.
// On success they are redirected to LoginView to sign in with their new account.
//
// Existing users can click the "Already have an account? Login" link to skip registration.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class RegisterView : UserControl
    {
        // AuthService handles all the registration logic including input validation,
        // creating the Supabase Auth account, and inserting the users table row.
        private readonly AuthService _authService;

        public RegisterView()
        {
            InitializeComponent();
            _authService = ((MainWindow)Application.Current.MainWindow).AuthService;
        }

        // Fires when the user clicks the Register button.
        // Passes all four form fields to AuthService, which validates them and talks to Supabase.
        // A MessageBox always appears — either a success message or an error explaining what went wrong.
        public async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await _authService.RegisterAsync(
                FullNameTextBox.Text,
                EmailTextBox.Text,
                PasswordBox.Password,
                ConfirmPasswordBox.Password);

            // Always show a message so the user knows what happened.
            MessageBox.Show(result.Message);

            // Only navigate away if registration actually succeeded.
            // If it failed the user stays here to correct their input.
            if (result.Success)
                ((MainWindow)Application.Current.MainWindow).Navigate(new LoginView());
        }

        // Fires when the user clicks the "Already have an account? Login" text link.
        // Takes them straight to the login form without going through registration.
        public void LoginText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Navigate(new LoginView());
        }
    }
}
