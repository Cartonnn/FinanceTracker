namespace FinanceTracker;

public class Expenditure
{
    public decimal Amount { get; }
    public string Category { get; }
    public bool IsIncome { get; }
    public DateTime Date { get; } = DateTime.Now;

    public Expenditure(decimal amount, string category, bool isIncome)
    {
        Amount = amount;
        Category = category;
        IsIncome = isIncome;
    }
    public Expenditure(decimal amount, string category, bool isIncome, DateTime date)
    {
        Amount = amount;
        Category = category;
        IsIncome = isIncome;
        Date = date;
    }
}