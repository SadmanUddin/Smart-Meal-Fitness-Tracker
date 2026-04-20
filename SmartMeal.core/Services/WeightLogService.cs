using SmartMeal.core.Models;
using Supabase;

namespace SmartMeal.core.Services
{
    public class WeightLogService
    {
        private readonly Client _client;

        public WeightLogService(Client client)
        {
            _client = client;
        }

        public async Task AddWeightLogAsync(string userId, decimal weightKg, string? notes = null)
        {
            var log = new WeightLog
            {
                UserId = userId,
                WeightKg = weightKg,
                LoggedAt = DateTime.UtcNow,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            };

            await _client.From<WeightLog>().Insert(log);
        }

        public async Task<List<WeightLog>> GetWeightLogsByUserAsync(string userId)
        {
            var result = await _client.From<WeightLog>()
                .Where(w => w.UserId == userId)
                .Order("logged_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            return result.Models;
        }

        public async Task<WeightLog?> GetLatestWeightLogAsync(string userId)
        {
            var logs = await GetWeightLogsByUserAsync(userId);
            return logs.FirstOrDefault();
        }

        public async Task UpdateWeightLogAsync(long weightLogId, decimal weightKg, string? notes = null)
        {
            var cleanedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

            await _client.From<WeightLog>()
                .Where(w => w.WeightLogId == weightLogId)
                .Set(w => w.WeightKg, weightKg)
                .Set(w => w.Notes!, cleanedNotes)
                .Update();
        }

        public async Task DeleteWeightLogAsync(long weightLogId)
        {
            await _client.From<WeightLog>()
                .Where(w => w.WeightLogId == weightLogId)
                .Delete();
        }
    }
}
