using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Caching;
using SpendSmart.Application.Common;
using SpendSmart.Application.Expenses;
using SpendSmart.Domain.Entities;
using SpendSmart.UnitTests.TestSupport;

namespace SpendSmart.UnitTests.Application;

public class ExpenseServiceTests
{
    private static readonly DateOnly Today = new(2026, 3, 15);

    private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDashboardCache _cache = Substitute.For<IDashboardCache>();
    private readonly ExpenseService _sut;

    public ExpenseServiceTests()
    {
        _currentUser.UserId.Returns(TestData.UserId);
        _sut = new ExpenseService(
            _expenses,
            _categories,
            _unitOfWork,
            _currentUser,
            _cache,
            new SaveExpenseRequestValidator(new FixedTimeProvider(2026, 3, 15)),
            new ExpenseQueryValidator(),
            NullLogger<ExpenseService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsSavesAndInvalidatesDashboard()
    {
        var category = TestData.Category(id: 7);
        _categories.GetAsync(7, TestData.UserId, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _sut.CreateAsync(new SaveExpenseRequest(42.50m, "Groceries", Today, 7));

        _expenses.Received(1).Add(Arg.Is<Expense>(e =>
            e.UserId == TestData.UserId && e.Amount == 42.50m && e.CategoryId == 7));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _cache.Received(1).Invalidate(TestData.UserId);
        Assert.Equal("Groceries", result.CategoryName);
        Assert.False(result.IsRecurring);
    }

    [Fact]
    public async Task CreateAsync_WithCategoryOfAnotherUser_ThrowsValidationException()
    {
        // The repository only finds categories owned by the caller, so another user's id returns null.
        _categories.GetAsync(99, TestData.UserId, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateAsync(new SaveExpenseRequest(10m, "Lunch", Today, 99)));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(SaveExpenseRequest.CategoryId));
        _expenses.DidNotReceive().Add(Arg.Any<Expense>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ThrowsBeforeTouchingRepositories()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateAsync(new SaveExpenseRequest(0m, "", Today, 1)));

        await _categories.DidNotReceive().GetAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenNotOwned_ThrowsNotFound()
    {
        _expenses.GetAsync(5, TestData.UserId, Arg.Any<CancellationToken>()).Returns((Expense?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetAsync(5));
    }

    [Fact]
    public async Task UpdateAsync_ChangesEntityAndSaves()
    {
        var oldCategory = TestData.Category(id: 1);
        var newCategory = TestData.Category(id: 2, name: "Dining Out");
        var expense = new Expense(TestData.UserId, 10m, "Old", Today, 1).WithId(3).WithCategory(oldCategory);
        _expenses.GetAsync(3, TestData.UserId, Arg.Any<CancellationToken>()).Returns(expense);
        _categories.GetAsync(2, TestData.UserId, Arg.Any<CancellationToken>()).Returns(newCategory);

        var result = await _sut.UpdateAsync(3, new SaveExpenseRequest(25m, "New", Today, 2));

        Assert.Equal(25m, expense.Amount);
        Assert.Equal("New", expense.Description);
        Assert.Equal("Dining Out", result.CategoryName);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_RemovesOwnedExpense()
    {
        var expense = new Expense(TestData.UserId, 10m, "Taxi", Today, 1).WithId(3);
        _expenses.GetAsync(3, TestData.UserId, Arg.Any<CancellationToken>()).Returns(expense);

        await _sut.DeleteAsync(3);

        _expenses.Received(1).Remove(expense);
        _cache.Received(1).Invalidate(TestData.UserId);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotOwned_ThrowsNotFoundAndRemovesNothing()
    {
        _expenses.GetAsync(3, TestData.UserId, Arg.Any<CancellationToken>()).Returns((Expense?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(3));

        _expenses.DidNotReceive().Remove(Arg.Any<Expense>());
    }

    [Fact]
    public async Task ListAsync_MapsItemsAndPagingMetadata()
    {
        var category = TestData.Category();
        var items = new List<Expense>
        {
            new Expense(TestData.UserId, 5m, "Coffee", Today, 1).WithId(1).WithCategory(category),
            new Expense(TestData.UserId, 15m, "Lunch", Today, 1).WithId(2).WithCategory(category),
        };
        var query = new ExpenseQuery { Page = 2, PageSize = 2 };
        _expenses.ListAsync(TestData.UserId, query, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Expense>)items, 5));

        var page = await _sut.ListAsync(query);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public async Task ListAsync_WithOversizedPage_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.ListAsync(new ExpenseQuery { PageSize = ExpenseQuery.MaxPageSize + 1 }));
    }
}
