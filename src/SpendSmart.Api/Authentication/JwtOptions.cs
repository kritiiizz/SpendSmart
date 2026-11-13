namespace SpendSmart.Api.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "SpendSmart";

    public string Audience { get; set; } = "SpendSmart.Api";

    /// <summary>HMAC-SHA256 key, at least 32 characters. Supply via environment variable / secret store, never commit it.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
