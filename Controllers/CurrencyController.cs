using CurrencyMini.Data;
using CurrencyMini.Models;
using CurrencyMini.Services;
using CurrencyMini.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CurrencyMini.Controllers;

[Authorize]
public class CurrencyController : Controller
{
    private readonly ICurrencyService _currency;
    private readonly AppDbContext _db;

    public CurrencyController(ICurrencyService currency, AppDbContext db)
    {
        _currency = currency;
        _db = db;
    }

    // Id of the logged-in user (from the login cookie). Every DB read/write below is scoped to it.
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    public async Task<IActionResult> Index(decimal? amount, string? fromCurrency, string? toCurrency, CancellationToken ct)
    {
        var model = new ConvertViewModel();
        if (amount is not null) model.Amount = amount;
        if (!string.IsNullOrWhiteSpace(fromCurrency)) model.FromCurrency = fromCurrency.ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(toCurrency)) model.ToCurrency = toCurrency.ToUpperInvariant();

        await LoadFavoritesAsync(model, ct);
        await LoadRecentAsync(model, ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ConvertViewModel model, CancellationToken ct)
    {
        await LoadFavoritesAsync(model, ct);

        if (model.FromCurrency == model.ToCurrency)
            ModelState.AddModelError(nameof(model.ToCurrency), "Choose two different currencies.");

        if (!ModelState.IsValid)
        {
            await LoadRecentAsync(model, ct);
            return View(model);
        }

        try
        {
            var (rate, date) = await _currency.GetRateAsync(model.FromCurrency, model.ToCurrency, ct);
            model.Rate = rate;
            model.RateDate = date;
            model.ConvertedAmount = Math.Round(model.Amount!.Value * rate, 2);

            await _db.Conversions.AddAsync(new ConversionHistory
            {
                UserId = UserId,
                Amount = model.Amount.Value,
                FromCurrency = model.FromCurrency,
                ToCurrency = model.ToCurrency,
                Rate = rate,
                ConvertedAmount = model.ConvertedAmount.Value,
                ConvertedAt = DateTime.UtcNow
            }, ct);
            await _db.SaveChangesAsync(ct);

            // Fetch 30-day history for the chart on the right column
            try
            {
                var end = DateOnly.FromDateTime(DateTime.UtcNow);
                var start = end.AddMonths(-1);
                var points = await _currency.GetHistoryAsync(model.FromCurrency, model.ToCurrency, start, end, null, ct);
                // Trend points are stored as (Date, Rate, Value). For the chart, Value is Amount * Rate.
                model.ChartPoints = points.Select(p => new TrendPoint(p.Date, p.Rate, Math.Round(model.Amount.Value * p.Rate, 2))).ToList();
            }
            catch { /* Ignore if history fails */ }
        }
        catch (InvalidOperationException ex)
        {
            model.ErrorMessage = ex.Message; // friendly, no stack trace shown to the user
        }

        await LoadRecentAsync(model, ct); // after saving, so the new conversion shows up right away
        return View(model);
    }

    // Saves the currently selected pair as a favorite (button uses formaction to hit this instead of Convert).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFavorite(decimal? amount, string fromCurrency, string toCurrency, CancellationToken ct)
    {
        fromCurrency = fromCurrency?.Trim().ToUpperInvariant() ?? "";
        toCurrency = toCurrency?.Trim().ToUpperInvariant() ?? "";

        if (fromCurrency.Length == 3 && toCurrency.Length == 3 && fromCurrency != toCurrency)
        {
            var uid = UserId;
            var exists = await _db.Favorites.AnyAsync(f => f.UserId == uid && f.FromCurrency == fromCurrency && f.ToCurrency == toCurrency, ct);
            if (!exists)
            {
                await _db.Favorites.AddAsync(new FavoriteCurrencyPair
                {
                    UserId = uid,
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency,
                    CreatedAt = DateTime.UtcNow
                }, ct);
                await _db.SaveChangesAsync(ct);
                TempData["Message"] = $"Added \u2b50 {fromCurrency} \u2192 {toCurrency} to favorites.";
            }
            else
            {
                TempData["Message"] = "That pair is already in your favorites.";
            }
        }

        return RedirectToAction(nameof(Index), new { amount, fromCurrency, toCurrency });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFavorite(int id, decimal? amount, string? fromCurrency, string? toCurrency, CancellationToken ct)
    {
        var uid = UserId;
        var favorite = await _db.Favorites.FirstOrDefaultAsync(f => f.Id == id && f.UserId == uid, ct); // can't delete someone else's
        if (favorite is not null)
        {
            _db.Favorites.Remove(favorite);
            await _db.SaveChangesAsync(ct);
            TempData["Message"] = "Removed from favorites.";
        }

        return RedirectToAction(nameof(Index), new { amount, fromCurrency, toCurrency });
    }

    [HttpGet]
    public IActionResult Travel() => View(new TravelBudgetViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Travel(TravelBudgetViewModel model, CancellationToken ct)
    {
        if (model.FromCurrency == model.ToCurrency)
            ModelState.AddModelError(nameof(model.ToCurrency), "Choose two different currencies.");

        if (!ModelState.IsValid) return View(model);

        try
        {
            var (rate, date) = await _currency.GetRateAsync(model.FromCurrency, model.ToCurrency, ct);
            var days = model.Days!.Value;

            model.Rate = rate;
            model.RateDate = date;
            model.ConvertedTotal = Math.Round(model.TotalBudget!.Value * rate, 2);
            model.DailyBudgetHome = Math.Round(model.TotalBudget.Value / days, 2);
            model.DailyBudgetConverted = Math.Round(model.ConvertedTotal.Value / days, 2);
            model.DailyBuffer = Math.Round(model.DailyBudgetConverted.Value * TravelBudgetViewModel.BufferPercent, 2);
            model.SuggestedDailySpending = model.DailyBudgetConverted.Value - model.DailyBuffer.Value;

            // Reuse the history table so travel calculations show up in History too.
            await _db.Conversions.AddAsync(new ConversionHistory
            {
                UserId = UserId,
                Amount = model.TotalBudget.Value,
                FromCurrency = model.FromCurrency,
                ToCurrency = model.ToCurrency,
                Rate = rate,
                ConvertedAmount = model.ConvertedTotal.Value,
                ConvertedAt = DateTime.UtcNow
            }, ct);
            await _db.SaveChangesAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            model.ErrorMessage = ex.Message;
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Trend() => View(new TrendViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Trend(TrendViewModel model, CancellationToken ct)
    {
        if (model.FromCurrency == model.ToCurrency)
            ModelState.AddModelError(nameof(model.ToCurrency), "Choose two different currencies.");
        if (!TrendViewModel.Periods.ContainsKey(model.Period))
            ModelState.AddModelError(nameof(model.Period), "Choose a valid period.");

        if (!ModelState.IsValid) return View(model);

        try
        {
            var (months, group) = TrendViewModel.Periods[model.Period];
            var end = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = end.AddMonths(-months);

            var points = await _currency.GetHistoryAsync(model.FromCurrency, model.ToCurrency, start, end, group, ct);

            // "What would my money have been worth?" = amount x the rate on each date.
            var amount = model.Amount!.Value;
            model.Points = points
                .Select(p => new TrendPoint(p.Date, p.Rate, Math.Round(amount * p.Rate, 2)))
                .ToList();

            var first = model.Points[0];
            var last = model.Points[^1];
            var high = model.Points.MaxBy(p => p.Value)!;
            var low = model.Points.MinBy(p => p.Value)!;

            model.StartValue = first.Value;
            model.StartDate = first.Date;
            model.CurrentValue = last.Value;
            model.CurrentDate = last.Date;
            model.Change = last.Value - first.Value;
            model.ChangePercent = first.Value == 0 ? 0 : Math.Round((last.Value - first.Value) / first.Value * 100, 2);
            model.HighValue = high.Value;
            model.HighDate = high.Date;
            model.LowValue = low.Value;
            model.LowDate = low.Date;
        }
        catch (InvalidOperationException ex)
        {
            model.ErrorMessage = ex.Message;
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> History(CancellationToken ct)
    {
        var uid = UserId;
        var list = await _db.Conversions.Where(c => c.UserId == uid).OrderByDescending(c => c.ConvertedAt).ToListAsync(ct);
        return View(list);
    }

    private async Task LoadRecentAsync(ConvertViewModel model, CancellationToken ct)
    {
        var uid = UserId;
        model.Recent = await _db.Conversions.Where(c => c.UserId == uid)
            .OrderByDescending(c => c.ConvertedAt).Take(5).ToListAsync(ct);
    }

    private async Task LoadFavoritesAsync(ConvertViewModel model, CancellationToken ct)
    {
        var uid = UserId;
        model.Favorites = await _db.Favorites.Where(f => f.UserId == uid).OrderBy(f => f.CreatedAt).ToListAsync(ct);
    }
}
