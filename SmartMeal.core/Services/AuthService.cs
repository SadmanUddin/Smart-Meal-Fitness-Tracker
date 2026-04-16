// AuthService handles everything related to who is using the app.
// It is responsible for two things:
//   1. Talking to Supabase Auth to create accounts, sign in, and sign out.
//   2. Holding the currently logged-in user's profile in memory (CurrentUser)
//      so any view can read it without making another database call.
//
// Every view that needs to know "who is logged in?" reads AuthService.CurrentUser.
// If CurrentUser is null, no one is logged in and the user should be redirected to LoginView.

using SmartMeal.core.Models;
using System.Collections;
using System.Text.Json;

namespace SmartMeal.core.Services
{
    public class AuthService
    {
        // The Supabase SDK client — used for both auth operations (SignUp, SignIn, SignOut)
        // and for querying the users table to load the profile after login.
        private readonly Supabase.Client _client;

        public AuthService(Supabase.Client client)
        {
            _client = client;
        }

        // The full profile of the currently signed-in user, loaded from the public.users table.
        // This is set in LoginAsync after a successful sign-in and cleared in SignOutAsync.
        // It is read-only outside this class — only AuthService should change who is logged in.
        public Models.User? CurrentUser { get; private set; }

        // Creates a new account.
        //
        // There are two stages in this flow:
        //   Stage 1 — Supabase Auth creates an auth account and (if enabled) sends a
        //             confirmation email.
        //   Stage 2 — Once the account is confirmed and the user signs in, we ensure the
        //             corresponding public.users profile row exists.
        //
        // Returns a tuple of (Success, Message) so the calling view can show the right message
        // without needing to catch exceptions itself.
        public async Task<(bool Success, string Message)> RegisterAsync(
            string name, string email, string password, string confirmPassword)
        {
            var cleanedName = name.Trim();
            var cleanedEmail = email.Trim();

            // Validate inputs before hitting the network — faster feedback for the user.
            if (string.IsNullOrWhiteSpace(cleanedName) || string.IsNullOrWhiteSpace(cleanedEmail)
                || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
                return (false, "Please fill in all fields.");

            // Basic email sanity check (a proper regex would be overkill here).
            if (!cleanedEmail.Contains('@') || !cleanedEmail.Contains('.'))
                return (false, "Invalid email format.");

            if (password != confirmPassword)
                return (false, "Passwords do not match.");

            // Supabase Auth enforces a minimum of 6 characters. We check here first
            // so we can give a clear message before the network call.
            if (password.Length < 6)
                return (false, "Password must be at least 6 characters.");

            try
            {
                var signUpOptions = new Supabase.Gotrue.SignUpOptions
                {
                    Data = new Dictionary<string, object>
                    {
                        ["full_name"] = cleanedName
                    }
                };

                // Ask Supabase Auth to create the account.
                // With email confirmation enabled, this usually creates the auth user first
                // and only allows a proper session after the user confirms by email.
                var session = await _client.Auth.SignUp(cleanedEmail, password, signUpOptions);
                if (session?.User?.Id == null)
                    return (false, "Registration failed. Please try again.");

                var isEmailConfirmed = session.User.EmailConfirmedAt.HasValue;

                // If confirmations are disabled, we may already have a valid session now.
                // In that case, ensure the profile row exists immediately.
                if (isEmailConfirmed)
                {
                    await EnsureUserProfileExistsAsync(session.User, cleanedName, cleanedEmail);
                    return (true, "Registration successful! Please log in.");
                }

                return (true, "Registration successful. Please confirm your email, then sign in.");
            }
            catch (Exception ex)
            {
                // Supabase returns descriptive error messages (e.g. "User already registered"),
                // so passing ex.Message directly gives the user useful information.
                return (false, ex.Message);
            }
        }

        // Signs the user in using their email and password.
        //
        // After a successful sign-in we ensure the user's public.users row exists, then load
        // it into CurrentUser. This means every other part of the app can read
        // CurrentUser.Id, CurrentUser.FullName, etc. without making extra DB calls.
        //
        // Returns (Success, Message) so the calling view can handle outcomes cleanly.
        public async Task<(bool Success, string Message)> LoginAsync(string email, string password)
        {
            var cleanedEmail = email.Trim();

            if (string.IsNullOrWhiteSpace(cleanedEmail) || string.IsNullOrWhiteSpace(password))
                return (false, "Please fill in all fields.");

            try
            {
                // Supabase Auth verifies the credentials and returns a session with a JWT token.
                // That token is automatically included in all subsequent API calls by the SDK.
                var session = await _client.Auth.SignIn(cleanedEmail, password);
                if (session?.User?.Id == null)
                    return (false, "Invalid email or password.");

                if (!session.User.EmailConfirmedAt.HasValue)
                    return (false, "Please confirm your email address before signing in.");

                // Ensure a profile row exists, then cache it in memory.
                CurrentUser = await EnsureUserProfileExistsAsync(session.User, null, cleanedEmail);
                return (true, "Login successful.");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase))
                    return (false, "Please confirm your email address before signing in.");

                return (false, ex.Message);
            }
        }

        // Signs the user out.
        // Tells Supabase Auth to invalidate the session token, then clears CurrentUser
        // so the rest of the app knows nobody is logged in anymore.
        public async Task SignOutAsync()
        {
            await _client.Auth.SignOut();
            CurrentUser = null;
        }

        private async Task<User> EnsureUserProfileExistsAsync(
            Supabase.Gotrue.User authUser,
            string? preferredFullName,
            string fallbackEmail)
        {
            var existingResult = await _client.From<User>()
                .Where(u => u.Id == authUser.Id)
                .Get();

            var existing = existingResult.Models.FirstOrDefault();
            if (existing != null)
                return existing;

            var fullName = ResolveFullName(authUser, preferredFullName);
            var profile = new User
            {
                Id = authUser.Id ?? throw new InvalidOperationException("Auth user ID is missing."),
                FullName = fullName,
                Email = !string.IsNullOrWhiteSpace(authUser.Email) ? authUser.Email! : fallbackEmail,
                Role = "user",
                CreatedAt = DateTime.UtcNow
            };

            await _client.From<User>().Insert(profile);
            return profile;
        }

        private static string ResolveFullName(Supabase.Gotrue.User authUser, string? preferredFullName)
        {
            if (!string.IsNullOrWhiteSpace(preferredFullName))
                return preferredFullName.Trim();

            var metadataName = TryReadMetadataValue(authUser.UserMetadata, "full_name");
            if (!string.IsNullOrWhiteSpace(metadataName))
                return metadataName.Trim();

            if (!string.IsNullOrWhiteSpace(authUser.Email))
                return authUser.Email.Split('@')[0];

            return "User";
        }

        private static string? TryReadMetadataValue(object? metadata, string key)
        {
            if (metadata is IDictionary<string, object> dict && dict.TryGetValue(key, out var value))
                return value?.ToString();

            if (metadata is IDictionary untypedDict && untypedDict.Contains(key))
                return untypedDict[key]?.ToString();

            if (metadata == null)
                return null;

            try
            {
                using var json = JsonDocument.Parse(metadata.ToString() ?? "{}");
                if (json.RootElement.ValueKind == JsonValueKind.Object
                    && json.RootElement.TryGetProperty(key, out var property)
                    && property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString();
                }
            }
            catch
            {
                // Ignore metadata parsing failures and fall back to other name sources.
            }

            return null;
        }
    }
}
