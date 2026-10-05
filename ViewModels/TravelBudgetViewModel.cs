using System.ComponentModel.DataAnnotations;

namespace CurrencyMini.ViewModels;

public class TravelBudgetViewModel
{
    public static readonly string[] Currencies = { "USD", "EUR", "INR", "GBP", "JPY", "AUD", "CAD", "CNY" };

    // Share of each day's budget kept as an emergency buffer; the rest is "suggested daily spending".
    public const decimal BufferPercent = 0.10m;

    // Simple split of the suggested daily spending (must add up to 1.0).
    public static readonly (string Label, decimal Share)[] Split =
    {
        ("Stay", 0.40m), ("Food", 0.30m), ("Transport", 0.15m), ("Activities & shopping", 0.15m)
    };

    [Required(ErrorMessage = "Enter your total budget.")]
    [Range(0.01, 1_000_000_000, ErrorMessage = "Budget must be greater than zero.")]
    public decimal? TotalBudget { get; set; }

    [Required(ErrorMessage = "Enter the number of days.")]
    [Range(1, 365, ErrorMessage = "Days must be between 1 and 365.")]
    public int? Days { get; set; }

    [Required]
    public string FromCurrency { get; set; } = "INR";

    [Required]
    public string ToCurrency { get; set; } = "USD";

    // Filled in only after a successful calculation.
    public decimal? Rate { get; set; }
    public DateOnly? RateDate { get; set; }
    public decimal? ConvertedTotal { get; set; }
    public decimal? DailyBudgetHome { get; set; }        // in source currency
    public decimal? DailyBudgetConverted { get; set; }   // in destination currency
    public decimal? SuggestedDailySpending { get; set; } // in destination currency, after buffer
    public decimal? DailyBuffer { get; set; }            // in destination currency
    public string? ErrorMessage { get; set; }

    public bool HasResult => ConvertedTotal is not null;
}
