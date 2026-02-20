using FluentAssertions;
using Xunit;

namespace TradingSystem.Tests.Services;

/// <summary>
/// Unit tests for Portfolio calculations and risk management.
/// </summary>
public class PortfolioServiceTests
{
    #region Drawdown Calculation Tests

    [Fact]
    public void CalculateDrawdown_AtPeak_ShouldBeZero()
    {
        // Arrange
        var currentValue = 10000m;
        var peakValue = 10000m;

        // Act
        var drawdown = CalculateDrawdownPercent(currentValue, peakValue);

        // Assert
        drawdown.Should().Be(0m);
    }

    [Fact]
    public void CalculateDrawdown_At20PercentLoss_ShouldBeMinus20()
    {
        // Arrange
        var currentValue = 8000m;
        var peakValue = 10000m;

        // Act
        var drawdown = CalculateDrawdownPercent(currentValue, peakValue);

        // Assert
        drawdown.Should().Be(-20m);
    }

    [Fact]
    public void CalculateDrawdown_At15PercentLoss_ShouldTriggerWarning()
    {
        // Arrange
        var currentValue = 8500m;
        var peakValue = 10000m;
        const decimal WARNING_THRESHOLD = -15m;

        // Act
        var drawdown = CalculateDrawdownPercent(currentValue, peakValue);
        var shouldWarn = drawdown <= WARNING_THRESHOLD;

        // Assert
        drawdown.Should().Be(-15m);
        shouldWarn.Should().BeTrue();
    }

    [Fact]
    public void CalculateDrawdown_AboveWarningThreshold_ShouldNotTriggerWarning()
    {
        // Arrange
        var currentValue = 9000m;
        var peakValue = 10000m;
        const decimal WARNING_THRESHOLD = -15m;

        // Act
        var drawdown = CalculateDrawdownPercent(currentValue, peakValue);
        var shouldWarn = drawdown <= WARNING_THRESHOLD;

        // Assert
        drawdown.Should().Be(-10m);
        shouldWarn.Should().BeFalse();
    }

    [Fact]
    public void CalculateDrawdown_WithZeroPeak_ShouldReturnZero()
    {
        // Arrange
        var currentValue = 1000m;
        var peakValue = 0m;

        // Act
        var drawdown = CalculateDrawdownPercent(currentValue, peakValue);

        // Assert
        drawdown.Should().Be(0m);
    }

    #endregion

    #region Peak Value Tracking Tests

    [Fact]
    public void UpdatePeakValue_WhenCurrentExceedsPeak_ShouldUpdate()
    {
        // Arrange
        var currentValue = 11000m;
        var peakValue = 10000m;

        // Act
        var newPeak = UpdatePeakValue(currentValue, peakValue);

        // Assert
        newPeak.Should().Be(11000m);
    }

    [Fact]
    public void UpdatePeakValue_WhenCurrentBelowPeak_ShouldNotChange()
    {
        // Arrange
        var currentValue = 9000m;
        var peakValue = 10000m;

        // Act
        var newPeak = UpdatePeakValue(currentValue, peakValue);

        // Assert
        newPeak.Should().Be(10000m);
    }

    [Fact]
    public void UpdatePeakValue_WhenEqual_ShouldNotChange()
    {
        // Arrange
        var currentValue = 10000m;
        var peakValue = 10000m;

        // Act
        var newPeak = UpdatePeakValue(currentValue, peakValue);

        // Assert
        newPeak.Should().Be(10000m);
    }

    #endregion

    #region Position Sizing Tests

    [Fact]
    public void CalculatePositionSize_WithinLimit_ShouldReturnRequestedSize()
    {
        // Arrange
        var portfolioValue = 10000m;
        var requestedAmount = 500m;
        var maxPositionPercent = 10m; // 10% max

        // Act
        var actualSize = CalculateAllowedPositionSize(portfolioValue, requestedAmount, maxPositionPercent);

        // Assert
        actualSize.Should().Be(500m);
    }

    [Fact]
    public void CalculatePositionSize_ExceedsLimit_ShouldCapAtMaxPercent()
    {
        // Arrange
        var portfolioValue = 10000m;
        var requestedAmount = 2000m;
        var maxPositionPercent = 10m; // 10% max = $1000

        // Act
        var actualSize = CalculateAllowedPositionSize(portfolioValue, requestedAmount, maxPositionPercent);

        // Assert
        actualSize.Should().Be(1000m);
    }

    [Fact]
    public void CalculatePositionSize_ExactlyAtLimit_ShouldReturnRequestedSize()
    {
        // Arrange
        var portfolioValue = 10000m;
        var requestedAmount = 1000m;
        var maxPositionPercent = 10m; // 10% max = $1000

        // Act
        var actualSize = CalculateAllowedPositionSize(portfolioValue, requestedAmount, maxPositionPercent);

        // Assert
        actualSize.Should().Be(1000m);
    }

    #endregion

    #region Unrealized P&L Tests

    [Fact]
    public void CalculateUnrealizedPL_WithGain_ShouldBePositive()
    {
        // Arrange
        var quantity = 10;
        var avgCost = 100m;
        var currentPrice = 110m;

        // Act
        var (pl, plPercent) = CalculateUnrealizedPL(quantity, avgCost, currentPrice);

        // Assert
        pl.Should().Be(100m); // 10 shares * $10 gain
        plPercent.Should().Be(10m); // 10% gain
    }

    [Fact]
    public void CalculateUnrealizedPL_WithLoss_ShouldBeNegative()
    {
        // Arrange
        var quantity = 10;
        var avgCost = 100m;
        var currentPrice = 90m;

        // Act
        var (pl, plPercent) = CalculateUnrealizedPL(quantity, avgCost, currentPrice);

        // Assert
        pl.Should().Be(-100m); // 10 shares * $10 loss
        plPercent.Should().Be(-10m); // 10% loss
    }

    [Fact]
    public void CalculateUnrealizedPL_NoChange_ShouldBeZero()
    {
        // Arrange
        var quantity = 10;
        var avgCost = 100m;
        var currentPrice = 100m;

        // Act
        var (pl, plPercent) = CalculateUnrealizedPL(quantity, avgCost, currentPrice);

        // Assert
        pl.Should().Be(0m);
        plPercent.Should().Be(0m);
    }

    #endregion

    #region Trading Halt Tests

    [Fact]
    public void ShouldHaltTrading_AtExactly20PercentDrawdown_ShouldReturnTrue()
    {
        // Arrange
        var drawdownPercent = -20m;
        const decimal HALT_THRESHOLD = -20m;

        // Act
        var shouldHalt = ShouldHaltTrading(drawdownPercent, HALT_THRESHOLD);

        // Assert
        shouldHalt.Should().BeTrue();
    }

    [Fact]
    public void ShouldHaltTrading_Beyond20PercentDrawdown_ShouldReturnTrue()
    {
        // Arrange
        var drawdownPercent = -25m;
        const decimal HALT_THRESHOLD = -20m;

        // Act
        var shouldHalt = ShouldHaltTrading(drawdownPercent, HALT_THRESHOLD);

        // Assert
        shouldHalt.Should().BeTrue();
    }

    [Fact]
    public void ShouldHaltTrading_Under20PercentDrawdown_ShouldReturnFalse()
    {
        // Arrange
        var drawdownPercent = -19m;
        const decimal HALT_THRESHOLD = -20m;

        // Act
        var shouldHalt = ShouldHaltTrading(drawdownPercent, HALT_THRESHOLD);

        // Assert
        shouldHalt.Should().BeFalse();
    }

    #endregion

    #region Holding Period Tests

    [Fact]
    public void CalculateHoldingPeriod_SameDay_ShouldBeZero()
    {
        // Arrange
        var purchaseDate = DateTime.Today;
        var currentDate = DateTime.Today;

        // Act
        var holdingPeriod = CalculateHoldingPeriod(purchaseDate, currentDate);

        // Assert
        holdingPeriod.Should().Be(0);
    }

    [Fact]
    public void CalculateHoldingPeriod_OneWeek_ShouldBe7()
    {
        // Arrange
        var purchaseDate = DateTime.Today.AddDays(-7);
        var currentDate = DateTime.Today;

        // Act
        var holdingPeriod = CalculateHoldingPeriod(purchaseDate, currentDate);

        // Assert
        holdingPeriod.Should().Be(7);
    }

    [Fact]
    public void CalculateHoldingPeriod_OneMonth_ShouldBeApproximately30()
    {
        // Arrange
        var purchaseDate = DateTime.Today.AddMonths(-1);
        var currentDate = DateTime.Today;

        // Act
        var holdingPeriod = CalculateHoldingPeriod(purchaseDate, currentDate);

        // Assert
        holdingPeriod.Should().BeInRange(28, 31);
    }

    #endregion

    #region Diversification Tests

    [Fact]
    public void IsWithinDiversificationLimit_UnderLimit_ShouldReturnTrue()
    {
        // Arrange
        var currentPositionCount = 5;
        var maxPositions = 10;

        // Act
        var isWithinLimit = IsWithinDiversificationLimit(currentPositionCount, maxPositions);

        // Assert
        isWithinLimit.Should().BeTrue();
    }

    [Fact]
    public void IsWithinDiversificationLimit_AtLimit_ShouldReturnFalse()
    {
        // Arrange
        var currentPositionCount = 10;
        var maxPositions = 10;

        // Act
        var isWithinLimit = IsWithinDiversificationLimit(currentPositionCount, maxPositions);

        // Assert
        isWithinLimit.Should().BeFalse();
    }

    [Fact]
    public void IsWithinDiversificationLimit_OverLimit_ShouldReturnFalse()
    {
        // Arrange
        var currentPositionCount = 12;
        var maxPositions = 10;

        // Act
        var isWithinLimit = IsWithinDiversificationLimit(currentPositionCount, maxPositions);

        // Assert
        isWithinLimit.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private decimal CalculateDrawdownPercent(decimal currentValue, decimal peakValue)
    {
        if (peakValue == 0) return 0m;
        return Math.Round(((currentValue - peakValue) / peakValue) * 100, 2);
    }

    private decimal UpdatePeakValue(decimal currentValue, decimal peakValue)
    {
        return Math.Max(currentValue, peakValue);
    }

    private decimal CalculateAllowedPositionSize(decimal portfolioValue, decimal requestedAmount, decimal maxPositionPercent)
    {
        var maxAllowed = portfolioValue * (maxPositionPercent / 100);
        return Math.Min(requestedAmount, maxAllowed);
    }

    private (decimal pl, decimal plPercent) CalculateUnrealizedPL(int quantity, decimal avgCost, decimal currentPrice)
    {
        var costBasis = quantity * avgCost;
        var currentValue = quantity * currentPrice;
        var pl = currentValue - costBasis;
        var plPercent = costBasis > 0 ? (pl / costBasis) * 100 : 0;
        return (pl, Math.Round(plPercent, 2));
    }

    private bool ShouldHaltTrading(decimal drawdownPercent, decimal haltThreshold)
    {
        return drawdownPercent <= haltThreshold;
    }

    private int CalculateHoldingPeriod(DateTime purchaseDate, DateTime currentDate)
    {
        return (currentDate - purchaseDate).Days;
    }

    private bool IsWithinDiversificationLimit(int currentPositionCount, int maxPositions)
    {
        return currentPositionCount < maxPositions;
    }

    #endregion
}
