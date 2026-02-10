using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Data;
using TradingSystem.Functions.Models;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services
{
    /// <summary>
    /// Service for managing portfolio state and positions
    /// </summary>
    public class PortfolioService : IPortfolioService
    {
        private readonly TradingDbContext _dbContext;
        private readonly ILogger<PortfolioService> _logger;

        public PortfolioService(TradingDbContext dbContext, ILogger<PortfolioService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current active portfolio
        /// </summary>
        public async Task<Portfolio?> GetCurrentPortfolioAsync()
        {
            return await _dbContext.Portfolios
                .FirstOrDefaultAsync(p => p.IsActive);
        }

        /// <summary>
        /// Syncs portfolio state with broker account data
        /// </summary>
        public async Task SyncPortfolioStateAsync(int portfolioId, AccountInfo accountInfo, List<PositionInfo> positions)
        {
            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId);
            if (portfolio == null)
            {
                throw new InvalidOperationException($"Portfolio {portfolioId} not found");
            }

            // Update portfolio with account data
            portfolio.CurrentCash = accountInfo.Cash;
            portfolio.CurrentEquity = accountInfo.Equity;
            portfolio.BuyingPower = accountInfo.BuyingPower;
            portfolio.LastSyncTimestamp = DateTime.UtcNow;

            // Update peak value if current equity is higher
            if (accountInfo.Equity > portfolio.PeakValue)
            {
                portfolio.PeakValue = accountInfo.Equity;
                _logger.LogInformation("New peak portfolio value: ${peak}", portfolio.PeakValue);
            }

            // Calculate current drawdown
            if (portfolio.PeakValue > 0)
            {
                portfolio.CurrentDrawdownPercent = ((portfolio.PeakValue - accountInfo.Equity) / portfolio.PeakValue) * 100;
            }

            // Sync positions
            await SyncPositionsAsync(portfolioId, positions);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Portfolio synced: Cash=${cash}, Equity=${equity}, Drawdown={drawdown}%",
                portfolio.CurrentCash, portfolio.CurrentEquity, portfolio.CurrentDrawdownPercent);
        }

        /// <summary>
        /// Syncs positions with broker data
        /// </summary>
        private async Task SyncPositionsAsync(int portfolioId, List<PositionInfo> brokerPositions)
        {
            // Get existing positions
            var existingPositions = await _dbContext.Positions
                .Where(p => p.PortfolioId == portfolioId)
                .ToListAsync();

            var brokerSymbols = brokerPositions.Select(p => p.Symbol).ToHashSet();

            // Remove positions that no longer exist at broker
            var positionsToRemove = existingPositions
                .Where(p => !brokerSymbols.Contains(p.Symbol))
                .ToList();

            if (positionsToRemove.Count > 0)
            {
                _dbContext.Positions.RemoveRange(positionsToRemove);
                _logger.LogInformation("Removed {count} closed positions", positionsToRemove.Count);
            }

            // Update or add positions
            foreach (var brokerPosition in brokerPositions)
            {
                var existingPosition = existingPositions.FirstOrDefault(p => p.Symbol == brokerPosition.Symbol);

                if (existingPosition != null)
                {
                    // Update existing position
                    existingPosition.Quantity = brokerPosition.Quantity;
                    existingPosition.AverageCostBasis = brokerPosition.AverageCostBasis;
                    existingPosition.CurrentPrice = brokerPosition.CurrentPrice;
                    existingPosition.UnrealizedProfitLoss = brokerPosition.UnrealizedPL;
                    existingPosition.UnrealizedProfitLossPercent = brokerPosition.UnrealizedPLPercent;
                    existingPosition.LastUpdated = DateTime.UtcNow;
                }
                else
                {
                    // Add new position
                    var newPosition = new Position
                    {
                        PortfolioId = portfolioId,
                        Symbol = brokerPosition.Symbol,
                        Quantity = brokerPosition.Quantity,
                        AverageCostBasis = brokerPosition.AverageCostBasis,
                        CurrentPrice = brokerPosition.CurrentPrice,
                        UnrealizedProfitLoss = brokerPosition.UnrealizedPL,
                        UnrealizedProfitLossPercent = brokerPosition.UnrealizedPLPercent,
                        OpenedAt = DateTime.UtcNow,
                        LastUpdated = DateTime.UtcNow
                    };
                    _dbContext.Positions.Add(newPosition);
                    _logger.LogInformation("Added new position: {symbol} x {qty}", brokerPosition.Symbol, brokerPosition.Quantity);
                }
            }
        }

        /// <summary>
        /// Halts trading for a portfolio
        /// </summary>
        public async Task HaltTradingAsync(int portfolioId, string reason)
        {
            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId);
            if (portfolio == null)
            {
                throw new InvalidOperationException($"Portfolio {portfolioId} not found");
            }

            portfolio.IsTradingPaused = true;
            portfolio.PausedReason = reason;

            // Log the halt event
            var auditLog = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                EventType = "TRADING_HALT",
                Severity = "CRITICAL",
                PortfolioId = portfolioId,
                Message = $"Trading halted: {reason}",
                AdditionalDataJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Reason = reason,
                    CurrentEquity = portfolio.CurrentEquity,
                    PeakValue = portfolio.PeakValue,
                    DrawdownPercent = portfolio.CurrentDrawdownPercent
                })
            };
            _dbContext.AuditLogs.Add(auditLog);

            await _dbContext.SaveChangesAsync();

            _logger.LogCritical("Trading halted for portfolio {id}: {reason}", portfolioId, reason);
        }

        /// <summary>
        /// Resumes trading for a portfolio
        /// </summary>
        public async Task ResumeTradingAsync(int portfolioId)
        {
            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId);
            if (portfolio == null)
            {
                throw new InvalidOperationException($"Portfolio {portfolioId} not found");
            }

            portfolio.IsTradingPaused = false;
            portfolio.PausedReason = null;

            // Log the resume event
            var auditLog = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                EventType = "TRADING_RESUME",
                Severity = "INFO",
                PortfolioId = portfolioId,
                Message = "Trading resumed",
                AdditionalDataJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    CurrentEquity = portfolio.CurrentEquity,
                    DrawdownPercent = portfolio.CurrentDrawdownPercent
                })
            };
            _dbContext.AuditLogs.Add(auditLog);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Trading resumed for portfolio {id}", portfolioId);
        }

        /// <summary>
        /// Updates portfolio drawdown and checks risk limits
        /// </summary>
        public async Task<DrawdownStatus> UpdateDrawdownAsync(int portfolioId)
        {
            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId);
            if (portfolio == null)
            {
                throw new InvalidOperationException($"Portfolio {portfolioId} not found");
            }

            // Calculate current drawdown
            var drawdownPercent = 0m;
            if (portfolio.PeakValue > 0)
            {
                drawdownPercent = ((portfolio.PeakValue - portfolio.CurrentEquity) / portfolio.PeakValue) * 100;
            }

            portfolio.CurrentDrawdownPercent = drawdownPercent;
            await _dbContext.SaveChangesAsync();

            // Determine status
            var status = new DrawdownStatus
            {
                CurrentDrawdownPercent = drawdownPercent,
                PeakValue = portfolio.PeakValue,
                CurrentValue = portfolio.CurrentEquity,
                ShouldHalt = drawdownPercent >= 20,
                ShouldWarn = drawdownPercent >= 15 && drawdownPercent < 20
            };

            return status;
        }

        /// <summary>
        /// Gets all current positions for a portfolio
        /// </summary>
        public async Task<List<Position>> GetPositionsAsync(int portfolioId)
        {
            return await _dbContext.Positions
                .Where(p => p.PortfolioId == portfolioId)
                .ToListAsync();
        }
    }

    /// <summary>
    /// Drawdown status for risk monitoring
    /// </summary>
    public class DrawdownStatus
    {
        public decimal CurrentDrawdownPercent { get; set; }
        public decimal PeakValue { get; set; }
        public decimal CurrentValue { get; set; }
        public bool ShouldHalt { get; set; }
        public bool ShouldWarn { get; set; }
    }
}