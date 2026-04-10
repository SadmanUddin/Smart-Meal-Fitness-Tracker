using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartMeal.core.Models;
namespace SmartMeal.core.Services
{
    public class MealService
    {
        private static readonly List<Meal> Meals = new List<Meal>();

        public void AddMeal(Meal meal)
        {
            Meals.Add(meal);
        }
        public List<Meal> GetMealsByUser(Guid userId)
        {
            var result = new List<Meal>(); //creating an empty list each time for each user
            foreach (var i in Meals) //using loop to filter the meals for specific user
            {
                if (i.UserId == userId) // Check if the meal belongs to the user
                {
                    result.Add(i);
                }
            }
            return result;
        }
        public  List<Meal> GetMeals()
        {
            return Meals;
        }

    }
}
