// AdminDashboardView is the admin-only screen for user management.
//
// Features:
//   - Loads all users (requires admin RLS policy)
//   - Shows total / active / banned user counts
//   - Allows ban/unban toggling per user
//   - Lets admin sign out
//
// Security assumptions:
//   - Login routing only navigates here when CurrentUser.Role == "admin"
//     and CurrentUser.IsBanned == false
//   - RLS policies still enforce admin access server-side

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SmartMeal.core.Models;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class AdminDashboardView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly AdminService _adminService;
        private readonly AuthService _authService;

        // Raw users from DB; used to resolve action button clicks by UserId.
        private List<User> _users = new();

        public AdminDashboardView()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _adminService = _mainWindow.AdminService;
            _authService = _mainWindow.AuthService;

            Loaded += AdminDashboardView_Loaded;
        }

        private async void AdminDashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= AdminDashboardView_Loaded;
            await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            var currentUser = _authService.CurrentUser;
            if (currentUser == null || currentUser.Role != "admin" || currentUser.IsBanned)
            {
                if (currentUser?.IsBanned == true)
                {
                    try
                    {
                        await _authService.SignOutAsync();
                    }
                    catch
                    {
                        // If sign-out fails, still force navigation away from admin view.
                    }
                }

                MessageBox.Show(
                    "Active admin access is required.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                _mainWindow.Navigate(new LoginView());
                return;
            }

            try
            {
                _users = await _adminService.GetAllUsersAsync();

                var rows = _users.Select(ToRow).ToList();
                UsersDataGrid.ItemsSource = rows;

                var total = _users.Count;
                var banned = _users.Count(u => u.IsBanned);
                var active = total - banned;

                TotalUsersBlock.Text = total.ToString();
                ActiveUsersBlock.Text = active.ToString();
                BannedUsersBlock.Text = banned.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load admin data: {ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void BanToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string userId || string.IsNullOrWhiteSpace(userId))
                return;

            var targetUser = _users.FirstOrDefault(u => u.Id == userId);
            if (targetUser == null)
            {
                MessageBox.Show("The selected user could not be found.");
                return;
            }

            var currentUser = _authService.CurrentUser;
            if (currentUser != null && currentUser.Id == targetUser.Id)
            {
                MessageBox.Show("You cannot ban your own account.");
                return;
            }

            var nextBannedState = !targetUser.IsBanned;
            var actionText = nextBannedState ? "ban" : "unban";

            var confirm = MessageBox.Show(
                $"Are you sure you want to {actionText} {targetUser.Email}?",
                "Confirm Action",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                await _adminService.SetBannedAsync(targetUser.Id, nextBannedState);
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not update user status: {ex.Message}",
                    "Update Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void LogOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _authService.SignOutAsync();
                _mainWindow.Navigate(new LoginView());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not sign out: {ex.Message}",
                    "Logout Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static AdminUserRow ToRow(User user)
        {
            var isBanned = user.IsBanned;
            var actionLabel = isBanned ? "Unban" : "Ban";
            var actionColor = isBanned
                ? new SolidColorBrush(Color.FromRgb(16, 185, 129))   // green
                : new SolidColorBrush(Color.FromRgb(239, 68, 68));   // red

            return new AdminUserRow
            {
                UserId = user.Id,
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? "(No name)" : user.FullName,
                Email = user.Email,
                Role = user.Role,
                JoinedDate = user.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd"),
                Status = isBanned ? "Banned" : "Active",
                ActionLabel = actionLabel,
                ActionColor = actionColor
            };
        }

        private sealed class AdminUserRow
        {
            public string UserId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string JoinedDate { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string ActionLabel { get; set; } = string.Empty;
            public Brush ActionColor { get; set; } = Brushes.Gray;
        }
    }
}
