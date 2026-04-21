using SmartMeal.core.Models;
using Supabase;

namespace SmartMeal.core.Services
{
    // ActService handles activity persistence in Supabase.
    // It writes to and reads from the public.activities table.
    public class ActService
    {
        // Shared Supabase client initialized in MainWindow and passed into services.
        private readonly Client _client;

        public ActService(Client client)
        {
            _client = client;
        }

        // Inserts one activity row for the current user.
        public async Task AddActivityAsync(Activity activity)
        {
            await _client.From<Activity>().Insert(activity);
        }

        // Returns only activities belonging to the provided user, ordered by timestamp.
        public async Task<List<Activity>> GetActivitiesByUserAsync(string userId)
        {
            var result = await _client.From<Activity>()
                .Where(a => a.UserId == userId)
                .Order("logged_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();
            return result.Models;
        }
        public async Task DeleteActivityAsync(long activityId)
        {
            await _client.From<Activity>()
                .Where(a => a.ActivityId == activityId)
                .Delete();
        }
    }
}
