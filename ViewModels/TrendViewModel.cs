using System.ComponentModel.DataAnnotations;

namespace CurrencyMini.ViewModels;

public record TrendPoint(DateOnly Date, decimal Rate, decimal Value);

public class TrendViewModel
{
    // Period key -> (months to look back, optional API grouping to keep long ranges small).
    public static readonly Dictionary<string, (int Months, string? Group)> Periods = new()
    {
        ["1M"] = (1, null),
        ["3M"] = (3, null),
        ["6M"] = (6, null),
        ["1Y"] = (12, "week"),
        ["5Y"] = (60, "week"),
    };

    public static readonly Dictionary<string, string> PeriodLabels = new()
    {
        ["1M"] = "1 Month", ["3M"] = "3 Months", ["6M"] = "6 Months", ["1Y"] = "1 Year", ["5Y"] = "5 Years"
    };

    [Required(ErrorMessage = "Enter an amount.")]
    [Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be greater than zero.")]
    public decimal? Amount { get; set; } = 100000;

    [Required]
    public string FromCurrency { get; set; } = "INR";

    [Required]
    public string ToCurrency { get; set; } = "USD";

    [Required]
    public string Period { get; set; } = "1Y";

    // Filled in only after a successful lookup.
    public List<TrendPoint> Points { get; set; } = new();
    public decimal? StartValue { get; set; }
    public DateOnly? StartDate { get; set; }
    public decimal? CurrentValue { get; set; }
    public DateOnly? CurrentDate { get; set; }
    public decimal? Change { get; set; }
    public decimal? ChangePercent { get; set; }
    public decimal? HighValue { get; set; }
    public DateOnly? HighDate { get; set; }
    public decimal? LowValue { get; set; }
    public DateOnly? LowDate { get; set; }
    public string? ErrorMessage { get; set; }

    public bool HasResult => Points.Count > 1;
}
