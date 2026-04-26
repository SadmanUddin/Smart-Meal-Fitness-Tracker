// AdminService provides admin-only database operations.
//
// All methods rely on Supabase RLS policies that gate access to users whose
// own users row has role = 'admin'. A regular user's JWT will be rejected by
// the policy, so these queries simply return no data or throw for non-admins.
//
// Operations:
//   GetAllUsersAsync   — returns every row in public.users (requires admin RLS)
//   SetBannedAsync     — sets is_banned on a single user row (requires admin RLS)
//   GetTotalMealLogsCountAsync   — total meal_log rows across all users
//   GetTotalActivitiesCountAsync — total activity rows across all users

using SmartMeal.core.Models;
using Supabase;

namespace SmartMeal.core.Services
{
    public class AdminService
    {
        private readonly Client _client;

        public AdminService(Client client)
        {
            _client = client;
        }

        // Returns all users ordered newest-first.
        // The admin RLS policy must exist on public.users:
        //   CREATE POLICY users_select_all_admin
        //   ON public.users FOR SELECT
        //   USING (public.is_current_user_admin());
        public async Task<List<User>> GetAllUsersAsync()
        {
            var result = await _client.From<User>()
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models;
        }

        // Flips the is_banned flag on a single user row.
        // Uses a fetch-then-update pattern (same as GoalService) because the SDK
        // requires a full model instance to run an UPDATE.
        public async Task SetBannedAsync(string userId, bool banned)
        {
            var result = await _client.From<User>().Where(u => u.Id == userId).Get();
            var user = result.Models.FirstOrDefault()
                ?? throw new InvalidOperationException($"User {userId} not found.");
            user.IsBanned = banned;
            await _client.From<User>().Where(u => u.Id == userId).Update(user);
        }

        // Counts all rows in meal_logs across all users.
        // Requires a corresponding admin SELECT policy on public.meal_logs.
        public async Task<int> GetTotalMealLogsCountAsync()
        {
            return await _client
                .From<MealLog>()
                .Count(Supabase.Postgrest.Constants.CountType.Exact);
        }

        // Counts all rows in activities across all users.
        // Requires a corresponding admin SELECT policy on public.activities.
        public async Task<int> GetTotalActivitiesCountAsync()
        {
            return await _client
                .From<Activity>()
                .Count(Supabase.Postgrest.Constants.CountType.Exact);
        }
        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync()
        {
            // Load all required data in parallel
            var usersTask = _client.From<User>().Get();
            var mealsTask = _client.From<MealLog>().Get();
            var activitiesTask = _client.From<Activity>().Get();
            var weightsTask = _client.From<WeightLog>().Get();

            await Task.WhenAll(usersTask, mealsTask, activitiesTask, weightsTask);

            var users = usersTask.Result.Models;
            var meals = mealsTask.Result.Models;
            var activities = activitiesTask.Result.Models;
            var weights = weightsTask.Result.Models;

            var leaderboard = new List<LeaderboardEntry>();

            foreach (var user in users)
            {
                var userMeals = meals.Where(m => m.UserId == user.Id).ToList();
                var userActivities = activities.Where(a => a.UserId == user.Id).ToList();
                var userWeights = weights.Where(w => w.UserId == user.Id).ToList();

                // Unique active days (consistency metric)
                var activeDays = userMeals.Select(m => m.LogDate)
                    .Concat(userActivities.Select(a => a.LoggedAt.Date.ToString("yyyy-MM-dd")))
                    .Concat(userWeights.Select(w => w.LoggedAt.Date.ToString("yyyy-MM-dd")))
                    .Distinct()
                    .Count();

                int mealCount = userMeals.Count;
                int activityCount = userActivities.Count;
                int weightCount = userWeights.Count;

                // OPTIONAL anti-spam cap (recommended)
                mealCount = Math.Min(mealCount, activeDays * 5);
                activityCount = Math.Min(activityCount, activeDays * 3);

                int score =
                    (activeDays * 3) +
                    (mealCount * 1) +
                    (activityCount * 2) +
                    (weightCount * 2);

                leaderboard.Add(new LeaderboardEntry
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    ActiveDays = activeDays,
                    MealLogs = mealCount,
                    ActivityLogs = activityCount,
                    WeightLogs = weightCount,
                    Score = score
                });
            }

            // Rank users by score
            var ranked = leaderboard
                .OrderByDescending(x => x.Score)
                .Select((user, index) =>
                {
                    user.Rank = index + 1;
                    return user;
                })
                .ToList();

            return ranked;
        }
    }
}
