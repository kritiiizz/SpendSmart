using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpendSmart.Domain.Entities;
using SpendSmart.Infrastructure.Identity;

namespace SpendSmart.Infrastructure.Persistence.Configurations;

// Deleting a user cascades to all of their data. Category foreign keys use NO ACTION so a category
// can't be removed while it's in use (the service layer also checks this and returns 409).

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.UserId).HasMaxLength(450).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(Category.NameMaxLength).IsRequired();
        builder.Property(c => c.Color).HasMaxLength(7).IsRequired();

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
    }
}

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.Property(e => e.UserId).HasMaxLength(450).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Description).HasMaxLength(Expense.DescriptionMaxLength).IsRequired();

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<RecurringExpense>().WithMany().HasForeignKey(e => e.RecurringExpenseId).OnDelete(DeleteBehavior.SetNull);

        // Covers the default list query (a user's expenses, newest first) and date-range reports.
        builder.HasIndex(e => new { e.UserId, e.Date });

        // Last line of defence against the background job booking the same occurrence twice.
        builder.HasIndex(e => new { e.RecurringExpenseId, e.Date }).IsUnique();
    }
}

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.Property(b => b.UserId).HasMaxLength(450).IsRequired();
        builder.Property(b => b.MonthlyLimit).HasPrecision(18, 2);

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(b => b.Category).WithMany().HasForeignKey(b => b.CategoryId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(b => new { b.UserId, b.CategoryId }).IsUnique();
    }
}

internal sealed class RecurringExpenseConfiguration : IEntityTypeConfiguration<RecurringExpense>
{
    public void Configure(EntityTypeBuilder<RecurringExpense> builder)
    {
        builder.Property(r => r.UserId).HasMaxLength(450).IsRequired();
        builder.Property(r => r.Amount).HasPrecision(18, 2);
        builder.Property(r => r.Description).HasMaxLength(Expense.DescriptionMaxLength).IsRequired();
        builder.Property(r => r.Frequency).HasConversion<string>().HasMaxLength(16);

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.Category).WithMany().HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.NoAction);

        // The background job's query: active templates that are due.
        builder.HasIndex(r => new { r.IsActive, r.NextOccurrence });
    }
}
