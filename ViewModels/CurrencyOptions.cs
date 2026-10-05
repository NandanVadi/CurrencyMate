using Microsoft.AspNetCore.Mvc.Rendering;

namespace CurrencyMini.ViewModels;

// Friendly "USD - US Dollar" labels for every currency dropdown (same list in all pages).
public static class CurrencyOptions
{
    public static readonly Dictionary<string, string> Names = new()
    {
        ["USD"] = "US Dollar",
        ["EUR"] = "Euro",
        ["INR"] = "Indian Rupee",
        ["GBP"] = "British Pound",
        ["JPY"] = "Japanese Yen",
        ["AUD"] = "Australian Dollar",
        ["CAD"] = "Canadian Dollar",
        ["CNY"] = "Chinese Yuan"
    };

    public static IEnumerable<SelectListItem> Items =>
        TravelBudgetViewModel.Currencies.Select(c =>
            new SelectListItem(Names.TryGetValue(c, out var n) ? $"{c} \u00b7 {n}" : c, c));
}
