using SmartMeal.core.Models;
using Supabase;

namespace SmartMeal.core.Services
{
    public class ActService
    {
        private readonly Client _client;

        public ActService(Client client)
        {
            _client = client;
        }

        public async Task AddActivityAsync(Activity activity)
        {
            await _client.From<Activity>().Insert(activity);
        }

        public async Task<List<Activity>> GetActivitiesByUserAsync(string userId)
        {
            var result = await _client.From<Activity>()
                .Where(a => a.UserId == userId)
                .Order("logged_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();
            return result.Models;
        }
    }
}
