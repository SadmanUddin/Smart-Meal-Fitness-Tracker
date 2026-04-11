using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartMeal.core.Models;
namespace SmartMeal.core.Services
{
    public class ActService
    {
        private static readonly List<Activity> Activities = new List<Activity>();

        public void AddActivity(Activity activity)
        {
            Activities.Add(activity);
        }
        public List<Activity> GetActivities()
        {
            return Activities;
        }

        public List<Activity> GetActivitiesByUser(Guid userId)
            {
                var result = new List<Activity>(); //creating an empty list each time for each user
                foreach (var i in Activities) //using loop to filter the meals for specific user
                {
                    if (i.UserId == userId) // Check if the meal belongs to the user
                    {
                        result.Add(i);
                    }
                }
                return result;
        }
    }
}
