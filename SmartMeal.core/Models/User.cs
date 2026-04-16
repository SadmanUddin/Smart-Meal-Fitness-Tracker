// Represents a row in the public.users table.
// Every user who registers gets one row here, linked to their Supabase Auth account
// by sharing the same UUID as the primary key.
//
// The [Table] attribute tells the Supabase ORM which table this class maps to.
// The [Column] attributes map each C# property to its database column name.
// BaseModel is a Supabase SDK base class that provides change-tracking for updates.

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
        // The second parameter (false) on [PrimaryKey] tells the ORM NOT to auto-generate
        // this value — we supply it ourselves from the Auth response.
        [PrimaryKey("id", false)]
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

        // Optional profile fields — not collected at registration yet.
        // These columns exist in the database and the model is ready for them,
        // but there is no profile-editing UI in the app at this time.
        [Column("age")]
        public int? Age { get; set; }

        [Column("height_cm")]
        public decimal? HeightCm { get; set; }

        [Column("weight_kg")]
        public decimal? WeightKg { get; set; }

        // Must be "male", "female", or "other" — the DB has a check constraint.
        // Nullable because we do not collect it during registration.
        [Column("gender")]
        public string? Gender { get; set; }

        // Set automatically by the database (DEFAULT now()), but we also set it in C#
        // when inserting so the in-memory object has a valid timestamp immediately.
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
