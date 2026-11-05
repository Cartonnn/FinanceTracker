using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace FinanceTracker;

public partial class MainPage : ContentPage
{
    public ObservableCollection<Expenditure> Expenses { get; } = new();

    private bool _ascending = true;
    private decimal budget;

    private readonly string path = """C:\Users\Student\Documents\Jakub Bijata\FinanceTracker\FinanceTracker\Resources\Finances\Finances.csv""";

    public MainPage()
    {
        InitializeComponent();
        expenditureView.ItemsSource = Expenses;
        motyw.SelectedIndex = Preferences.Get("motyw", 1);

        budget = (decimal)Preferences.Get("budget", 0.0);
        ReadFromCsv(path);
        Update();

        Loaded += (s, e) => AskForBudget();
    }

    private void OnChangeBudgetClicked(object sender, EventArgs e)
    {
        AskForBudget();
    }

    private async void AskForBudget()
    {
        string input = await DisplayPromptAsync("Budżet", "Podaj swój budżet (zł):",
            "OK", "Anuluj", "np. 2000", keyboard: Keyboard.Numeric);

        if (!decimal.TryParse(input, out decimal value) || value <= 0)
            return;

        budget = value;
        Preferences.Set("budget", (double)budget);
        Update();
        CheckBudget();
    }

    private void CheckBudget()
    {
        decimal spent = Expenses.Sum(x => x.Amount);

        if (budget > 0 && spent > budget)
            DisplayAlert("Uwaga!", $"Przekroczono budżet o {spent - budget:N2} zł", "OK");
    }

    private void Update()
    {
        decimal spent = Expenses.Sum(x => x.Amount);
        decimal remaining = budget - spent;

        budgetLabel.Text = $"Budżet: {budget:N2} zł";
        spentLabel.Text = $"Wydano: {spent:N2} zł";

        if (remaining >= 0)
        {
            remainingLabel.Text = $"Pozostało: {remaining:N2} zł";
            remainingLabel.TextColor = Colors.SeaGreen;
        }
        else
        {
            remainingLabel.Text = $"Przekroczono o: {-remaining:N2} zł";
            remainingLabel.TextColor = Colors.Red;
        }

        double ratio = budget > 0 ? (double)(spent / budget) : 0;
        budgetProgress.Progress = Math.Min(ratio, 1);

        if (ratio >= 1) budgetProgress.ProgressColor = Colors.Red;
        else if (ratio >= 0.8) budgetProgress.ProgressColor = Colors.Orange;
        else budgetProgress.ProgressColor = Colors.SeaGreen;

        UpdateChart(remaining);
    }

    private void UpdateChart(decimal remaining)
    {
        var series = Expenses
            .GroupBy(x => x.Category)
            .Select(g =>
            {
                double sum = (double)g.Sum(x => x.Amount);
                string text = $"{sum:N2} zł";

                return new PieSeries<double>
                {
                    Name = g.Key,
                    Values = new[] { sum },
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsPosition = PolarLabelsPosition.Middle,
                    DataLabelsFormatter = _ => text,
                    ToolTipLabelFormatter = _ => text
                };
            })
            .ToList();

        
        if (remaining > 0)
        {
            string text = $"{remaining:N2} zł";

            series.Add(new PieSeries<double>
            {
                Name = "Pozostały budżet",
                Values = new[] { (double)remaining },
                Fill = new SolidColorPaint(SKColors.SeaGreen),
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsPosition = PolarLabelsPosition.Middle,
                DataLabelsFormatter = _ => text,
                ToolTipLabelFormatter = _ => text
            });
        }

        pieChart.Series = series.ToArray();
    }

    private void ChangeTheme(object sender, EventArgs e)
    {
        if (Application.Current == null || motyw.SelectedIndex < 0)
            return;

        Application.Current.UserAppTheme = motyw.SelectedIndex switch
        {
            0 => AppTheme.Light,
            1 => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        Preferences.Set("motyw", motyw.SelectedIndex);
    }

    private void SaveToCsv(string path)
    {
        try
        {
            var lines = new List<string> { "Amount;Category;Date;IsIncome" };

            lines.AddRange(Expenses.Select(x =>
                $"{x.Amount};{x.Category};{x.Date:yyyy-MM-dd HH:mm:ss};{x.IsIncome}"));

            File.WriteAllLines(path, lines);
        }
        catch (Exception ex)
        {
            DisplayAlert("Błąd", ex.Message, "OK");
        }
    }

    private void ReadFromCsv(string path)
    {
        if (!File.Exists(path)) return;

        try
        {
            foreach (string line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line == "Amount;Category;Date;IsIncome") continue;

                string[] czesci = line.Split(';');

                // przychodow juz nie uzywamy, pomijamy
                if (bool.Parse(czesci[3])) continue;

                Expenses.Add(new Expenditure(
                    decimal.Parse(czesci[0]),
                    czesci[1],
                    false,
                    DateTime.Parse(czesci[2])
                ));
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("Błąd", ex.Message, "OK");
        }
    }

    private void AddExpenditure(object sender, EventArgs e)
    {
        if (budget == 0)
        {
            DisplayAlert("Błąd", "Najpierw ustaw budżet", "OK");
            return;
        }

        if (!decimal.TryParse(expance.Text, out decimal value) || value <= 0)
        {
            DisplayAlert("Błąd", "Podaj poprawną kwotę", "OK");
            return;
        }

        string categoryText = string.IsNullOrWhiteSpace(category.Text) ? "Inne" : category.Text.Trim();

        Expenses.Add(new Expenditure(value, categoryText, false));
        SaveToCsv(path);
        Update();

        expance.Text = string.Empty;
        category.Text = string.Empty;

        CheckBudget();
    }

    private void OnSortClicked(object sender, EventArgs e)
    {
        string column = (string)((Button)sender).CommandParameter;

        List<Expenditure> sorted = column switch
        {
            "Amount" => Expenses.OrderBy(x => x.Amount).ToList(),
            "Category" => Expenses.OrderBy(x => x.Category).ToList(),
            _ => Expenses.OrderBy(x => x.Date).ToList()
        };

        if (!_ascending)
            sorted.Reverse();

        _ascending = !_ascending;

        Expenses.Clear();
        foreach (var item in sorted)
            Expenses.Add(item);
    }
}