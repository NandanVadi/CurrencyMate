namespace CurrencyMini.Models;

// A saved currency pair for one-click quick convert. Table: Favorites.
public class FavoriteCurrencyPair
{
    public int Id { get; set; }
    public int UserId { get; set; }   // owner: every query filters on this
    public string FromCurrency { get; set; } = "";
    public string ToCurrency { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
