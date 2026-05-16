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
            try
            {
                if (!TryParseOptionalInt(AgeTextBox.Text, out var age))
                {
                    MessageBox.Show("Please enter a valid age.");
                    return;
                }

                if (!TryParseOptionalDecimal(HeightTextBox.Text, out var heightCm))
                {
                    MessageBox.Show("Please enter a valid height in cm.");
                    return;
                }

                if (!TryParseOptionalDecimal(WeightTextBox.Text, out var weightKg))
                {
                    MessageBox.Show("Please enter a valid weight in kg.");
                    return;
                }

                // Read the gender Tag from the selected ComboBoxItem.
                // ComboBoxItem.Tag stores the lowercase DB value ("male", "female", "other", or "").
                var gender = GetSelectedGenderTag();

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
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred during registration: {ex.Message}",
                    "Registration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Fires when the user clicks the "Already have an account? Login" text link.
        // Takes them straight to the login form without going through registration.
        public void LoginText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Navigate(new LoginView());
        }

        private string GetSelectedGenderTag()
        {
            if (GenderComboBox.SelectedItem is not ComboBoxItem selectedItem)
                return string.Empty;

            if (selectedItem.Tag == null)
                return string.Empty;

            var tagText = selectedItem.Tag.ToString();
            if (string.IsNullOrEmpty(tagText))
                return string.Empty;

            return tagText;
        }

        // Optional numeric field parser:
        // - blank text => null (valid)
        // - valid number => parsed value
        // - anything else => invalid
        private static bool TryParseOptionalInt(string text, out int? value)
        {
            var cleaned = text.Trim();
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                value = null;
                return true;
            }

            if (int.TryParse(cleaned, out var parsed))
            {
                value = parsed;
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryParseOptionalDecimal(string text, out decimal? value)
        {
            var cleaned = text.Trim();
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                value = null;
                return true;
            }

            if (decimal.TryParse(cleaned, out var parsed))
            {
                value = parsed;
                return true;
            }

            value = null;
            return false;
        }
    }
}
