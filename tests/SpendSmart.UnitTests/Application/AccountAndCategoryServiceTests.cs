using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Accounts;
using SpendSmart.Application.Caching;
using SpendSmart.Application.Categories;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Entities;
using SpendSmart.UnitTests.TestSupport;

namespace SpendSmart.UnitTests.Application;

public class AccountServiceTests
{
    private readonly IIdentityService _identity = Substitute.For<IIdentityService>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _sut = new AccountService(
            _identity, _categories, _unitOfWork, new RegisterRequestValidator(), new LoginRequestValidator(),
            NullLogger<AccountService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_CreatesUserWithDefaultCategories()
    {
        _identity.CreateUserAsync("new@spendsmart.dev", "Password1", Arg.Any<CancellationToken>())
            .Returns(new IdentityResultDto("new-id", []));

        var userId = await _sut.RegisterAsync(new RegisterRequest("new@spendsmart.dev", "Password1"));

        Assert.Equal("new-id", userId);
        _categories.Received(DefaultCategories.All.Count).Add(Arg.Is<Category>(c => c.UserId == "new-id"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_MapsIdentityErrorsToFields()
    {
        _identity.CreateUserAsync(default!, default!, default).ReturnsForAnyArgs(new IdentityResultDto(null,
        [
            new IdentityError("DuplicateEmail", "Email is already taken."),
            new IdentityError("PasswordRequiresDigit", "Passwords must have at least one digit."),
        ]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.RegisterAsync(new RegisterRequest("taken@spendsmart.dev", "Password")));

        Assert.Contains(ex.Errors, e => e.PropertyName == "Email");
        Assert.Contains(ex.Errors, e => e.PropertyName == "Password");
        _categories.DidNotReceive().Add(Arg.Any<Category>());
    }
}

public class CategoryServiceTests
{
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestData.UserId);
        _sut = new CategoryService(
            _categories, Substitute.For<IUnitOfWork>(), currentUser, Substitute.For<IDashboardCache>(),
            new SaveCategoryRequestValidator());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsValidationException()
    {
        _categories.NameExistsAsync(TestData.UserId, "Groceries", null, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(new SaveCategoryRequest(" Groceries ", null)));
    }

    [Fact]
    public async Task DeleteAsync_WhenInUse_ThrowsConflictAndKeepsCategory()
    {
        var category = TestData.Category(4);
        _categories.GetAsync(4, TestData.UserId, Arg.Any<CancellationToken>()).Returns(category);
        _categories.IsInUseAsync(4, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(4));

        _categories.DidNotReceive().Remove(Arg.Any<Category>());
    }
}
