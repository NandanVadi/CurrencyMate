namespace CurrencyMini.Services;

public interface ICurrencyService
{
    // Throws on any API/network problem; controller turns that into a friendly message.
    Task<(decimal Rate, DateOnly? Date)> GetRateAsync(string from, string to, CancellationToken ct = default);

    // Daily (or weekly, when group == "week") rates for a pair between two dates, oldest first.
    Task<IReadOnlyList<(DateOnly Date, decimal Rate)>> GetHistoryAsync(
        string from, string to, DateOnly start, DateOnly end, string? group = null, CancellationToken ct = default);
}
