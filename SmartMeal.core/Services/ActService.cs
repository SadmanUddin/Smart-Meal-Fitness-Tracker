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

        public async Task<SortedDictionary<string, int>> GetLast7DaysBurnedCaloriesAsync(string userId)
        {
            var activities = await _client
                .From<Activity>()
                .Where(a => a.UserId == userId)
                .Get();

            var result = new SortedDictionary<string, int>();

            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i).ToString("yyyy-MM-dd");
                int total = 0;

                foreach (var activity in activities.Models)
                {
                    var activityDate = activity.LoggedAt.Date.ToString("yyyy-MM-dd");

                    if (activityDate == date)
                    {
                        total += activity.CaloriesBurned;
                    }
                }

                result[date] = total;
            }

            return result;
        }
    }
}
