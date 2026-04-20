using SmartMeal.core.Services;

namespace SmartMeal.Helpers
{
    internal static class SessionHelper
    {
        public static bool TryGetCurrentUserId(AuthService authService, out string userId)
        {
            userId = string.Empty;

            var currentUser = authService.CurrentUser;
            if (currentUser == null)
                return false;

            if (string.IsNullOrWhiteSpace(currentUser.Id))
                return false;

            userId = currentUser.Id;
            return true;
        }
    }
}
