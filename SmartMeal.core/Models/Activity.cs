using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace SmartMeal.core.Models
{
    [Table("activities")]
    public class Activity : BaseModel
    {
        [PrimaryKey("activity_id")]
        public long ActivityId { get; set; }

        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("calories_burned")]
        public int CaloriesBurned { get; set; }

        [Column("duration_minutes")]
        public int DurationMinutes { get; set; }

        [Column("logged_at")]
        public DateTime LoggedAt { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }
    }
}
