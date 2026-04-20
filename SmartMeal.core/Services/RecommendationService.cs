using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.GenAI;

namespace SmartMeal.core.Services
{
    public class RecommendationService
    {
        private readonly Client client;
        public RecommendationService()
        {
            client = new Client();
        }

        public async Task<string> GetRecommendationAsync(int calorieGoal,int caloriesConsumed,int caloriesBurned,int mealsCount,int activitesCount)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine($"You are a health recommendation assistant.");
            prompt.AppendLine($"Give a short and practical recommendation.");
            prompt.AppendLine($"Do not use markdown.");
            prompt.AppendLine();
            prompt.AppendLine($"Daily calorie goal:{calorieGoal}");
            prompt.AppendLine($"Calories consumed today:{caloriesConsumed}");
            prompt.AppendLine($"Calories burned today:{caloriesBurned}");
            prompt.AppendLine($"Number of meals added today:{mealsCount}");
            prompt.AppendLine($"Number of activities today:{activitesCount}");
            prompt.AppendLine();
            prompt.AppendLine("Based on the above information, what is your recommendation for the user to stay on track with their health goals?");

            var response = await client.Models.GenerateContentAsync(
            
                model : "models/gemini-2.5-flash",
                contents : prompt.ToString()
            );
                return response?.Text ?? "No response";
        }

    }
}
