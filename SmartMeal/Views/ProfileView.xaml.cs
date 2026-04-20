// ProfileView lets the logged-in user view and edit their profile fields.
//
// Editable fields map directly to public.users columns:
//   - full_name (required)
//   - age (optional)
//   - height_cm (optional)
//   - weight_kg (optional)
//   - gender (optional: male/female/other)
//
// Save uses AuthService.UpdateCurrentUserProfileAsync so CurrentUser stays synced
// after the database update.

using System.Windows;
using System.Windows.Controls;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class ProfileView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly AuthService _authService;

        public ProfileView()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _authService = _mainWindow.AuthService;
            Loaded += ProfileView_Loaded;
        }

        private void ProfileView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ProfileView_Loaded;
            LoadProfileFromCurrentUser();
        }

        private void LoadProfileFromCurrentUser()
        {
            var user = _authService.CurrentUser;
            if (user == null)
            {
                MessageBox.Show("Session expired. Please log in again.");
                _mainWindow.Navigate(new LoginView());
                return;
            }

            FullNameTextBox.Text = user.FullName;
            EmailTextBox.Text = user.Email;
            RoleTextBox.Text = string.IsNullOrWhiteSpace(user.Role) ? "user" : user.Role;
            AgeTextBox.Text = user.Age.HasValue ? user.Age.Value.ToString() : string.Empty;
            HeightTextBox.Text = user.HeightCm.HasValue ? user.HeightCm.Value.ToString() : string.Empty;
            WeightTextBox.Text = user.WeightKg.HasValue ? user.WeightKg.Value.ToString() : string.Empty;
            SelectGenderByTag(user.Gender);

            var prefs = (user.FoodPreferences ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            PrefVegetarian.IsChecked  = prefs.Contains("Vegetarian", StringComparer.OrdinalIgnoreCase);
            PrefVegan.IsChecked       = prefs.Contains("Vegan", StringComparer.OrdinalIgnoreCase);
            PrefHalal.IsChecked       = prefs.Contains("Halal", StringComparer.OrdinalIgnoreCase);
            PrefKeto.IsChecked        = prefs.Contains("Keto", StringComparer.OrdinalIgnoreCase);
            PrefHighProtein.IsChecked = prefs.Contains("High-Protein", StringComparer.OrdinalIgnoreCase);
            PrefLowCarb.IsChecked     = prefs.Contains("Low-Carb", StringComparer.OrdinalIgnoreCase);
            PrefGlutenFree.IsChecked  = prefs.Contains("Gluten-Free", StringComparer.OrdinalIgnoreCase);
            PrefDairyFree.IsChecked   = prefs.Contains("Dairy-Free", StringComparer.OrdinalIgnoreCase);

            var allergies = (user.Allergies ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            AllergyNuts.IsChecked      = allergies.Contains("Nuts", StringComparer.OrdinalIgnoreCase);
            AllergyDairy.IsChecked     = allergies.Contains("Dairy", StringComparer.OrdinalIgnoreCase);
            AllergyGluten.IsChecked    = allergies.Contains("Gluten", StringComparer.OrdinalIgnoreCase);
            AllergyShellfish.IsChecked = allergies.Contains("Shellfish", StringComparer.OrdinalIgnoreCase);
            AllergyEggs.IsChecked      = allergies.Contains("Eggs", StringComparer.OrdinalIgnoreCase);
            AllergySoy.IsChecked       = allergies.Contains("Soy", StringComparer.OrdinalIgnoreCase);
            AllergyFish.IsChecked      = allergies.Contains("Fish", StringComparer.OrdinalIgnoreCase);
            AllergyWheat.IsChecked     = allergies.Contains("Wheat", StringComparer.OrdinalIgnoreCase);
        }

        private string CollectCheckedTags(params (CheckBox box, string tag)[] items)
        {
            return string.Join(",", items
                .Where(x => x.box.IsChecked == true)
                .Select(x => x.tag));
        }

        private async void SaveProfile_Click(object sender, RoutedEventArgs e)
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

                var gender = GetSelectedGenderTag();

                var foodPreferences = CollectCheckedTags(
                    (PrefVegetarian, "Vegetarian"),
                    (PrefVegan, "Vegan"),
                    (PrefHalal, "Halal"),
                    (PrefKeto, "Keto"),
                    (PrefHighProtein, "High-Protein"),
                    (PrefLowCarb, "Low-Carb"),
                    (PrefGlutenFree, "Gluten-Free"),
                    (PrefDairyFree, "Dairy-Free"));

                var allergies = CollectCheckedTags(
                    (AllergyNuts, "Nuts"),
                    (AllergyDairy, "Dairy"),
                    (AllergyGluten, "Gluten"),
                    (AllergyShellfish, "Shellfish"),
                    (AllergyEggs, "Eggs"),
                    (AllergySoy, "Soy"),
                    (AllergyFish, "Fish"),
                    (AllergyWheat, "Wheat"));

                var result = await _authService.UpdateCurrentUserProfileAsync(
                    FullNameTextBox.Text,
                    age,
                    heightCm,
                    weightKg,
                    gender,
                    foodPreferences,
                    allergies);

                if (!result.Success)
                {
                    MessageBox.Show(
                        result.Message,
                        "Save Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show(
                    result.Message,
                    "Profile Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Re-read values after save (AuthService refreshed CurrentUser).
                LoadProfileFromCurrentUser();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred while saving your profile: {ex.Message}",
                    "Profile Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new DashboardView());
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new DashboardView());
        }

        private void Meals_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new MealsView());
        }

        private void Activities_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new AddActivityView());
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new WeightHistoryView());
        }

        private void Recommendations_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new RecommendationsView());
        }

        private async void BackToLog_Click(object sender, RoutedEventArgs e)
        {
            await _authService.SignOutAsync();
            _mainWindow.Navigate(new LoginView());
        }

        private string GetSelectedGenderTag()
        {
            if (GenderComboBox.SelectedItem is not ComboBoxItem selectedItem)
                return string.Empty;

            if (selectedItem.Tag == null)
                return string.Empty;

            var tag = selectedItem.Tag.ToString();
            if (string.IsNullOrWhiteSpace(tag))
                return string.Empty;

            return tag;
        }

        private void SelectGenderByTag(string? gender)
        {
            var target = string.IsNullOrWhiteSpace(gender)
                ? string.Empty
                : gender.Trim().ToLowerInvariant();

            foreach (var item in GenderComboBox.Items)
            {
                if (item is not ComboBoxItem comboItem)
                    continue;

                var tagText = comboItem.Tag?.ToString() ?? string.Empty;
                if (string.Equals(tagText, target, StringComparison.OrdinalIgnoreCase))
                {
                    GenderComboBox.SelectedItem = comboItem;
                    return;
                }
            }

            GenderComboBox.SelectedIndex = 0;
        }

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