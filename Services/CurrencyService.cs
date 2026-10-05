using System.Net.Http.Json;
using System.Text.Json;

namespace CurrencyMini.Services;

// Talks to the free Frankfurter exchange-rate API via an injected, pre-configured HttpClient.
public class CurrencyService : ICurrencyService
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly ILogger<CurrencyService> _log;

    public CurrencyService(HttpClient http, ILogger<CurrencyService> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<(decimal Rate, DateOnly? Date)> GetRateAsync(string from, string to, CancellationToken ct = default)
    {
        from = from.Trim().ToUpperInvariant();
        to = to.Trim().ToUpperInvariant();

        if (from == to) return (1m, null);

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync($"rate/{from}/{to}", ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException("Unable to reach the exchange-rate service.", ex);
        }

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("The exchange-rate service could not process that currency pair.");

        var dto = await response.Content.ReadFromJsonAsync<RateDto>(JsonOpts, ct)
                  ?? throw new InvalidOperationException("The exchange-rate service returned an invalid response.");

        var date = DateOnly.TryParse(dto.Date, out var d) ? d : (DateOnly?)null;
        return (dto.Rate, date);
    }

    public async Task<IReadOnlyList<(DateOnly Date, decimal Rate)>> GetHistoryAsync(
        string from, string to, DateOnly start, DateOnly end, string? group = null, CancellationToken ct = default)
    {
        from = from.Trim().ToUpperInvariant();
        to = to.Trim().ToUpperInvariant();

        // Primary: Frankfurter v2 (relative to the configured base address).
        var v2 = $"rates?base={from}&quotes={to}&from={start:yyyy-MM-dd}&to={end:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(group)) v2 += $"&group={group}";

        // Fallback: Frankfurter v1 time series (stable, documented format). Absolute URL overrides BaseAddress.
        var v1 = $"https://api.frankfurter.dev/v1/{start:yyyy-MM-dd}..{end:yyyy-MM-dd}?base={from}&symbols={to}";

        try
        {
            return await FetchHistoryAsync(v2, to, ct);
        }
        catch (InvalidOperationException ex)
        {
            _log.LogWarning(ex, "v2 history request failed ({Inner}); falling back to v1.", ex.InnerException?.Message ?? ex.Message);
        }

        try
        {
            var points = await FetchHistoryAsync(v1, to, ct);
            return group == "week" ? Thin(points, 7) : points;
        }
        catch (InvalidOperationException ex)
        {
            _log.LogError(ex, "v1 history request also failed ({Inner}).", ex.InnerException?.Message ?? ex.Message);
            throw;
        }
    }

    // Keeps roughly one point per `step` days so long ranges stay light on the chart.
    private static IReadOnlyList<(DateOnly Date, decimal Rate)> Thin(List<(DateOnly Date, decimal Rate)> pts, int step)
    {
        var result = pts.Where((_, i) => i % step == 0).ToList();
        if (result[^1] != pts[^1]) result.Add(pts[^1]); // always keep the latest point
        return result;
    }

    private async Task<List<(DateOnly Date, decimal Rate)>> FetchHistoryAsync(string url, string to, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException("The exchange-rate service took too long to respond.", ex);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException("Unable to reach the exchange-rate service.", ex);
        }

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"The exchange-rate service could not return history for that currency pair (HTTP {(int)response.StatusCode}).");

        var points = new List<(DateOnly Date, decimal Rate)>();
        try
        {
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                // v2 shape: [ { "date": "2025-01-02", "base": "INR", "quote": "USD", "rate": 0.0117 }, ... ]
                foreach (var el in root.EnumerateArray())
                {
                    if (el.TryGetProperty("quote", out var q) && !string.Equals(q.GetString(), to, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (DateOnly.TryParse(el.GetProperty("date").GetString(), out var d))
                        points.Add((d, el.GetProperty("rate").GetDecimal()));
                }
            }
            else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("rates", out var rates))
            {
                // v1 shape: { "rates": { "2025-01-02": { "USD": 0.0117 } } }
                foreach (var day in rates.EnumerateObject())
                    if (DateOnly.TryParse(day.Name, out var d) && day.Value.TryGetProperty(to, out var r))
                        points.Add((d, r.GetDecimal()));
            }
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new InvalidOperationException("The exchange-rate service returned an invalid response.", ex);
        }

        if (points.Count < 2)
            throw new InvalidOperationException("Not enough historical data is available for that pair and period.");

        return points.OrderBy(p => p.Date).ToList();
    }

    private sealed class RateDto
    {
        public string? Date { get; set; }
        public decimal Rate { get; set; }
    }
}
