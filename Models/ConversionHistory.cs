namespace CurrencyMini.Models;

// One saved conversion. Table: Conversions.
public class ConversionHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }   // owner: every query filters on this
    public decimal Amount { get; set; }
    public string FromCurrency { get; set; } = "";
    public string ToCurrency { get; set; } = "";
    public decimal Rate { get; set; }
    public decimal ConvertedAmount { get; set; }
    public DateTime ConvertedAt { get; set; }
}
