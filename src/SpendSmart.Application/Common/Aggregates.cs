namespace SpendSmart.Application.Common;

/// <summary>Spending per category, aggregated in the database.</summary>
public sealed record CategoryTotal(int CategoryId, string CategoryName, string Color, decimal Total);

/// <summary>Spending per calendar month, aggregated in the database.</summary>
public sealed record MonthlyTotal(int Year, int Month, decimal Total);
