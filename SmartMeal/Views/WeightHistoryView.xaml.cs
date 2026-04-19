// WeightHistoryView shows the user's full weight history as a line graph
// and provides a form to log a new weigh-in.
//
// Layout:
//   Top right  — "Latest Weight" card (most recent entry)
//   Middle     — Log Weight inline form (weight kg + optional notes + button)
//   Bottom     — Line graph with 7 Days / 30 Days / All Time filter buttons
//
// Data source: public.weight_logs table in Supabase, fetched via WeightLogService.
// The chart is drawn directly on a WPF Canvas using Shapes (Polyline, Ellipse, Line)
// — no external charting library is required.
//
// The starting weight entered at registration appears as the first data point.
// Subsequent weigh-ins logged here appear as additional points on the graph.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SmartMeal.Helpers;
using SmartMeal.core.Models;
using SmartMeal.core.Services;

namespace SmartMeal.Views
{
    public partial class WeightHistoryView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly WeightLogService _weightLogService;
        private readonly GoalService _goalService;
        private readonly AuthService _authService;

        // All logs loaded from the DB, sorted oldest-first (for correct chart direction).
        private List<WeightLog> _allLogs = new();

        // The subset currently shown on the chart after the time filter is applied.
        private List<WeightLog> _displayedLogs = new();

        // 0 means "all time"; any other value is a day count cutoff from now.
        private int _filterDays = 0;

        // Null when the user has not set a weight goal yet.
        private decimal? _targetWeight = null;

        public WeightHistoryView()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _weightLogService = _mainWindow.WeightLogService;
            _goalService = _mainWindow.GoalService;
            _authService = _mainWindow.AuthService;
            Loaded += WeightHistoryView_Loaded;
        }

        // Fires once when the view is on screen. Immediately unsubscribes to prevent
        // double-loading if the control is ever detached and re-attached.
        private async void WeightHistoryView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= WeightHistoryView_Loaded;
            await LoadWeightLogsAsync();
        }

        // Fetches weight logs and the user's goal in parallel, then triggers a redraw.
        // GetWeightLogsByUserAsync returns rows in DESC order (newest first), so we reverse
        // them here to get oldest-first, which is the correct direction for a time-series graph.
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

        // Updates the "Target Weight" stat card in the header.
        private void UpdateTargetWeightLabel()
        {
            TargetWeightBlock.Text = _targetWeight.HasValue
                ? $"{_targetWeight.Value:F1} kg"
                : "—";
        }

        // Slices _allLogs based on the selected time filter and redraws the chart.
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

        // Updates the "Latest Weight" stat card in the top-right corner.
        private void UpdateLatestWeightLabel()
        {
            if (_allLogs.Count > 0)
            {
                var latest = _allLogs[^1]; // last element = most recent
                LatestWeightBlock.Text = $"{latest.WeightKg:F1} kg";
                LatestWeightDateBlock.Text = latest.LoggedAt.ToLocalTime().ToString("MMM d, yyyy");
            }
            else
            {
                LatestWeightBlock.Text = "—";
                LatestWeightDateBlock.Text = "No entries yet";
            }
        }

        // Fires when the user clicks "Log Weight".
        // Validates the weight field, inserts a row via WeightLogService, then reloads the chart.
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
                await LoadWeightLogsAsync(); // reload and redraw
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

        // --- Filter button handlers ---

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

        // Sets one filter button to "active" (blue) and resets the others to grey.
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

        // --- Chart drawing ---

        // Fires whenever the ChartAreaGrid changes size (including the initial layout pass).
        // This is the trigger for redrawing the chart with correct dimensions.
        private void ChartAreaGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawChart(_displayedLogs);
        }

        // Draws the weight line graph on WeightChartCanvas using WPF Shapes.
        //
        // The chart consists of:
        //   - Horizontal gridlines at even weight intervals
        //   - Y-axis labels (weight values)
        //   - X-axis labels (dates)
        //   - A subtle fill polygon under the line
        //   - A Polyline connecting all data points
        //   - Filled Ellipse dots at each data point
        //   - A weight label above each dot
        private void DrawChart(List<WeightLog> logs)
        {
            WeightChartCanvas.Children.Clear();

            // Use the parent Grid's size — Canvas doesn't report its own size until after a draw.
            double w = ChartAreaGrid.ActualWidth;
            double h = ChartAreaGrid.ActualHeight;

            // Not yet laid out — the SizeChanged event will fire again once it is.
            if (w < 20 || h < 20) return;

            if (logs.Count == 0)
            {
                // Show a friendly empty-state message centred in the canvas.
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

            // Padding around the chart area to leave room for axis labels.
            const double padL = 58;  // left: room for Y-axis labels
            const double padR = 20;
            const double padT = 20;
            const double padB = 40;  // bottom: room for X-axis date labels

            double chartW = w - padL - padR;
            double chartH = h - padT - padB;

            // --- Value ranges ---
            double minW = (double)logs.Min(l => l.WeightKg);
            double maxW = (double)logs.Max(l => l.WeightKg);

            // Extend the range to include the target weight so the dashed line is always visible.
            if (_targetWeight.HasValue)
            {
                double tw = (double)_targetWeight.Value;
                minW = Math.Min(minW, tw);
                maxW = Math.Max(maxW, tw);
            }

            double range = maxW - minW;
            if (range < 2) range = 2; // minimum range so a single data point isn't at the canvas edge

            // Add 12% padding above and below the data range so the line isn't clipped.
            double yMin = minW - range * 0.12;
            double yMax = maxW + range * 0.12;

            var minDate = logs.Min(l => l.LoggedAt);
            var maxDate = logs.Max(l => l.LoggedAt);
            double totalDays = Math.Max((maxDate - minDate).TotalDays, 1);

            // Convert a data value to a canvas pixel coordinate.
            double ToX(DateTime dt) => padL + (dt - minDate).TotalDays / totalDays * chartW;
            double ToY(double kg) => padT + (1.0 - (kg - yMin) / (yMax - yMin)) * chartH;

            // --- Horizontal gridlines + Y-axis labels ---
            const int gridCount = 5;
            for (int i = 0; i <= gridCount; i++)
            {
                double weight = yMin + (yMax - yMin) * i / gridCount;
                double y = ToY(weight);

                WeightChartCanvas.Children.Add(new Line
                {
                    X1 = padL, Y1 = y, X2 = padL + chartW, Y2 = y,
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

            // --- X-axis date labels ---
            // Show up to 7 labels spread evenly across the date range.
            // Skip labels that would overlap with the previous one (<50px gap).
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

            // --- Area fill (light blue polygon under the line) ---
            if (logs.Count > 1)
            {
                var areaPoints = new PointCollection();
                // Start at bottom-left of the first point, trace the line, close at bottom-right.
                areaPoints.Add(new Point(ToX(logs[0].LoggedAt), padT + chartH));
                foreach (var log in logs)
                    areaPoints.Add(new Point(ToX(log.LoggedAt), ToY((double)log.WeightKg)));
                areaPoints.Add(new Point(ToX(logs[^1].LoggedAt), padT + chartH));

                WeightChartCanvas.Children.Add(new Polygon
                {
                    Points = areaPoints,
                    Fill = new SolidColorBrush(Color.FromArgb(25, 37, 99, 235)), // very translucent blue
                    Stroke = Brushes.Transparent
                });
            }

            // --- Target weight dashed line (amber) ---
            if (_targetWeight.HasValue)
            {
                double ty = ToY((double)_targetWeight.Value);
                var targetLine = new Line
                {
                    X1 = padL,
                    Y1 = ty,
                    X2 = padL + chartW,
                    Y2 = ty,
                    Stroke = new SolidColorBrush(Color.FromRgb(245, 158, 11)), // amber
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

            // --- Line ---
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

            // --- Data points (dots + weight labels) ---
            foreach (var log in logs)
            {
                double x = ToX(log.LoggedAt);
                double y = ToY((double)log.WeightKg);

                // White-bordered filled circle
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

                // Weight value label above the dot
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

        // --- Sidebar navigation ---

        private void Dashboard_Click(object sender, RoutedEventArgs e) =>
            _mainWindow.Navigate(new DashboardView());

        private void Meals_Click(object sender, RoutedEventArgs e) =>
            _mainWindow.Navigate(new MealsView());

        private void Activities_Click(object sender, RoutedEventArgs e) =>
            _mainWindow.Navigate(new AddActivityView());

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
