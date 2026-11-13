using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Caching;
using SpendSmart.Application.RecurringExpenses;
using SpendSmart.Domain.Entities;
using SpendSmart.Domain.Enums;
using SpendSmart.UnitTests.TestSupport;

namespace SpendSmart.UnitTests.Application;

public class RecurringExpenseProcessorTests
{
    private readonly IRecurringExpenseRepository _recurring = Substitute.For<IRecurringExpenseRepository>();
    private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDashboardCache _cache = Substitute.For<IDashboardCache>();
    private readonly RecurringExpenseProcessor _sut;

    public RecurringExpenseProcessorTests()
    {
        _sut = new RecurringExpenseProcessor(
            _recurring, _expenses, _unitOfWork, _cache, new FixedTimeProvider(2026, 3, 15),
            NullLogger<RecurringExpenseProcessor>.Instance);
    }

    [Fact]
    public async Task ProcessDueAsync_CreatesExpensesForEveryDueOccurrence_InOneSave()
    {
        var rent = new RecurringExpense("alice", 1000m, "Rent", 1, RecurrenceFrequency.Monthly, new DateOnly(2026, 2, 1)).WithId(1);
        var gym = new RecurringExpense("bob", 30m, "Gym", 2, RecurrenceFrequency.Monthly, new DateOnly(2026, 3, 10)).WithId(2);
        _recurring.GetDueAsync(new DateOnly(2026, 3, 15), Arg.Any<CancellationToken>()).Returns(new List<RecurringExpense> { rent, gym });

        var created = await _sut.ProcessDueAsync();

        Assert.Equal(3, created); // rent for Feb + Mar, gym for Mar
        _expenses.Received(2).Add(Arg.Is<Expense>(e => e.UserId == "alice" && e.RecurringExpenseId == 1));
        _expenses.Received(1).Add(Arg.Is<Expense>(e => e.UserId == "bob" && e.Date == new DateOnly(2026, 3, 10)));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _cache.Received(1).Invalidate("alice");
        _cache.Received(1).Invalidate("bob");
        Assert.Equal(new DateOnly(2026, 4, 1), rent.NextOccurrence);
    }

    [Fact]
    public async Task ProcessDueAsync_WithNothingDue_DoesNotSave()
    {
        _recurring.GetDueAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(new List<RecurringExpense>());

        var created = await _sut.ProcessDueAsync();

        Assert.Equal(0, created);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueAsync_RunTwice_DoesNotDoubleBook()
    {
        var rent = new RecurringExpense("alice", 1000m, "Rent", 1, RecurrenceFrequency.Monthly, new DateOnly(2026, 3, 1)).WithId(1);
        _recurring.GetDueAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(new List<RecurringExpense> { rent });

        var first = await _sut.ProcessDueAsync();
        var second = await _sut.ProcessDueAsync();

        Assert.Equal(1, first);
        Assert.Equal(0, second);
    }
}
