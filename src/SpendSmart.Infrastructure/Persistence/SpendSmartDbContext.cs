using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpendSmart.Application.Abstractions;
using SpendSmart.Domain.Entities;
using SpendSmart.Infrastructure.Identity;

namespace SpendSmart.Infrastructure.Persistence;

public sealed class SpendSmartDbContext(DbContextOptions<SpendSmartDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IUnitOfWork
{
    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Budget> Budgets => Set<Budget>();

    public DbSet<RecurringExpense> RecurringExpenses => Set<RecurringExpense>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(SpendSmartDbContext).Assembly);
    }
}
