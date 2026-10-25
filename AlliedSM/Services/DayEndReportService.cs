using AlliedSM.Data;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Services
{
    public class DayEndReportService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DayEndReportService> _logger;
        private DateTime? _lastSentDate;

        public DayEndReportService(IServiceScopeFactory scopeFactory, ILogger<DayEndReportService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var sms = scope.ServiceProvider.GetRequiredService<ISmsService>();

                    var settings = await context.SchoolSettings.FirstOrDefaultAsync();

                    if (settings != null
                        && !string.IsNullOrWhiteSpace(settings.PrincipalMobile)
                        && TimeSpan.TryParse(settings.DayEndReportTime, out var targetTime)
                        && DateTime.Now.TimeOfDay >= targetTime
                        && _lastSentDate?.Date != DateTime.Today)
                    {
                        var message = await DayEndReportBuilder.BuildAsync(context, DateTime.Today);
                        await sms.SendAsync(settings.PrincipalMobile, message);

                        _lastSentDate = DateTime.Today;
                        _logger.LogInformation("Day-end report sent to principal for {Date}", DateTime.Today);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Day-end report failed");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
