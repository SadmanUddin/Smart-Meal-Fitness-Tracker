
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace SmartMeal.core.Models
{
    [Table("users")]
    public class User : BaseModel
    {
        // The primary key — a UUID string that comes directly from Supabase Auth.
        // When a user registers, Supabase Auth creates an auth account and returns a UUID.
        // We use that same UUID here so the two systems stay in sync.
        // Set shouldInsert = true so Postgrest sends this value on INSERT.
        // This table's id is NOT database-generated; it must match auth.uid().
        [PrimaryKey("id", true)]
        public string Id { get; set; } = string.Empty;

        // The user's display name, collected on the registration form.
        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        // Used for login — must be unique across all users (enforced by the DB).
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        // Either "user" (default) or "admin". Currently only "user" is assigned.
        // Could be used in future to restrict admin-only features.
        [Column("role")]
        public string Role { get; set; } = "user";

        // Optional profile fields — collected on the registration form.
        // All four are optional; the user can leave them blank and update later.
        // Age, HeightCm, and WeightKg are numeric (nullable).
        // Gender must be "male", "female", or "other" (DB check constraint), or null.
        [Column("age")]
        public int? Age { get; set; }

        [Column("height_cm")]
        public decimal? HeightCm { get; set; }

        // Snapshot of weight at registration time. Ongoing weight tracking uses
        // the weight_logs table (see WeightLog.cs) — that is the authoritative history.
        [Column("weight_kg")]
        public decimal? WeightKg { get; set; }

        [Column("gender")]
        public string? Gender { get; set; }

        // Set automatically by the database (DEFAULT now()), but we also set it in C#
        // when inserting so the in-memory object has a valid timestamp immediately.
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Set to true by an admin to prevent the user from signing in.
        // Defaults to false (DB DEFAULT false). Read at login — banned users are
        // shown an error and their session is immediately invalidated.
        [Column("is_banned")]
        public bool IsBanned { get; set; }

        // Comma-separated dietary preference tags, e.g. "Vegetarian,Keto,High-Protein".
        // Used by GeminiService when building the meal recommendation prompt.
        [Column("food_preferences")]
        public string FoodPreferences { get; set; } = string.Empty;

        // Comma-separated allergy tags, e.g. "Nuts,Dairy,Gluten".
        // Used by GeminiService to exclude allergens from recommendations.
        [Column("allergies")]
        public string Allergies { get; set; } = string.Empty;
    }
}
