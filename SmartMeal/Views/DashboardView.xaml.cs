using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly MealService mealService;
        private void LoadMeals()
        {
            var meals = mealService.GetMeals();
            MealsCountBlock.Text = meals.Count.ToString();
            int totalCalories = 0;
            foreach (var i in meals)
            {
                totalCalories += i.Calories;
            }
            CaloriesGoalBlock.Text = totalCalories.ToString();
            if (meals.Count > 0)
            {
                var latestMeal = meals[meals.Count - 1];
                RecentMealsTextBlock.Text = $"{latestMeal.Name} - {latestMeal.Calories} cal";

            }
            else
            {
                RecentMealsTextBlock.Text = "No meals added yet.";
            }
        }
        public DashboardView()
        {
            InitializeComponent();
            mealService = ((MainWindow)Application.Current.MainWindow).MealService;
            LoadMeals();//calling the methond to load the meals when the dashboard
        }
        private void AddMeal_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Navigate(new AddMealView());
        }
    }
}
