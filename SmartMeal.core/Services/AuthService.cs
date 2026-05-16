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
        private readonly string? _emailRedirectUrl;

        public AuthService(Supabase.Client client, string? emailRedirectUrl = null)
        {
            _client = client;
            _emailRedirectUrl = string.IsNullOrWhiteSpace(emailRedirectUrl) ? null : emailRedirectUrl.Trim();
        }

        // The full profile of the currently signed-in user, loaded from the public.users table.
        // This is set in LoginAsync after a successful sign-in and cleared in SignOutAsync.
        // It is read-only outside this class — only AuthService should change who is logged in.
        public Models.User? CurrentUser { get; private set; }

        // Creates a new account.
        //
        // Two stages:
        //   Stage 1 — Supabase Auth creates an auth account and (if enabled) sends a
        //             confirmation email.
        //   Stage 2 — Once the account is confirmed and the user signs in, we ensure the
        //             corresponding public.users profile row exists, including all profile fields.
        //
        // Optional profile fields (age, heightCm, weightKg, gender) are stored in Supabase
        // Auth user_metadata at signup time. If email confirmation is required, the profile
        // row is created at first login — EnsureUserProfileExistsAsync reads the metadata and
        // saves the fields to the users table then. If confirmation is disabled (auto-confirm),
        // the full profile is written immediately.
        //
        // If weightKg is provided, an initial weight_logs entry ("Starting weight") is also
        // created so the weight history chart has a baseline entry.
        //
        // Returns a tuple of (Success, Message) so the calling view can show the right message
        // without needing to catch exceptions itself.
        public async Task<(bool Success, string Message)> RegisterAsync(
            string name, string email, string password, string confirmPassword,
            int? age = null, decimal? heightCm = null, decimal? weightKg = null, string? gender = null)
        {
            var cleanedName = name.Trim();
            var cleanedEmail = email.Trim();

            // Validate required fields before hitting the network — faster feedback for the user.
            if (string.IsNullOrWhiteSpace(cleanedName) || string.IsNullOrWhiteSpace(cleanedEmail)
                || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
                return (false, "Please fill in all required fields.");

            // Basic email sanity check (a proper regex would be overkill here).
            if (!cleanedEmail.Contains('@') || !cleanedEmail.Contains('.'))
                return (false, "Invalid email format.");

            if (password != confirmPassword)
                return (false, "Passwords do not match.");

            // Supabase Auth enforces a minimum of 6 characters.
            if (password.Length < 6)
                return (false, "Password must be at least 6 characters.");

            // Validate optional profile fields if provided.
            if (age.HasValue && (age.Value < 1 || age.Value > 120))
                return (false, "Please enter a valid age (1–120).");
            if (heightCm.HasValue && heightCm.Value <= 0)
                return (false, "Please enter a valid height in cm.");
            if (weightKg.HasValue && weightKg.Value <= 0)
                return (false, "Please enter a valid weight in kg.");
            if (!string.IsNullOrWhiteSpace(gender) && gender != "male" && gender != "female" && gender != "other")
                return (false, "Please select a valid gender option.");

            try
            {
                var signUpOptions = new Supabase.Gotrue.SignUpOptions
                {
                    // Store profile data in Auth user_metadata so it survives the
                    // email-confirmation gap (if confirmation is required, the users
                    // table row is created at first login, not here).
                    Data = new Dictionary<string, object>
                    {
                        ["full_name"] = cleanedName,
                        ["age"] = age?.ToString() ?? "",
                        ["height_cm"] = heightCm?.ToString() ?? "",
                        ["weight_kg"] = weightKg?.ToString() ?? "",
                        ["gender"] = gender ?? ""
                    }
                };

                if (!string.IsNullOrWhiteSpace(_emailRedirectUrl))
                    signUpOptions.RedirectTo = _emailRedirectUrl;

                //var session = await _client.Auth.SignUp(cleanedEmail, password, signUpOptions);

                //if (session?.User == null || string.IsNullOrWhiteSpace(session.User.Id))
                //{
                //    return (false, "Registration failed.");
                //}

                //// Detect duplicate email situation
                //if (!string.IsNullOrWhiteSpace(session.User.Email) &&
                //    session.User.EmailConfirmedAt.HasValue == false &&
                //    session.AccessToken == null)
                //{
                //    return (false, "This email is already registered.");
                //}
                var session = await _client.Auth.SignUp(cleanedEmail, password, signUpOptions);
                if (session?.User?.Id == null)
                    return (false, "Registration failed. Please try again.");

                var isEmailConfirmed = session.User.EmailConfirmedAt.HasValue;

                // If email confirmation is disabled, the session is live now.
                // Insert the full users row immediately, including all profile fields.
                if (isEmailConfirmed)
                {
                    await EnsureUserProfileExistsAsync(
                        session.User, cleanedName, cleanedEmail,
                        age, heightCm, weightKg, NormalizeGender(gender));
                    return (true, "Registration successful! Please log in.");
                }

                // Email confirmation required — the users row will be created at first login.
                // The profile fields are preserved in Auth user_metadata until then.
                return (true, "Registration successful. Please confirm your email, then sign in.");
            }
            catch (Exception ex)
            {
                // Supabase returns descriptive error messages (e.g. "User already registered").
                return (false, ex.Message);
            }
        }

        // Signs the user in using their email and password.
        //
        // After a successful sign-in we ensure the user's public.users row exists, then load
        // it into CurrentUser. This means every other part of the app can read
        // CurrentUser.Id, CurrentUser.FullName, etc. without making extra DB calls.
        //
        // On first login after email confirmation, EnsureUserProfileExistsAsync reads the
        // profile fields from Auth user_metadata and saves them to the users table.
        //
        // Returns (Success, Message) so the calling view can handle outcomes cleanly.
        public async Task<(bool Success, string Message)> LoginAsync(string email, string password)
        {
            var cleanedEmail = email.Trim();

            if (string.IsNullOrWhiteSpace(cleanedEmail) || string.IsNullOrWhiteSpace(password))
                return (false, "Please fill in all fields.");

            try
            {
                var session = await _client.Auth.SignIn(cleanedEmail, password);
                if (session?.User?.Id == null)
                    return (false, "Invalid email or password.");

                if (!session.User.EmailConfirmedAt.HasValue)
                    return (false, "Please confirm your email address before signing in.");

                // Ensure a profile row exists (creates it on first login if needed),
                // then cache it in memory. Profile fields are read from user_metadata
                // if this is the first login.
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

        // Updates the current user's profile fields in public.users.
        //
        // Editable fields:
        //   - full_name (required)
        //   - age, height_cm, weight_kg, gender (all optional)
        //
        // On success, CurrentUser is refreshed from the DB so every view sees
        // the latest values immediately.
        public async Task<(bool Success, string Message)> UpdateCurrentUserProfileAsync(
            string fullName,
            int? age = null,
            decimal? heightCm = null,
            decimal? weightKg = null,
            string? gender = null,
            string? foodPreferences = null,
            string? allergies = null)
        {
            var currentUser = CurrentUser;
            if (currentUser == null || string.IsNullOrWhiteSpace(currentUser.Id))
                return (false, "No user is currently logged in.");

            var cleanedName = fullName.Trim();
            if (string.IsNullOrWhiteSpace(cleanedName))
                return (false, "Full name is required.");

            if (age.HasValue && (age.Value < 1 || age.Value > 120))
                return (false, "Please enter a valid age (1–120).");
            if (heightCm.HasValue && heightCm.Value <= 0)
                return (false, "Please enter a valid height in cm.");
            if (weightKg.HasValue && weightKg.Value <= 0)
                return (false, "Please enter a valid weight in kg.");

            var normalizedGender = NormalizeGender(gender);
            if (!string.IsNullOrWhiteSpace(gender) && normalizedGender == null)
                return (false, "Please select a valid gender option.");

            try
            {
                // Work on a local copy first so CurrentUser cannot drift from DB state
                // if the UPDATE request fails.
                var previousWeight = currentUser.WeightKg;
                var updatedUser = new User
                {
                    Id = currentUser.Id,
                    FullName = cleanedName,
                    Email = currentUser.Email,
                    Role = currentUser.Role,
                    Age = age,
                    HeightCm = heightCm,
                    WeightKg = weightKg,
                    Gender = normalizedGender,
                    CreatedAt = currentUser.CreatedAt,
                    IsBanned = currentUser.IsBanned,
                    FoodPreferences = foodPreferences ?? currentUser.FoodPreferences,
                    Allergies = allergies ?? currentUser.Allergies
                };

                await _client.From<User>()
                    .Where(u => u.Id == updatedUser.Id)
                    .Update(updatedUser);

                // Re-read from DB so CurrentUser reflects exactly what was persisted.
                var refreshed = await _client.From<User>()
                    .Where(u => u.Id == updatedUser.Id)
                    .Get();
                var latest = refreshed.Models.FirstOrDefault();
                if (latest != null)
                    CurrentUser = latest;
                else
                    CurrentUser = updatedUser;

                // Keep weight history aligned with the profile's weight snapshot.
                if (weightKg.HasValue && (!previousWeight.HasValue || previousWeight.Value != weightKg.Value))
                {
                    try
                    {
                        await _client.From<WeightLog>().Insert(new WeightLog
                        {
                            UserId = updatedUser.Id,
                            WeightKg = weightKg.Value,
                            LoggedAt = DateTime.UtcNow,
                            Notes = "Profile update"
                        });
                    }
                    catch (Exception ex)
                    {
                        return (true, $"Profile updated, but weight history entry failed: {ex.Message}");
                    }
                }

                return (true, "Profile updated successfully.");
            }
            catch (Exception ex)
            {
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

        // Creates the public.users row for a new user if it doesn't exist yet.
        //
        // Called from both RegisterAsync (if auto-confirmed) and LoginAsync (first login).
        // At registration time, the explicit profile fields are passed in.
        // At login time, no explicit fields are passed — they're read from Auth user_metadata
        // instead, which were stored there at signup.
        //
        // If a starting weight is available (from params or metadata), an initial
        // weight_logs entry is also created so the weight history chart has a baseline.
        private async Task<User> EnsureUserProfileExistsAsync(
            Supabase.Gotrue.User authUser,
            string? preferredFullName,
            string fallbackEmail,
            int? age = null,
            decimal? heightCm = null,
            decimal? weightKg = null,
            string? gender = null)
        {
            var existingResult = await _client.From<User>()
                .Where(u => u.Id == authUser.Id)
                .Get();

            var existing = existingResult.Models.FirstOrDefault();
            if (existing != null)
                return existing;

            // Resolve profile fields — prefer explicitly passed values (from RegisterAsync),
            // fall back to values stored in Auth user_metadata (for first-login-after-confirmation).
            var resolvedAge = age ?? TryReadMetadataInt(authUser.UserMetadata, "age");
            var resolvedHeight = heightCm ?? TryReadMetadataDecimal(authUser.UserMetadata, "height_cm");
            var resolvedWeight = weightKg ?? TryReadMetadataDecimal(authUser.UserMetadata, "weight_kg");
            var resolvedGender = gender ?? NormalizeGender(TryReadMetadataValue(authUser.UserMetadata, "gender"));

            var fullName = ResolveFullName(authUser, preferredFullName);
            var profile = new User
            {
                Id = authUser.Id ?? throw new InvalidOperationException("Auth user ID is missing."),
                FullName = fullName,
                Email = !string.IsNullOrWhiteSpace(authUser.Email) ? authUser.Email! : fallbackEmail,
                Role = "user",
                Age = resolvedAge,
                HeightCm = resolvedHeight,
                WeightKg = resolvedWeight,
                Gender = resolvedGender,
                CreatedAt = DateTime.UtcNow
            };

            await _client.From<User>().Insert(profile);

            // Log the starting weight so the weight history chart has a baseline entry.
            // This only runs once (when the users row is first created).
            if (resolvedWeight.HasValue)
            {
                await _client.From<WeightLog>().Insert(new WeightLog
                {
                    UserId = profile.Id,
                    WeightKg = resolvedWeight.Value,
                    LoggedAt = DateTime.UtcNow,
                    Notes = "Starting weight"
                });
            }

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

        // Validates and normalises a gender string to the values the DB accepts.
        // Returns null if the value is empty or unrecognised.
        private static string? NormalizeGender(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var lower = value.Trim().ToLowerInvariant();
            return lower == "male" || lower == "female" || lower == "other" ? lower : null;
        }

        // --- Metadata reading helpers ---
        // Auth user_metadata can come back as several different types depending on the
        // SDK version and how the data was stored, so we handle all of them.

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
                // Ignore metadata parsing failures and fall back to null.
            }

            return null;
        }

        private static int? TryReadMetadataInt(object? metadata, string key)
        {
            var raw = TryReadMetadataValue(metadata, key);
            return int.TryParse(raw, out var result) ? result : null;
        }

        private static decimal? TryReadMetadataDecimal(object? metadata, string key)
        {
            var raw = TryReadMetadataValue(metadata, key);
            return decimal.TryParse(raw, out var result) ? result : null;
        }
    }
}
