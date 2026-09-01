using System.Text.RegularExpressions;
using TaskForge.Shared.Exceptions;

namespace TaskForge.Domain.Common;

public sealed record TenantSlug
{
    private static readonly Regex SlugRegex = new(
        @"^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public const int MinLength = 3;
    public const int MaxLength = 50;

    public string Value { get; }

    private TenantSlug(string value)
    {
        Value = value;
    }

    public static TenantSlug Create(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainException("Tenant slug cannot be empty.");
        }

        var normalized = slug.Trim().ToLowerInvariant();

        if (normalized.Length < MinLength || normalized.Length > MaxLength)
        {
            throw new DomainException($"Tenant slug must be between {MinLength} and {MaxLength} characters.");
        }

        if (!SlugRegex.IsMatch(normalized))
        {
            throw new DomainException($"Tenant slug '{slug}' is invalid. Only lowercase alphanumeric characters and single hyphens are allowed.");
        }

        return new TenantSlug(normalized);
    }

    public override string ToString() => Value;

    public static implicit operator string(TenantSlug slug) => slug.Value;
}
