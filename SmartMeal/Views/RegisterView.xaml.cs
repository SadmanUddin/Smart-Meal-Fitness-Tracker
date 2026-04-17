// RegisterView is the account creation screen.
// The user fills in their name, email, password, and optional profile details
// (age, gender, height, starting weight), then clicks Register.
// On success they are redirected to LoginView to sign in with their new account.
//
// Profile fields are optional — the user can leave them blank and fill them in later.
// If a starting weight is entered, it is saved to the weight_logs table as the
// baseline entry that appears at the start of the weight history graph.
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
        // creating the Supabase Auth account, inserting the users table row,
        // and logging the starting weight if provided.
        private readonly AuthService _authService;

        public RegisterView()
        {
            InitializeComponent();
            _authService = ((MainWindow)Application.Current.MainWindow).AuthService;
        }

        // Fires when the user clicks the Register button.
        // Collects all form fields, passes them to AuthService, and shows the result.
        // Required fields: Full Name, Email, Password, Confirm Password.
        // Optional fields: Age, Gender, Height, Starting Weight.
        // A MessageBox always appears — either a success message or an error explaining what went wrong.
        public async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Parse optional numeric fields.
            // Blank is allowed (null), but non-blank invalid input should fail fast with a clear message.
            var ageText = AgeTextBox.Text.Trim();
            int? age = null;
            if (!string.IsNullOrWhiteSpace(ageText))
            {
                if (!int.TryParse(ageText, out var parsedAge))
                {
                    MessageBox.Show("Please enter a valid age.");
                    return;
                }
                age = parsedAge;
            }

            var heightText = HeightTextBox.Text.Trim();
            decimal? heightCm = null;
            if (!string.IsNullOrWhiteSpace(heightText))
            {
                if (!decimal.TryParse(heightText, out var parsedHeight))
                {
                    MessageBox.Show("Please enter a valid height in cm.");
                    return;
                }
                heightCm = parsedHeight;
            }

            var weightText = WeightTextBox.Text.Trim();
            decimal? weightKg = null;
            if (!string.IsNullOrWhiteSpace(weightText))
            {
                if (!decimal.TryParse(weightText, out var parsedWeight))
                {
                    MessageBox.Show("Please enter a valid weight in kg.");
                    return;
                }
                weightKg = parsedWeight;
            }

            // Read the gender Tag from the selected ComboBoxItem.
            // ComboBoxItem.Tag stores the lowercase DB value ("male", "female", "other", or "").
            var genderTag = (GenderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            string? gender = string.IsNullOrEmpty(genderTag) ? null : genderTag;

            var result = await _authService.RegisterAsync(
                FullNameTextBox.Text,
                EmailTextBox.Text,
                PasswordBox.Password,
                ConfirmPasswordBox.Password,
                age,
                heightCm,
                weightKg,
                gender);

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
