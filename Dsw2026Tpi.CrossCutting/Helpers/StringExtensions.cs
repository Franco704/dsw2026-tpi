namespace Dsw2026Tpi.CrossCutting.Helpers;

public static class StringExtensions
{
    public static string NormalizeEmail(this string email) =>
        email.Trim().ToLowerInvariant();
}