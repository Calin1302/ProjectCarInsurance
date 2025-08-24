using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using CarInsurance.Api.Data;


namespace CarInsurance.Api.Services;

public sealed class PolicyExpiryNotifierOptions
{
    // la cât timp verificăm (default: 5 minute)
    public int IntervalSeconds { get; set; } = 300;

    // dev helper: lărgește fereastra [00:00, 01:00) la N ore (ex. 24) pentru test ușor în timpul zilei
    public int DevTestWindowHours { get; set; } = 1;
}

public class PolicyExpiryNotifier(
    ILogger<PolicyExpiryNotifier> logger,
    IServiceProvider services,
    TimeProvider timeProvider,
    IOptions<PolicyExpiryNotifierOptions> options
) : BackgroundService
{
    private readonly ILogger<PolicyExpiryNotifier> _logger = logger;
    private readonly IServiceProvider _services = services;
    private readonly TimeProvider _time = timeProvider;
    private readonly PolicyExpiryNotifierOptions _opt = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PolicyExpiryNotifier started. Interval: {sec}s", _opt.IntervalSeconds);
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_opt.IntervalSeconds));

        // rulează imediat o dată, apoi periodic
        await ProcessOnce(stoppingToken);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessOnce(stoppingToken);
        }
    }

    private async Task ProcessOnce(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var nowLocal = _time.GetLocalNow().DateTime;
            var today = DateOnly.FromDateTime(nowLocal);
            var yesterday = today.AddDays(-1);

            // „momentul expirării” = 00:00 în ziua imediat următoare EndDate
            var windowStart = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Local);
            var windowEnd = windowStart.AddHours(_opt.DevTestWindowHours);

            // Procesăm DOAR în fereastra [00:00, 01:00) (sau lărgită pentru dev)
            if (nowLocal < windowStart || nowLocal >= windowEnd)
            {
                _logger.LogDebug("Outside processing window ({start}..{end}). Now: {now}", windowStart, windowEnd, nowLocal);
                return;
            }

            // Găsește polițele care AU expirat ieri și nu au mai fost procesate
            var expiredPolicies = await db.Policies
                .Where(p => p.EndDate == yesterday)
                .Select(p => new { p.Id, p.CarId, p.EndDate, p.Provider })
                .ToListAsync(ct);

            if (expiredPolicies.Count == 0)
            {
                _logger.LogDebug("No policies expired on {yesterday}.", yesterday);
                return;
            }

            // Filtrăm cele deja logate
            var processedPolicyIds = await db.ProcessedExpirations
                .Where(x => expiredPolicies.Select(ep => ep.Id).Contains(x.PolicyId))
                .Select(x => x.PolicyId)
                .ToListAsync(ct);

            foreach (var p in expiredPolicies.Where(ep => !processedPolicyIds.Contains(ep.Id)))
            {
                _logger.LogInformation("Policy expired: PolicyId={PolicyId}, CarId={CarId}, Provider={Provider}, EndDate={EndDate}",
                    p.Id, p.CarId, p.Provider, p.EndDate);

                db.ProcessedExpirations.Add(new Models.ProcessedPolicyExpiration
                {
                    PolicyId = p.Id,
                    EndDate = p.EndDate,
                    ProcessedAtUtc = _time.GetUtcNow().UtcDateTime
                });
            }

            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // ignore stop
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PolicyExpiryNotifier iteration failed.");
        }
    }
}
