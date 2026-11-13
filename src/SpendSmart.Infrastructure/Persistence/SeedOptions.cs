namespace SpendSmart.Infrastructure.Persistence;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Create the demo account and sample data on startup if it doesn't exist yet.</summary>
    public bool Enabled { get; set; } = true;

    public string DemoEmail { get; set; } = "demo@spendsmart.dev";

    public string DemoPassword { get; set; } = "Demo1234!";
}
