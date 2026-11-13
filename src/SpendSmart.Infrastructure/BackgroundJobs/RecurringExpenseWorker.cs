using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpendSmart.Application.RecurringExpenses;

namespace SpendSmart.Infrastructure.BackgroundJobs;

public sealed class RecurringExpenseOptions
{
    public const string SectionName = "RecurringExpenses";

    public bool Enabled { get; set; } = true;

    /// <summary>How often to look for due templates. Runs once immediately on startup.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
}

/// <summary>
/// Hosted service that periodically generates expenses from due recurring templates.
/// Each run gets its own DI scope (and so its own DbContext).
/// </summary>
public sealed class RecurringExpenseWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RecurringExpenseOptions> options,
    TimeProvider timeProvider,
    ILogger<RecurringExpenseWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Recurring expense worker is disabled");
            return;
        }

        logger.LogInformation("Recurring expense worker started; interval {Interval}", settings.Interval);
        using var timer = new PeriodicTimer(settings.Interval, timeProvider);

        try
        {
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IRecurringExpenseProcessor>();
            await processor.ProcessDueAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One failed run must not kill the worker; the next tick retries.
            logger.LogError(ex, "Recurring expense run failed");
        }
    }
}
