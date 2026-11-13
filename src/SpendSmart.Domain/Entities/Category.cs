using System.Text.RegularExpressions;
using SpendSmart.Domain.Common;
using SpendSmart.Domain.Exceptions;

namespace SpendSmart.Domain.Entities;

public sealed partial class Category : Entity, IOwnedByUser
{
    public const int NameMaxLength = 50;
    public const string DefaultColor = "#6c757d";

    private Category()
    {
    }

    public Category(string userId, string name, string? color = null)
    {
        UserId = Guard.NotBlank(userId, nameof(UserId), 450);
        Rename(name, color);
    }

    public string UserId { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>Hex colour used by the dashboard charts, e.g. <c>#0d6efd</c>.</summary>
    public string Color { get; private set; } = DefaultColor;

    public void Rename(string name, string? color = null)
    {
        Name = Guard.NotBlank(name, nameof(Name), NameMaxLength);

        if (color is not null)
        {
            if (!HexColor().IsMatch(color))
            {
                throw new DomainException("Color must be a hex value such as #0d6efd.");
            }

            Color = color.ToLowerInvariant();
        }
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();
}
