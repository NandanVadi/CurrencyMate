using System.ComponentModel.DataAnnotations;
using CurrencyMini.Models;

namespace CurrencyMini.ViewModels;

public class ConvertViewModel
{
    [Required(ErrorMessage = "Enter an amount.")]
    [Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be greater than zero.")]
    public decimal? Amount { get; set; }

    [Required]
    public string FromCurrency { get; set; } = "USD";

    [Required]
    public string ToCurrency { get; set; } = "INR";

    // Filled in only after a successful conversion.
    public decimal? Rate { get; set; }
    public decimal? ConvertedAmount { get; set; }
    public DateOnly? RateDate { get; set; }
    public string? ErrorMessage { get; set; }

    // Saved currency pairs, shown as "Quick Convert" star buttons on the form.
    public List<FavoriteCurrencyPair> Favorites { get; set; } = new();

    // The user's latest conversions, shown under the form so they can pick up where they left off.
    public List<ConversionHistory> Recent { get; set; } = new();

    // Chart points for the 30-day history graph on the right column
    public List<TrendPoint> ChartPoints { get; set; } = new();
}
