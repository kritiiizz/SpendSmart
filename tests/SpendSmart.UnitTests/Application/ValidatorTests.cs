using SpendSmart.Application.Accounts;
using SpendSmart.Application.Categories;
using SpendSmart.Application.Expenses;
using SpendSmart.UnitTests.TestSupport;

namespace SpendSmart.UnitTests.Application;

public class ValidatorTests
{
    private readonly SaveExpenseRequestValidator _expenseValidator = new(new FixedTimeProvider(2026, 3, 15));

    [Fact]
    public void SaveExpense_ValidRequest_Passes()
    {
        var result = _expenseValidator.Validate(new SaveExpenseRequest(19.99m, "Books", new DateOnly(2026, 3, 15), 1));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("0", "Amount")]
    [InlineData("10.555", "Amount")]
    [InlineData("1000000.01", "Amount")]
    public void SaveExpense_InvalidAmount_Fails(string amount, string property)
    {
        var result = _expenseValidator.Validate(
            new SaveExpenseRequest(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), "Books", new DateOnly(2026, 3, 1), 1));

        Assert.Contains(result.Errors, e => e.PropertyName == property);
    }

    [Fact]
    public void SaveExpense_FutureDate_Fails()
    {
        var result = _expenseValidator.Validate(new SaveExpenseRequest(10m, "Books", new DateOnly(2026, 4, 1), 1));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SaveExpenseRequest.Date));
    }

    [Fact]
    public void ExpenseQuery_ToBeforeFrom_Fails()
    {
        var result = new ExpenseQueryValidator().Validate(new ExpenseQuery
        {
            From = new DateOnly(2026, 3, 10),
            To = new DateOnly(2026, 3, 1),
        });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ExpenseQuery.To));
    }

    [Fact]
    public void ExpenseQuery_Defaults_AreValid()
    {
        Assert.True(new ExpenseQueryValidator().Validate(new ExpenseQuery()).IsValid);
    }

    [Theory]
    [InlineData("not-an-email", "Password1")]
    [InlineData("a@b.dev", "short1")]
    public void Register_InvalidInput_Fails(string email, string password)
    {
        Assert.False(new RegisterRequestValidator().Validate(new RegisterRequest(email, password)).IsValid);
    }

    [Theory]
    [InlineData("Pets", "#00aa11", true)]
    [InlineData("Pets", null, true)]
    [InlineData("Pets", "green", false)]
    [InlineData("", "#00aa11", false)]
    public void SaveCategory_ValidatesNameAndColor(string name, string? color, bool expected)
    {
        Assert.Equal(expected, new SaveCategoryRequestValidator().Validate(new SaveCategoryRequest(name, color)).IsValid);
    }
}
