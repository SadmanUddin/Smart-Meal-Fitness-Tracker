using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMeal.core.Models
{
    public class LeaderboardEntry
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; }=string.Empty;
        public int ActiveDays { get; set; }
        public int MealLogs { get; set; }
        public int ActivityLogs { get; set; }
        public int WeightLogs { get; set; }
        public int Score { get; set; }
        public int Rank { get; set; }

    }
}
