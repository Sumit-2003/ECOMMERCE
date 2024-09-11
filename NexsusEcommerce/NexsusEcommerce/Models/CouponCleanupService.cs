using Microsoft.EntityFrameworkCore;
using NexsusEcommerce.Models;

public class CouponCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CouponCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Adjust as needed

    public CouponCleanupService(IServiceScopeFactory scopeFactory, ILogger<CouponCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_cleanupInterval, stoppingToken);
            try
            {
                await CleanupExpiredCoupons(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up expired coupons.");
            }
        }
    }

    private async Task CleanupExpiredCoupons(CancellationToken cancellationToken)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
            var now = DateTime.Now;

            var expiredCoupons = await context.Coupons
                .Where(c => c.EndDate < now)
                .ToListAsync(cancellationToken);

            if (expiredCoupons.Any())
            {
                _logger.LogInformation($"Found {expiredCoupons.Count} expired coupons.");
                context.Coupons.RemoveRange(expiredCoupons);
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Expired coupons have been removed.");
            }
        }
    }
}
