namespace CurrencyMini.Models;

// A registered customer. Table: Users.
public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";          // stored trimmed + lower-case, unique
    public string PasswordHash { get; set; } = "";   // PBKDF2 hash with per-user salt (never the password itself)
    public DateTime CreatedAt { get; set; }
}
