using System.Text.RegularExpressions;

namespace WpfApp;

/// <summary>Shared field-validation rules reused across account/profile forms.</summary>
public static class ValidationHelper
{
    public const int MinPasswordLength = 6;

    private static readonly Regex UsernameRegex = new(@"^[a-z0-9]+$");
    private static readonly Regex FullNameRegex = new(@"^[\p{L}\s]+$");
    private static readonly Regex PhoneRegex = new(@"^0\d{9}$");
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    /// <summary>5-20 characters, lowercase letters and digits only.</summary>
    public static bool IsValidUsername(string username)
        => username.Length is >= 5 and <= 20 && UsernameRegex.IsMatch(username);

    /// <summary>Letters and spaces only (Unicode-aware, so Vietnamese names pass).</summary>
    public static bool IsValidFullName(string fullName)
        => FullNameRegex.IsMatch(fullName);

    /// <summary>10 digits starting with 0. Empty string is considered valid (phone is optional).</summary>
    public static bool IsValidPhone(string phone)
        => string.IsNullOrEmpty(phone) || PhoneRegex.IsMatch(phone);

    /// <summary>Basic local@domain.tld shape. Empty string is considered valid (email is optional).</summary>
    public static bool IsValidEmail(string email)
        => string.IsNullOrEmpty(email) || EmailRegex.IsMatch(email);

    public static bool IsValidPassword(string password)
        => password.Length >= MinPasswordLength;
}
