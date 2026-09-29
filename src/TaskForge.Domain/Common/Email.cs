using System.Text.RegularExpressions;
using TaskForge.Shared.Exceptions;

namespace TaskForge.Domain.Common;

public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public const int MaxLength = 256;

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email address cannot be empty.");
        }

        var trimmedEmail = email.Trim().ToLowerInvariant();

        if (trimmedEmail.Length > MaxLength)
        {
            throw new DomainException($"Email address cannot exceed {MaxLength} characters.");
        }

        if (!EmailRegex.IsMatch(trimmedEmail))
        {
            throw new DomainException($"Invalid email format: '{email}'.");
        }

        return new Email(trimmedEmail);
    }

    public static bool TryCreate(string? email, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Email? result)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            result = null;
            return false;
        }

        var trimmedEmail = email.Trim().ToLowerInvariant();

        if (trimmedEmail.Length > MaxLength || !EmailRegex.IsMatch(trimmedEmail))
        {
            result = null;
            return false;
        }

        result = new Email(trimmedEmail);
        return true;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
