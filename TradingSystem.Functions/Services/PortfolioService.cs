using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Data;
using TradingSystem.Functions.Models;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly TradingDbContext _dbContext;
        private readonly ILogger<PortfolioService> _logger;

        public PortfolioService(TradingDbContext dbContext, ILogger<PortfolioService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Portfolio?> GetCurrentPortfolioAsync()
        {
            return await _dbContext.Portfolios.FirstOrDefaultAsync(p => p.IsActive);
        }

        public async Task SyncPortfolioStateAsync(AccountInfo accountInfo, List<PositionInfo> positions)
        {
            var portfolio = await GetCurrentPortfolioAsync()
                ?? throw new InvalidOperationException("No active portfolio found");
            await SyncPortfolioStateAsync(portfolio.PortfolioId, accountInfo, positions);
        }

        public async Task SyncPortfolioStateAsync(int portfolioId, AccountInfo accountInfo, List<PositionInfo> positions)
        {
            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId)
                ?? throw new InvalidOperationException($"Portfolio {portfolioId} not found");

            portfolio.CurrentCash = accountInfo.Cash;
            portfolio.CurrentEquity = accountInfo.Equity;
            portfolio.BuyingPower = accountInfo.BuyingPower;
            portfolio.LastSyncTimestamp = DateTime.UtcNow;
            portfolio.LastUpdated = DateTime.UtcNow;

            if (accountInfo.Equity > portfolio.PeakValue)
            {
                portfolio.PeakValue = accountInfo.Equity;
                _logger.LogInformation("New peak portfolio value: ${peak}", portfolio.PeakValue);
            }

            if (portfolio.PeakValue > 0)
            {
                portfolio.CurrentDrawdownPercent = ((portfolio.PeakValue - accountInfo.Equity) / portfolio.PeakValue) * 100;
            }

            await SyncPositionsAsync(portfolioId, positions);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Portfolio synced: Cash=${cash}, Equity=${equity}, Drawdown={drawdown}%",
                portfolio.CurrentCash, portfolio.CurrentEquity, portfolio.CurrentDrawdownPercent);
        }

        private async Task SyncPositionsAsync(int portfolioId, List<PositionInfo> brokerPositions)
        {
            var existingPositions = await _dbContext.Positions
                .Where(p => p.PortfolioId == portfolioId)
                .ToListAsync();

            var brokerSymbols = brokerPositions.Select(p => p.Symbol).ToHashSet();

            var positionsToRemove = existingPositions.Where(p => !brokerSymbols.Contains(p.Symbol)).ToList();
            if (positionsToRemove.Count > 0)
            {
                _dbContext.Positions.RemoveRange(positionsToRemove);
                _logger.LogInformation("Removed {count} closed positions", positionsToRemove.Count);
            }

            foreach (var brokerPosition in brokerPositions)
            {
                var existingPosition = existingPositions.FirstOrDefault(p => p.Symbol == brokerPosition.Symbol);

                if (existingPosition != null)
                {
                    // Cast decimal to int for Position.Quantity if needed
                    existingPosition.Quantity = (int)brokerPosition.Quantity;
                    existingPosition.AverageCostBasis = brokerPosition.AverageCostBasis;
                    existingPosition.CurrentPrice = brokerPosition.CurrentPrice;
                    existingPosition.UnrealizedProfitLoss = brokerPosition.UnrealizedPL;
                    existingPosition.UnrealizedProfitLossPercent = brokerPosition.UnrealizedPLPercent;
                    existingPosition.LastUpdated = DateTime.UtcNow;
                }
                else
                {
                    var newPosition = new Position
                    {
                        PortfolioId = portfolioId,
                        Symbol = brokerPosition.Symbol,
                        Quantity = (int)brokerPosition.Quantity,  // Cast to int
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

        public async Task HaltTradingAsync(string reason)
        {
            var portfolio = await GetCurrentPortfolioAsync()
                ?? throw new InvalidOperationException("No active portfolio found");
            await HaltTradingAsync(portfolio.PortfolioId, reason);
        }

        public async Task HaltTradingAsync(int portfolioId, string reason)
        {
            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId)
                ?? throw new InvalidOperationException($"Portfolio {portfolioId} not found");

            portfolio.IsTradingPaused = true;
            portfolio.PausedReason = reason;

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
            _dbContext.AuditLog.Add(auditLog);

            await _dbContext.SaveChangesAsync();
            _logger.LogCritical("Trading halted for portfolio {id}: {reason}", portfolioId, reason);
        }

        public async Task ResumeTradingAsync(int portfolioId)
        {
            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId)
                ?? throw new InvalidOperationException($"Portfolio {portfolioId} not found");

            portfolio.IsTradingPaused = false;
            portfolio.PausedReason = null;

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
            _dbContext.AuditLog.Add(auditLog);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Trading resumed for portfolio {id}", portfolioId);
        }

        public async Task<DrawdownStatus> UpdateDrawdownAsync(int portfolioId)
        {
            var portfolio = await _dbContext.Portfolios.FindAsync(portfolioId)
                ?? throw new InvalidOperationException($"Portfolio {portfolioId} not found");

            decimal drawdownPercent = 0m;
            if (portfolio.PeakValue > 0)
            {
                drawdownPercent = ((portfolio.PeakValue - portfolio.CurrentEquity) / portfolio.PeakValue) * 100;
            }

            portfolio.CurrentDrawdownPercent = drawdownPercent;
            await _dbContext.SaveChangesAsync();

            return new DrawdownStatus
            {
                CurrentDrawdownPercent = drawdownPercent,
                PeakValue = portfolio.PeakValue,
                CurrentValue = portfolio.CurrentEquity,
                ShouldHalt = drawdownPercent >= 20,
                ShouldWarn = drawdownPercent >= 15 && drawdownPercent < 20
            };
        }

        public async Task UpdateHoldingPeriodsAsync()
        {
            var portfolio = await GetCurrentPortfolioAsync();
            if (portfolio == null) return;

            var positions = await _dbContext.Positions
                .Where(p => p.PortfolioId == portfolio.PortfolioId)
                .ToListAsync();

            foreach (var position in positions)
            {
                var holdingDays = (DateTime.UtcNow - position.OpenedAt).Days;
                _logger.LogDebug("Position {symbol}: Holding for {days} days", position.Symbol, holdingDays);
            }

            _logger.LogInformation("Updated holding periods for {count} positions", positions.Count);
        }

        public async Task<List<Position>> GetPositionsAsync(int portfolioId)
        {
            return await _dbContext.Positions
                .Where(p => p.PortfolioId == portfolioId)
                .ToListAsync();
        }
    }
}