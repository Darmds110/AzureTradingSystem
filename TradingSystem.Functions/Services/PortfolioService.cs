using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Data;
using TradingSystem.Functions.Models;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services
{
    /// <summary>
    /// Implementation of portfolio service for managing portfolio state
    /// </summary>
    public class PortfolioService : IPortfolioService
    {
        private readonly TradingDbContext _dbContext;
        private readonly ILogger<PortfolioService> _logger;

        public PortfolioService(
            TradingDbContext dbContext,
            ILogger<PortfolioService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Syncs portfolio state with Alpaca account data
        /// </summary>
        public async Task SyncPortfolioStateAsync(AccountInfo accountInfo, List<PositionInfo> positions)
        {
            try
            {
                _logger.LogInformation("Starting portfolio sync");

                // Get the portfolio (assuming single portfolio for now)
                var portfolio = await GetCurrentPortfolioAsync();
                if (portfolio == null)
                {
                    _logger.LogError("No portfolio found in database");
                    throw new InvalidOperationException("Portfolio not found");
                }

                // Update portfolio values
                portfolio.CurrentEquity = accountInfo.Equity;
                portfolio.CurrentCash = accountInfo.Cash;
                portfolio.BuyingPower = accountInfo.BuyingPower;
                portfolio.LastSyncTimestamp = DateTime.UtcNow;

                // Update peak value if we've reached a new high
                if (accountInfo.Equity > portfolio.PeakValue)
                {
                    _logger.LogInformation(
                        "New peak value reached! Old: ${old}, New: ${new}",
                        portfolio.PeakValue,
                        accountInfo.Equity);
                    portfolio.PeakValue = accountInfo.Equity;
                }

                // Calculate and update drawdown
                decimal drawdown = 0;
                if (portfolio.PeakValue > 0)
                {
                    drawdown = ((accountInfo.Equity - portfolio.PeakValue) / portfolio.PeakValue) * 100;
                }
                portfolio.CurrentDrawdownPercent = drawdown;

                // Sync positions
                await SyncPositionsAsync(portfolio.PortfolioId, positions);

                // Save changes
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Portfolio sync complete. Equity: ${equity}, Positions: {count}, Drawdown: {drawdown}%",
                    accountInfo.Equity,
                    positions.Count,
                    drawdown);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing portfolio state");
                throw;
            }
        }

        /// <summary>
        /// Syncs positions table with current Alpaca positions
        /// </summary>
        private async Task SyncPositionsAsync(int portfolioId, List<PositionInfo> alpacaPositions)
        {
            // Get existing positions from database
            var existingPositions = await _dbContext.Positions
                .Where(p => p.PortfolioId == portfolioId)
                .ToListAsync();

            // Get list of current symbols from Alpaca
            var currentSymbols = alpacaPositions.Select(p => p.Symbol).ToList();

            // Remove positions that are no longer held
            var positionsToRemove = existingPositions
                .Where(p => !currentSymbols.Contains(p.Symbol))
                .ToList();

            if (positionsToRemove.Any())
            {
                _logger.LogInformation(
                    "Removing {count} closed positions: {symbols}",
                    positionsToRemove.Count,
                    string.Join(", ", positionsToRemove.Select(p => p.Symbol)));
                _dbContext.Positions.RemoveRange(positionsToRemove);
            }

            // Update or add positions
            foreach (var alpacaPos in alpacaPositions)
            {
                var existingPos = existingPositions
                    .FirstOrDefault(p => p.Symbol == alpacaPos.Symbol);

                if (existingPos != null)
                {
                    // Update existing position
                    existingPos.Quantity = (int)alpacaPos.Quantity;
                    existingPos.AverageCostBasis = alpacaPos.AverageCostBasis;
                    existingPos.CurrentPrice = alpacaPos.CurrentPrice;
                    existingPos.UnrealizedPL = alpacaPos.UnrealizedPL;
                    existingPos.UnrealizedPLPercent = alpacaPos.UnrealizedPLPercent * 100; // Convert to percentage
                    existingPos.UpdatedAt = DateTime.UtcNow;

                    _logger.LogDebug(
                        "Updated position {symbol}: Qty={qty}, P/L=${pl} ({plPct}%)",
                        existingPos.Symbol,
                        existingPos.Quantity,
                        existingPos.UnrealizedPL,
                        existingPos.UnrealizedPLPercent);
                }
                else
                {
                    // Add new position
                    var newPos = new Position
                    {
                        PortfolioId = portfolioId,
                        Symbol = alpacaPos.Symbol,
                        Quantity = (int)alpacaPos.Quantity,
                        AverageCostBasis = alpacaPos.AverageCostBasis,
                        CurrentPrice = alpacaPos.CurrentPrice,
                        UnrealizedPL = alpacaPos.UnrealizedPL,
                        UnrealizedPLPercent = alpacaPos.UnrealizedPLPercent * 100,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _dbContext.Positions.Add(newPos);

                    _logger.LogInformation(
                        "Added new position {symbol}: Qty={qty}, Cost=${cost}",
                        newPos.Symbol,
                        newPos.Quantity,
                        newPos.AverageCostBasis);
                }
            }
        }

        /// <summary>
        /// Gets the current portfolio from database
        /// </summary>
        public async Task<Portfolio> GetCurrentPortfolioAsync()
        {
            var portfolio = await _dbContext.Portfolios
                .FirstOrDefaultAsync();

            if (portfolio == null)
            {
                _logger.LogWarning("No portfolio found in database");
            }

            return portfolio;
        }

        /// <summary>
        /// Updates portfolio value
        /// </summary>
        public async Task UpdatePortfolioValueAsync(decimal newValue)
        {
            var portfolio = await GetCurrentPortfolioAsync();
            if (portfolio != null)
            {
                portfolio.CurrentEquity = newValue;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Portfolio value updated to ${value}", newValue);
            }
        }

        /// <summary>
        /// Updates peak value if current value is higher
        /// </summary>
        public async Task UpdatePeakValueAsync(decimal currentValue)
        {
            var portfolio = await GetCurrentPortfolioAsync();
            if (portfolio != null && currentValue > portfolio.PeakValue)
            {
                portfolio.PeakValue = currentValue;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Peak value updated to ${value}", currentValue);
            }
        }

        /// <summary>
        /// Halts trading by setting flag and logging event
        /// </summary>
        public async Task HaltTradingAsync(string reason)
        {
            var portfolio = await GetCurrentPortfolioAsync();
            if (portfolio != null && !portfolio.IsTradingPaused)
            {
                portfolio.IsTradingPaused = true;
                portfolio.PausedReason = reason;

                // Log to audit trail
                _dbContext.AuditLog.Add(new AuditLog
                {
                    EventType = "TRADING_HALTED",
                    EventTimestamp = DateTime.UtcNow,
                    PortfolioId = portfolio.PortfolioId,
                    Description = reason,
                    Severity = "CRITICAL",
                    MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        CurrentEquity = portfolio.CurrentEquity,
                        PeakValue = portfolio.PeakValue,
                        DrawdownPercent = portfolio.CurrentDrawdownPercent
                    })
                });

                await _dbContext.SaveChangesAsync();

                _logger.LogCritical("TRADING HALTED: {reason}", reason);
            }
        }

        /// <summary>
        /// Resumes trading (manual action required)
        /// </summary>
        public async Task ResumeTradingAsync()
        {
            var portfolio = await GetCurrentPortfolioAsync();
            if (portfolio != null && portfolio.IsTradingPaused)
            {
                portfolio.IsTradingPaused = false;
                portfolio.PausedReason = null;

                // Log to audit trail
                _dbContext.AuditLog.Add(new AuditLog
                {
                    EventType = "TRADING_RESUMED",
                    EventTimestamp = DateTime.UtcNow,
                    PortfolioId = portfolio.PortfolioId,
                    Description = "Trading manually resumed",
                    Severity = "INFO"
                });

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Trading resumed manually");
            }
        }

        /// <summary>
        /// Updates current drawdown percentage
        /// </summary>
        public async Task UpdateDrawdownAsync(decimal drawdownPercent)
        {
            var portfolio = await GetCurrentPortfolioAsync();
            if (portfolio != null)
            {
                portfolio.CurrentDrawdownPercent = drawdownPercent;
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Updates holding periods for all positions
        /// </summary>
        public async Task UpdateHoldingPeriodsAsync()
        {
            var positions = await _dbContext.Positions.ToListAsync();

            foreach (var position in positions)
            {
                var daysSinceCreated = (DateTime.UtcNow - position.CreatedAt).Days;
                position.HoldingPeriodDays = daysSinceCreated;
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Updated holding periods for {count} positions", positions.Count);
        }
    }
}