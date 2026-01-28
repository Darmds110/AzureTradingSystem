using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Functions;

public class MarketScheduleChecker
{
    private readonly IMarketDataService _marketDataService;
    private readonly IEmailService _emailService;
    private readonly ILogger<MarketScheduleChecker> _logger;

    public MarketScheduleChecker(
        IMarketDataService marketDataService,
        IEmailService emailService,
        ILogger<MarketScheduleChecker> logger)
    {
        _marketDataService = marketDataService;
        _emailService = emailService;
        _logger = logger;
    }

    [Function("MarketScheduleChecker")]
    public async Task Run([TimerTrigger("0 0 6 * * *")] TimerInfo timer)
    {
        _logger.LogInformation("MarketScheduleChecker executed at: {Time}", DateTime.UtcNow);

        try
        {
            // Update market schedule for next 30 days
            await _marketDataService.UpdateMarketSchedule();

            _logger.LogInformation("Market schedule updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating market schedule");
            await _emailService.SendErrorNotificationAsync("Failed to update market schedule", ex);
            throw;
        }
    }
}