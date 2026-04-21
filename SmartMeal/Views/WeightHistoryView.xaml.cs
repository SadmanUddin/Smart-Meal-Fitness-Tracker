using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SmartMeal.Helpers;
using SmartMeal.core.Models;
using SmartMeal.core.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace SmartMeal.Views
{
    public partial class WeightHistoryView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly WeightLogService _weightLogService;
        private readonly GoalService _goalService;
        private readonly AuthService _authService;
        private readonly MealService _mealService;
        private readonly ActService _activityService;
        private readonly FoodService _foodService;

        private List<WeightLog> _allLogs = new();
        private List<WeightLog> _displayedLogs = new();
        private int _filterDays = 0;
        private decimal? _targetWeight = null;

        public ISeries[] CalorieSeries { get; set; } = Array.Empty<ISeries>();
        public Axis[] CalorieXAxes { get; set; } = Array.Empty<Axis>();
        public Axis[] CalorieYAxes { get; set; } = Array.Empty<Axis>();

        public WeightHistoryView()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _weightLogService = _mainWindow.WeightLogService;
            _goalService = _mainWindow.GoalService;
            _authService = _mainWindow.AuthService;
            _mealService = _mainWindow.MealService;
            _activityService = _mainWindow.ActService;
            _foodService = _mainWindow.FoodService;

            DataContext = this;
            Loaded += WeightHistoryView_Loaded;
        }

        private async void WeightHistoryView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= WeightHistoryView_Loaded;
            await LoadWeightLogsAsync();
            await LoadCalorieChartAsync();
        }

        private async Task LoadWeightLogsAsync()
        {
            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
                return;

            try
            {
                var logsTask = _weightLogService.GetWeightLogsByUserAsync(userId);
                var goalTask = _goalService.GetGoalAsync(userId);
                await Task.WhenAll(logsTask, goalTask);

                _allLogs = logsTask.Result.OrderBy(l => l.LoggedAt).ToList();
                _targetWeight = goalTask.Result?.TargetWeightKg;

                UpdateTargetWeightLabel();
                ApplyFilter();
                UpdateLatestWeightLabel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load weight history: {ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task LoadCalorieChartAsync()
        {
            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
                return;

            try
            {
                var logs = await _mealService.GetAllLogsAsync(userId);
                var activities = await _activityService.GetActivitiesByUserAsync(userId);
                var foods = await _foodService.GetAccessibleFoodsAsync(userId);

                var foodCaloriesById = new Dictionary<long, decimal>();

                foreach (var food in foods)
                {
                    foodCaloriesById[food.FoodId] = food.CaloriesPer100g;
                }

                var consumedByDate = new Dictionary<string, double>();
                var burnedByDate = new Dictionary<string, double>();

                // Calculate consumed calories from meal logs
                foreach (var log in logs)
                {
                    string date = log.LogDate;

                    if (!consumedByDate.ContainsKey(date))
                        consumedByDate[date] = 0;

                    if (foodCaloriesById.TryGetValue(log.FoodId, out var caloriesPer100g))
                    {
                        double totalCalories =
                            (double)(caloriesPer100g * log.Grams / 100m);

                        consumedByDate[date] += totalCalories;
                    }
                }

                // Calculate burned calories from activities
                foreach (var activity in activities)
                {
                    string date = activity.LoggedAt
                        .ToLocalTime()
                        .ToString("yyyy-MM-dd");

                    if (!burnedByDate.ContainsKey(date))
                        burnedByDate[date] = 0;

                    burnedByDate[date] += activity.CaloriesBurned;
                }

                // Merge all dates
                var allDates = consumedByDate.Keys
                    .Union(burnedByDate.Keys)
                    .OrderBy(d => d)
                    .ToList();

                var consumedValues = new List<double>();
                var burnedValues = new List<double>();
                var labels = new List<string>();

                foreach (var date in allDates)
                {
                    labels.Add(DateTime.Parse(date).ToString("MMM d"));

                    consumedValues.Add(
                        consumedByDate.ContainsKey(date)
                            ? consumedByDate[date]
                            : 0
                    );

                    burnedValues.Add(
                        burnedByDate.ContainsKey(date)
                            ? burnedByDate[date]
                            : 0
                    );
                }

                CalorieSeries = new ISeries[]
                {
            new LineSeries<double>
            {
                Values = consumedValues,
                Name = "Calories Consumed"
            },

            new LineSeries<double>
            {
                Values = burnedValues,
                Name = "Calories Burned"
            }
                };

                CalorieXAxes = new Axis[]
                {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 15
            }
                };

                CalorieYAxes = new Axis[]
                {
            new Axis
            {
                Name = "Calories"
            }
                };
                CalorieChart.Series = CalorieSeries;
                CalorieChart.XAxes = CalorieXAxes;
                CalorieChart.YAxes = CalorieYAxes;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load calorie chart: {ex.Message}",
                    "Chart Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UpdateTargetWeightLabel()
        {
            TargetWeightBlock.Text = _targetWeight.HasValue
                ? $"{_targetWeight.Value:F1} kg"
                : "—";
        }

        private void ApplyFilter()
        {
            if (_filterDays == 0)
                _displayedLogs = _allLogs.ToList();
            else
            {
                var cutoff = DateTime.UtcNow.AddDays(-_filterDays);
                _displayedLogs = _allLogs.Where(l => l.LoggedAt >= cutoff).ToList();
            }

            DrawChart(_displayedLogs);
        }

        private void UpdateLatestWeightLabel()
        {
            if (_allLogs.Count > 0)
            {
                var latest = _allLogs[^1];
                LatestWeightBlock.Text = $"{latest.WeightKg:F1} kg";
                LatestWeightDateBlock.Text = latest.LoggedAt.ToLocalTime().ToString("MMM d, yyyy");
            }
            else
            {
                LatestWeightBlock.Text = "—";
                LatestWeightDateBlock.Text = "No entries yet";
            }
        }

        private async void LogWeight_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(WeightInputTextBox.Text.Trim(), out decimal weight) || weight <= 0)
            {
                MessageBox.Show("Please enter a valid weight in kg (e.g. 75.5).");
                return;
            }

            if (!SessionHelper.TryGetCurrentUserId(_authService, out var userId))
            {
                MessageBox.Show("No user logged in.");
                return;
            }

            try
            {
                var notes = NotesTextBox.Text.Trim();

                if (string.IsNullOrEmpty(notes))
                    await _weightLogService.AddWeightLogAsync(userId, weight, null);
                else
                    await _weightLogService.AddWeightLogAsync(userId, weight, notes);

                WeightInputTextBox.Clear();
                NotesTextBox.Clear();
                await LoadWeightLogsAsync();
                await LoadCalorieChartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not log weight: {ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Filter7Days_Click(object sender, RoutedEventArgs e)
        {
            _filterDays = 7;
            HighlightFilterButton(Filter7DaysButton);
            ApplyFilter();
        }

        private void Filter30Days_Click(object sender, RoutedEventArgs e)
        {
            _filterDays = 30;
            HighlightFilterButton(Filter30DaysButton);
            ApplyFilter();
        }

        private void FilterAllTime_Click(object sender, RoutedEventArgs e)
        {
            _filterDays = 0;
            HighlightFilterButton(FilterAllTimeButton);
            ApplyFilter();
        }

        private void HighlightFilterButton(Button active)
        {
            foreach (var btn in new[] { Filter7DaysButton, Filter30DaysButton, FilterAllTimeButton })
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(229, 231, 235));
                btn.Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39));
            }

            active.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            active.Foreground = Brushes.White;
        }

        private void ChartAreaGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawChart(_displayedLogs);
        }

        private void DrawChart(List<WeightLog> logs)
        {
            WeightChartCanvas.Children.Clear();

            double w = ChartAreaGrid.ActualWidth;
            double h = ChartAreaGrid.ActualHeight;

            if (w < 20 || h < 20) return;

            if (logs.Count == 0)
            {
                var msg = new TextBlock
                {
                    Text = "No weight data for this period.\nLog your first weigh-in above.",
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Width = w * 0.5
                };

                Canvas.SetLeft(msg, w * 0.25);
                Canvas.SetTop(msg, h / 2 - 20);
                WeightChartCanvas.Children.Add(msg);
                return;
            }

            const double padL = 58;
            const double padR = 20;
            const double padT = 20;
            const double padB = 40;

            double chartW = w - padL - padR;
            double chartH = h - padT - padB;

            double minW = (double)logs.Min(l => l.WeightKg);
            double maxW = (double)logs.Max(l => l.WeightKg);

            if (_targetWeight.HasValue)
            {
                double tw = (double)_targetWeight.Value;
                minW = Math.Min(minW, tw);
                maxW = Math.Max(maxW, tw);
            }

            double range = maxW - minW;
            if (range < 2) range = 2;

            double yMin = minW - range * 0.12;
            double yMax = maxW + range * 0.12;

            var minDate = logs.Min(l => l.LoggedAt);
            var maxDate = logs.Max(l => l.LoggedAt);
            double totalDays = Math.Max((maxDate - minDate).TotalDays, 1);

            double ToX(DateTime dt) => padL + (dt - minDate).TotalDays / totalDays * chartW;
            double ToY(double kg) => padT + (1.0 - (kg - yMin) / (yMax - yMin)) * chartH;

            const int gridCount = 5;
            for (int i = 0; i <= gridCount; i++)
            {
                double weight = yMin + (yMax - yMin) * i / gridCount;
                double y = ToY(weight);

                WeightChartCanvas.Children.Add(new Line
                {
                    X1 = padL,
                    Y1 = y,
                    X2 = padL + chartW,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                    StrokeThickness = 1
                });

                var yLabel = new TextBlock
                {
                    Text = weight.ToString("F1"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
                };

                Canvas.SetLeft(yLabel, 2);
                Canvas.SetTop(yLabel, y - 8);
                WeightChartCanvas.Children.Add(yLabel);
            }

            int xLabelTarget = Math.Min(logs.Count, 7);
            var usedX = new List<double>();

            for (int i = 0; i < xLabelTarget; i++)
            {
                int idx;
                if (logs.Count == 1)
                {
                    idx = 0;
                }
                else
                {
                    idx = (int)Math.Round((double)i / (xLabelTarget - 1) * (logs.Count - 1));
                }

                idx = Math.Clamp(idx, 0, logs.Count - 1);

                double x = ToX(logs[idx].LoggedAt);
                if (usedX.Any(px => Math.Abs(px - x) < 50)) continue;
                usedX.Add(x);

                var xLabel = new TextBlock
                {
                    Text = logs[idx].LoggedAt.ToLocalTime().ToString("MMM d"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
                };

                Canvas.SetLeft(xLabel, x - 22);
                Canvas.SetTop(xLabel, padT + chartH + 8);
                WeightChartCanvas.Children.Add(xLabel);
            }

            if (logs.Count > 1)
            {
                var areaPoints = new PointCollection();
                areaPoints.Add(new Point(ToX(logs[0].LoggedAt), padT + chartH));

                foreach (var log in logs)
                    areaPoints.Add(new Point(ToX(log.LoggedAt), ToY((double)log.WeightKg)));

                areaPoints.Add(new Point(ToX(logs[^1].LoggedAt), padT + chartH));

                WeightChartCanvas.Children.Add(new Polygon
                {
                    Points = areaPoints,
                    Fill = new SolidColorBrush(Color.FromArgb(25, 37, 99, 235)),
                    Stroke = Brushes.Transparent
                });
            }

            if (_targetWeight.HasValue)
            {
                double ty = ToY((double)_targetWeight.Value);

                var targetLine = new Line
                {
                    X1 = padL,
                    Y1 = ty,
                    X2 = padL + chartW,
                    Y2 = ty,
                    Stroke = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                    StrokeThickness = 1.8,
                    StrokeDashArray = new DoubleCollection { 6, 4 }
                };

                WeightChartCanvas.Children.Add(targetLine);

                var targetLabel = new TextBlock
                {
                    Text = $"Target {_targetWeight.Value:F1} kg",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11))
                };

                Canvas.SetLeft(targetLabel, padL + 4);
                Canvas.SetTop(targetLabel, ty - 16);
                WeightChartCanvas.Children.Add(targetLabel);
            }

            if (logs.Count > 1)
            {
                var line = new Polyline
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                    StrokeThickness = 2.5,
                    StrokeLineJoin = PenLineJoin.Round
                };

                foreach (var log in logs)
                    line.Points.Add(new Point(ToX(log.LoggedAt), ToY((double)log.WeightKg)));

                WeightChartCanvas.Children.Add(line);
            }

            foreach (var log in logs)
            {
                double x = ToX(log.LoggedAt);
                double y = ToY((double)log.WeightKg);

                var dot = new Ellipse
                {
                    Width = 9,
                    Height = 9,
                    Fill = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                };

                Canvas.SetLeft(dot, x - 4.5);
                Canvas.SetTop(dot, y - 4.5);
                WeightChartCanvas.Children.Add(dot);

                var label = new TextBlock
                {
                    Text = $"{log.WeightKg:F1}",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235))
                };

                Canvas.SetLeft(label, x - 14);
                Canvas.SetTop(label, y - 22);
                WeightChartCanvas.Children.Add(label);
            }
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e) =>
            _mainWindow.Navigate(new DashboardView());

        private void Meals_Click(object sender, RoutedEventArgs e) =>
            _mainWindow.Navigate(new MealsView());

        private void Activities_Click(object sender, RoutedEventArgs e) =>
            _mainWindow.Navigate(new ActivitiesView());

        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new ProfileView());
        }

        private void Recommendations_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Navigate(new RecommendationsView());
        }

        private async void BackToLog_Click(object sender, RoutedEventArgs e)
        {
            await _authService.SignOutAsync();
            _mainWindow.Navigate(new LoginView());
        }
    }
}