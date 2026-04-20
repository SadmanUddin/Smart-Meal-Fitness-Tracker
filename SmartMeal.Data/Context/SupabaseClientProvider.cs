// SupabaseClientProvider is responsible for creating and initialising the single Supabase client
// that the entire application shares. Think of it as the database connection factory —
// it reads the credentials, configures the connection options, and hands the ready client
// back to whoever needs it (currently MainWindow, which passes it to every service).

using Supabase;

namespace SmartMeal.Data.Context
{
    public class SupabaseClientProvider
    {
        // The live Supabase client. Once InitializeAsync() has been called this is ready
        // to execute queries against the PostgreSQL database via the PostgREST API.
        private readonly Client _client;

        // Creates the client with the project URL and anonymous key from supabase.config.json.
        // AutoRefreshToken = true means the JWT session token is automatically renewed before
        // it expires, so users are not silently logged out mid-session.
        // AutoConnectRealtime = false because this app does not need live push updates —
        // it reads data on demand rather than subscribing to changes.
        public SupabaseClientProvider(string url, string anonKey)
        {
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };
            _client = new Client(url, anonKey, options);
        }

        // Must be called before the client is used. This performs the actual handshake with
        // Supabase — resolving the URL, loading the schema, and setting up the auth layer.
        // If this fails (bad URL, wrong key, no internet), the app will not start.
        public async Task InitializeAsync()
        {
            await _client.InitializeAsync();
        }

        // Exposes the ready client so MainWindow can pass it into each service constructor.
        public Client Client => _client;
    }
}
