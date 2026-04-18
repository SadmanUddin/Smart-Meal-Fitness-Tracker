// MainWindow is the shell of the entire application.
// It owns the Supabase connection and all services, then passes them to views via its public properties.
// The main content area (MainContent) swaps views in and out — this is how navigation works in this app.
// There is no router — views are loaded by creating a new instance and setting MainContent.Content to it.

using SmartMeal.Data.Context;
using SmartMeal.Views;
using SmartMeal.core.Services;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace SmartMeal
{
    public partial class MainWindow : Window
    {
        // These strings are what a fresh, unconfigured supabase.config.json contains.
        // If the config still has these exact placeholder values, the app refuses to start
        // and tells the developer to fill in their real credentials first.
        private const string UrlPlaceholder = "YOUR_SUPABASE_PROJECT_URL";
        private const string KeyPlaceholder = "YOUR_SUPABASE_ANON_KEY";
        private const string RedirectPlaceholder = "YOUR_SUPABASE_EMAIL_REDIRECT_URL";
        private const string UrlEnvVar = "SMARTMEAL_SUPABASE_URL";
        private const string KeyEnvVar = "SMARTMEAL_SUPABASE_ANON_KEY";
        private const string RedirectEnvVar = "SMARTMEAL_SUPABASE_EMAIL_REDIRECT_URL";

        // All services are initialised asynchronously in InitializeAsync() once the Supabase
        // client is ready. They are declared as null! here because they cannot be created
        // until we have a live database connection. Views read these via the public properties below.
        public AuthService AuthService { get; private set; } = null!;
        public MealService MealService { get; private set; } = null!;
        public FoodService FoodService { get; private set; } = null!;
        public GoalService GoalService { get; private set; } = null!;
        public WeightLogService WeightLogService { get; private set; } = null!;
        public ActService ActService { get; private set; } = null!;
        // Admin-only service. Regular users don't hold a reference to this,
        // but it lives here so AdminDashboardView can pull it out the same way
        // all other views pull their services.
        public AdminService AdminService { get; private set; } = null!;
        public FoodSearchService FoodSearchService { get; private set; } = null!;
        public GeminiService GeminiService { get; private set; } = null!;

        public MainWindow()
        {
            InitializeComponent();

            // We cannot do async work inside a constructor, so we hook into the Loaded event
            // which fires after the window is fully rendered. That's where async startup happens.
            Loaded += MainWindow_Loaded;
        }

        // Called once the window is visible. Immediately unsubscribes itself so it only runs once.
        // Any failure during startup shows an error dialog and closes the app — there is no point
        // running without a working database connection.
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;

            try
            {
                await InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Application startup failed: {ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
            }
        }

        // The real startup sequence. Reads credentials, opens the Supabase connection,
        // builds all services, then shows the first screen (RegisterView).
        private async Task InitializeAsync()
        {
            var config = await LoadSupabaseConfigAsync();

            // Make sure the developer replaced the placeholder values with real credentials.
            ValidateConfig(config);

            // SupabaseClientProvider wraps the Supabase SDK client. InitializeAsync() opens
            // the connection and prepares it for queries. All services share this single client.
            var provider = new SupabaseClientProvider(config.SupabaseUrl, config.SupabaseAnonKey);
            await provider.InitializeAsync();

            // Build every service, handing each one the same live Supabase client.
            // Views pull these services out of MainWindow using the public properties above.
            AuthService = new AuthService(provider.Client, config.SupabaseEmailRedirectUrl);
            MealService = new MealService(provider.Client);
            FoodService = new FoodService(provider.Client);
            GoalService = new GoalService(provider.Client);
            WeightLogService = new WeightLogService(provider.Client);
            ActService = new ActService(provider.Client);
            AdminService = new AdminService(provider.Client);
            FoodSearchService = new FoodSearchService(config.UsdaApiKey);
            GeminiService = new GeminiService(config.GeminiApiKey ?? string.Empty);

            // Show the registration screen first. New users register, existing users
            // navigate to login from the link at the bottom of the register form.
            MainContent.Content = new RegisterView();
        }

        // The single navigation method used across every view in the app.
        // A view navigates by calling: ((MainWindow)Application.Current.MainWindow).Navigate(new SomeView())
        // This replaces whatever is currently shown in the MainContent area with the new view.
        public void Navigate(UserControl view)
        {
            MainContent.Content = view;
        }

        private async Task<SupabaseConfig> LoadSupabaseConfigAsync()
        {
            // Safe option 1: VS environment variables (recommended for local secrets).
            var envUrl = Environment.GetEnvironmentVariable(UrlEnvVar);
            var envKey = Environment.GetEnvironmentVariable(KeyEnvVar);
            var envRedirect = Environment.GetEnvironmentVariable(RedirectEnvVar);
            if (!string.IsNullOrWhiteSpace(envUrl) && !string.IsNullOrWhiteSpace(envKey))
                return new SupabaseConfig(envUrl.Trim(), envKey.Trim(), NormalizeOptional(envRedirect));

            // Safe option 2: local config files (gitignored).
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidatePaths = new[]
            {
                Path.Combine(baseDir, "supabase.config.local.json"),
                Path.Combine(baseDir, "supabase.config.json"),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "supabase.config.local.json")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "supabase.config.json"))
            };

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                    return await ReadConfigFileAsync(path);
            }

            throw new FileNotFoundException(
                $"Supabase config not found. Set environment variables `{UrlEnvVar}` and `{KeyEnvVar}`, or create `supabase.config.local.json` from `supabase.config.example.json` in the SmartMeal project.");
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static async Task<SupabaseConfig> ReadConfigFileAsync(string path)
        {
            var json = await File.ReadAllTextAsync(path);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<SupabaseConfig>(json, opts)
                ?? throw new InvalidOperationException($"Could not load Supabase config file: {path}");
        }

        // Guards against a developer accidentally shipping the app with placeholder credentials.
        // Also catches the case where the file exists but the URL or key were left blank.
        private static void ValidateConfig(SupabaseConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.SupabaseUrl) || string.IsNullOrWhiteSpace(config.SupabaseAnonKey))
                throw new InvalidOperationException("Supabase configuration is missing URL or anon key.");

            if (config.SupabaseUrl.Contains(UrlPlaceholder, StringComparison.OrdinalIgnoreCase)
                || config.SupabaseAnonKey.Contains(KeyPlaceholder, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Please replace placeholder values with real Supabase credentials.");

            if (!string.IsNullOrWhiteSpace(config.SupabaseEmailRedirectUrl)
                && config.SupabaseEmailRedirectUrl.Contains(RedirectPlaceholder, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Please replace the Supabase email redirect placeholder with a real URL, or remove the property.");
            }
        }

        // A lightweight record just for deserialising supabase.config.json.
        // The property names here must exactly match the JSON keys in the file.
        private record SupabaseConfig(
            string SupabaseUrl,
            string SupabaseAnonKey,
            string? SupabaseEmailRedirectUrl = null,
            string? UsdaApiKey = null,
            string? GeminiApiKey = null);
    }
}
