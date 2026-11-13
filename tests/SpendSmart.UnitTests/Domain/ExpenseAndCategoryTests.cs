using SpendSmart.Domain.Entities;
using SpendSmart.Domain.Exceptions;

namespace SpendSmart.UnitTests.Domain;

public class ExpenseAndCategoryTests
{
    [Fact]
    public void Expense_TrimsDescription()
    {
        var expense = new Expense("user", 12.5m, "  Lunch  ", new DateOnly(2026, 3, 1), 1);

        Assert.Equal("Lunch", expense.Description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Expense_RejectsNonPositiveAmount(int amount)
    {
        Assert.Throws<DomainException>(() => new Expense("user", amount, "Lunch", new DateOnly(2026, 3, 1), 1));
    }

    [Fact]
    public void Expense_RejectsTooLongDescription()
    {
        var description = new string('x', Expense.DescriptionMaxLength + 1);

        Assert.Throws<DomainException>(() => new Expense("user", 1m, description, new DateOnly(2026, 3, 1), 1));
    }

    [Fact]
    public void Expense_Update_ChangesAllFields()
    {
        var expense = new Expense("user", 10m, "Taxi", new DateOnly(2026, 3, 1), 1);

        expense.Update(20m, "Train", new DateOnly(2026, 3, 2), 2);

        Assert.Equal(20m, expense.Amount);
        Assert.Equal("Train", expense.Description);
        Assert.Equal(new DateOnly(2026, 3, 2), expense.Date);
        Assert.Equal(2, expense.CategoryId);
    }

    [Fact]
    public void Category_LowercasesColor_AndDefaultsWhenMissing()
    {
        Assert.Equal("#abcdef", new Category("user", "Fun", "#ABCDEF").Color);
        Assert.Equal(Category.DefaultColor, new Category("user", "Fun").Color);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("123456")]
    public void Category_RejectsInvalidColor(string color)
    {
        Assert.Throws<DomainException>(() => new Category("user", "Fun", color));
    }
}
