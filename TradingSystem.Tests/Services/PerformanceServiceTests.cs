using FluentAssertions;
using Xunit;

namespace TradingSystem.Tests.Services;

/// <summary>
/// Unit tests for Performance calculations including returns, Sharpe ratio, and win rate.
/// </summary>
public class PerformanceServiceTests
{
    #region Return Calculation Tests

    [Fact]
    public void CalculateDailyReturn_PositiveDay_ShouldBePositive()
    {
        // Arrange
        var previousValue = 10000m;
        var currentValue = 10100m;

        // Act
        var dailyReturn = CalculateDailyReturn(previousValue, currentValue);

        // Assert
        dailyReturn.Should().Be(1m); // 1% gain
    }

    [Fact]
    public void CalculateDailyReturn_NegativeDay_ShouldBeNegative()
    {
        // Arrange
        var previousValue = 10000m;
        var currentValue = 9900m;

        // Act
        var dailyReturn = CalculateDailyReturn(previousValue, currentValue);

        // Assert
        dailyReturn.Should().Be(-1m); // 1% loss
    }

    [Fact]
    public void CalculateDailyReturn_NoChange_ShouldBeZero()
    {
        // Arrange
        var previousValue = 10000m;
        var currentValue = 10000m;

        // Act
        var dailyReturn = CalculateDailyReturn(previousValue, currentValue);

        // Assert
        dailyReturn.Should().Be(0m);
    }

    [Fact]
    public void CalculateDailyReturn_ZeroPreviousValue_ShouldHandleGracefully()
    {
        // Arrange
        var previousValue = 0m;
        var currentValue = 10000m;

        // Act
        var dailyReturn = CalculateDailyReturn(previousValue, currentValue);

        // Assert
        dailyReturn.Should().Be(0m);
    }

    #endregion

    #region Total Return Tests

    [Fact]
    public void CalculateTotalReturn_Doubled_ShouldBe100Percent()
    {
        // Arrange
        var initialCapital = 1000m;
        var currentValue = 2000m;

        // Act
        var totalReturn = CalculateTotalReturn(initialCapital, currentValue);

        // Assert
        totalReturn.Should().Be(100m);
    }

    [Fact]
    public void CalculateTotalReturn_LostHalf_ShouldBeMinus50Percent()
    {
        // Arrange
        var initialCapital = 1000m;
        var currentValue = 500m;

        // Act
        var totalReturn = CalculateTotalReturn(initialCapital, currentValue);

        // Assert
        totalReturn.Should().Be(-50m);
    }

    [Fact]
    public void CalculateTotalReturn_NoChange_ShouldBeZero()
    {
        // Arrange
        var initialCapital = 1000m;
        var currentValue = 1000m;

        // Act
        var totalReturn = CalculateTotalReturn(initialCapital, currentValue);

        // Assert
        totalReturn.Should().Be(0m);
    }

    #endregion

    #region Win Rate Tests

    [Fact]
    public void CalculateWinRate_AllWinners_ShouldBe100()
    {
        // Arrange
        var winningTrades = 10;
        var totalTrades = 10;

        // Act
        var winRate = CalculateWinRate(winningTrades, totalTrades);

        // Assert
        winRate.Should().Be(100m);
    }

    [Fact]
    public void CalculateWinRate_AllLosers_ShouldBeZero()
    {
        // Arrange
        var winningTrades = 0;
        var totalTrades = 10;

        // Act
        var winRate = CalculateWinRate(winningTrades, totalTrades);

        // Assert
        winRate.Should().Be(0m);
    }

    [Fact]
    public void CalculateWinRate_HalfWinners_ShouldBe50()
    {
        // Arrange
        var winningTrades = 5;
        var totalTrades = 10;

        // Act
        var winRate = CalculateWinRate(winningTrades, totalTrades);

        // Assert
        winRate.Should().Be(50m);
    }

    [Fact]
    public void CalculateWinRate_NoTrades_ShouldBeZero()
    {
        // Arrange
        var winningTrades = 0;
        var totalTrades = 0;

        // Act
        var winRate = CalculateWinRate(winningTrades, totalTrades);

        // Assert
        winRate.Should().Be(0m);
    }

    #endregion

    #region Average Gain/Loss Tests

    [Fact]
    public void CalculateAverageGain_WithWinners_ShouldReturnCorrectAverage()
    {
        // Arrange
        var gains = new decimal[] { 100, 150, 200, 50 };

        // Act
        var avgGain = CalculateAverageGain(gains);

        // Assert
        avgGain.Should().Be(125m); // (100+150+200+50) / 4
    }

    [Fact]
    public void CalculateAverageGain_NoWinners_ShouldReturnZero()
    {
        // Arrange
        var gains = Array.Empty<decimal>();

        // Act
        var avgGain = CalculateAverageGain(gains);

        // Assert
        avgGain.Should().Be(0m);
    }

    [Fact]
    public void CalculateAverageLoss_WithLosers_ShouldReturnPositiveValue()
    {
        // Arrange
        var losses = new decimal[] { -100, -150, -200, -50 };

        // Act
        var avgLoss = CalculateAverageLoss(losses);

        // Assert
        avgLoss.Should().Be(125m); // Average of absolute values
    }

    #endregion

    #region Sharpe Ratio Tests

    [Fact]
    public void CalculateSharpeRatio_PositiveExcessReturn_ShouldBePositive()
    {
        // Arrange
        var annualizedReturn = 15m;  // 15% return
        var riskFreeRate = 5m;       // 5% risk-free rate
        var standardDeviation = 10m; // 10% standard deviation

        // Act
        var sharpe = CalculateSharpeRatio(annualizedReturn, riskFreeRate, standardDeviation);

        // Assert
        sharpe.Should().Be(1m); // (15-5)/10 = 1.0
    }

    [Fact]
    public void CalculateSharpeRatio_NegativeExcessReturn_ShouldBeNegative()
    {
        // Arrange
        var annualizedReturn = 3m;   // 3% return
        var riskFreeRate = 5m;       // 5% risk-free rate
        var standardDeviation = 10m;

        // Act
        var sharpe = CalculateSharpeRatio(annualizedReturn, riskFreeRate, standardDeviation);

        // Assert
        sharpe.Should().Be(-0.2m); // (3-5)/10 = -0.2
    }

    [Fact]
    public void CalculateSharpeRatio_ZeroStdDev_ShouldReturnZero()
    {
        // Arrange
        var annualizedReturn = 15m;
        var riskFreeRate = 5m;
        var standardDeviation = 0m;

        // Act
        var sharpe = CalculateSharpeRatio(annualizedReturn, riskFreeRate, standardDeviation);

        // Assert
        sharpe.Should().Be(0m);
    }

    [Fact]
    public void CalculateSharpeRatio_HighQuality_ShouldBeAbove1()
    {
        // Arrange - Good risk-adjusted return
        var annualizedReturn = 20m;
        var riskFreeRate = 5m;
        var standardDeviation = 10m;

        // Act
        var sharpe = CalculateSharpeRatio(annualizedReturn, riskFreeRate, standardDeviation);

        // Assert
        sharpe.Should().BeGreaterThan(1m); // 1.5
    }

    #endregion

    #region Benchmark Comparison Tests

    [Fact]
    public void CalculateAlpha_OutperformingBenchmark_ShouldBePositive()
    {
        // Arrange
        var portfolioReturn = 15m;
        var benchmarkReturn = 10m;

        // Act
        var alpha = CalculateAlpha(portfolioReturn, benchmarkReturn);

        // Assert
        alpha.Should().Be(5m);
    }

    [Fact]
    public void CalculateAlpha_UnderperformingBenchmark_ShouldBeNegative()
    {
        // Arrange
        var portfolioReturn = 8m;
        var benchmarkReturn = 10m;

        // Act
        var alpha = CalculateAlpha(portfolioReturn, benchmarkReturn);

        // Assert
        alpha.Should().Be(-2m);
    }

    [Fact]
    public void CalculateAlpha_MatchingBenchmark_ShouldBeZero()
    {
        // Arrange
        var portfolioReturn = 10m;
        var benchmarkReturn = 10m;

        // Act
        var alpha = CalculateAlpha(portfolioReturn, benchmarkReturn);

        // Assert
        alpha.Should().Be(0m);
    }

    #endregion

    #region Max Drawdown Tests

    [Fact]
    public void CalculateMaxDrawdown_WithDrawdown_ShouldReturnCorrectValue()
    {
        // Arrange - Peak at 10000, drops to 8000, recovers to 9000
        var portfolioValues = new decimal[] { 10000, 9500, 9000, 8500, 8000, 8500, 9000 };

        // Act
        var maxDrawdown = CalculateMaxDrawdown(portfolioValues);

        // Assert
        maxDrawdown.Should().Be(-20m); // 20% drawdown from 10000 to 8000
    }

    [Fact]
    public void CalculateMaxDrawdown_NoDrawdown_ShouldBeZero()
    {
        // Arrange - Always increasing
        var portfolioValues = new decimal[] { 10000, 10100, 10200, 10300, 10400 };

        // Act
        var maxDrawdown = CalculateMaxDrawdown(portfolioValues);

        // Assert
        maxDrawdown.Should().Be(0m);
    }

    [Fact]
    public void CalculateMaxDrawdown_MultipleDrawdowns_ShouldReturnLargest()
    {
        // Arrange - First drawdown 10%, second drawdown 15%
        var portfolioValues = new decimal[] 
        { 
            10000, 9500, 9000, 9500, 10000,  // 10% drawdown
            10500, 10000, 9500, 8925, 9500   // 15% drawdown from 10500
        };

        // Act
        var maxDrawdown = CalculateMaxDrawdown(portfolioValues);

        // Assert
        maxDrawdown.Should().Be(-15m);
    }

    #endregion

    #region Trade Statistics Tests

    [Fact]
    public void CalculateTradeStatistics_MixedTrades_ShouldCalculateCorrectly()
    {
        // Arrange
        var trades = new[]
        {
            new TradeResult { ProfitLoss = 100, IsWinner = true },
            new TradeResult { ProfitLoss = -50, IsWinner = false },
            new TradeResult { ProfitLoss = 200, IsWinner = true },
            new TradeResult { ProfitLoss = -75, IsWinner = false },
            new TradeResult { ProfitLoss = 150, IsWinner = true },
        };

        // Act
        var stats = CalculateTradeStatistics(trades);

        // Assert
        stats.TotalTrades.Should().Be(5);
        stats.WinningTrades.Should().Be(3);
        stats.LosingTrades.Should().Be(2);
        stats.WinRate.Should().Be(60m);
        stats.AverageGain.Should().Be(150m); // (100+200+150)/3
        stats.AverageLoss.Should().Be(62.5m); // (50+75)/2
        stats.TotalProfitLoss.Should().Be(325m);
    }

    #endregion

    #region Helper Methods

    private decimal CalculateDailyReturn(decimal previousValue, decimal currentValue)
    {
        if (previousValue == 0) return 0;
        return Math.Round(((currentValue - previousValue) / previousValue) * 100, 2);
    }

    private decimal CalculateTotalReturn(decimal initialCapital, decimal currentValue)
    {
        if (initialCapital == 0) return 0;
        return Math.Round(((currentValue - initialCapital) / initialCapital) * 100, 2);
    }

    private decimal CalculateWinRate(int winningTrades, int totalTrades)
    {
        if (totalTrades == 0) return 0;
        return Math.Round((decimal)winningTrades / totalTrades * 100, 2);
    }

    private decimal CalculateAverageGain(decimal[] gains)
    {
        if (gains.Length == 0) return 0;
        return gains.Average();
    }

    private decimal CalculateAverageLoss(decimal[] losses)
    {
        if (losses.Length == 0) return 0;
        return Math.Abs(losses.Average());
    }

    private decimal CalculateSharpeRatio(decimal annualizedReturn, decimal riskFreeRate, decimal standardDeviation)
    {
        if (standardDeviation == 0) return 0;
        return Math.Round((annualizedReturn - riskFreeRate) / standardDeviation, 2);
    }

    private decimal CalculateAlpha(decimal portfolioReturn, decimal benchmarkReturn)
    {
        return portfolioReturn - benchmarkReturn;
    }

    private decimal CalculateMaxDrawdown(decimal[] portfolioValues)
    {
        if (portfolioValues.Length == 0) return 0;

        decimal peak = portfolioValues[0];
        decimal maxDrawdown = 0;

        foreach (var value in portfolioValues)
        {
            if (value > peak)
            {
                peak = value;
            }
            else
            {
                var drawdown = ((value - peak) / peak) * 100;
                if (drawdown < maxDrawdown)
                {
                    maxDrawdown = drawdown;
                }
            }
        }

        return Math.Round(maxDrawdown, 2);
    }

    private TradeStatistics CalculateTradeStatistics(TradeResult[] trades)
    {
        var winners = trades.Where(t => t.IsWinner).ToArray();
        var losers = trades.Where(t => !t.IsWinner).ToArray();

        return new TradeStatistics
        {
            TotalTrades = trades.Length,
            WinningTrades = winners.Length,
            LosingTrades = losers.Length,
            WinRate = trades.Length > 0 ? Math.Round((decimal)winners.Length / trades.Length * 100, 2) : 0,
            AverageGain = winners.Length > 0 ? winners.Average(t => t.ProfitLoss) : 0,
            AverageLoss = losers.Length > 0 ? Math.Abs(losers.Average(t => t.ProfitLoss)) : 0,
            TotalProfitLoss = trades.Sum(t => t.ProfitLoss)
        };
    }

    #endregion

    #region Test Helper Classes

    private class TradeResult
    {
        public decimal ProfitLoss { get; set; }
        public bool IsWinner { get; set; }
    }

    private class TradeStatistics
    {
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal WinRate { get; set; }
        public decimal AverageGain { get; set; }
        public decimal AverageLoss { get; set; }
        public decimal TotalProfitLoss { get; set; }
    }

    #endregion
}
