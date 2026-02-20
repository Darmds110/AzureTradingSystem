using FluentAssertions;
using Xunit;

namespace TradingSystem.Tests.Services;

/// <summary>
/// Unit tests for Risk Management rules and validations.
/// Tests the trading rules defined in the requirements (REQ-2.5.x).
/// </summary>
public class RiskManagementTests
{
    #region Position Size Validation (REQ-2.5.4)

    [Theory]
    [InlineData(10000, 500, 10, true)]   // 5% of portfolio, under 10% limit
    [InlineData(10000, 1000, 10, true)]  // Exactly 10%, at limit
    [InlineData(10000, 1500, 10, false)] // 15%, exceeds limit
    [InlineData(10000, 2000, 10, false)] // 20%, exceeds limit
    public void ValidatePositionSize_ShouldEnforceMaxPositionPercent(
        decimal portfolioValue, decimal positionValue, decimal maxPercent, bool expectedValid)
    {
        // Act
        var isValid = ValidatePositionSize(portfolioValue, positionValue, maxPercent);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Fact]
    public void ValidatePositionSize_WithDefaultLimit_ShouldBe10Percent()
    {
        // Arrange
        var portfolioValue = 10000m;
        var positionValue = 1001m; // Just over 10%
        var defaultMaxPercent = 10m;

        // Act
        var isValid = ValidatePositionSize(portfolioValue, positionValue, defaultMaxPercent);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Concurrent Positions Validation (REQ-2.5.4)

    [Theory]
    [InlineData(5, 10, true)]   // 5 positions, max 10
    [InlineData(9, 10, true)]   // 9 positions, max 10
    [InlineData(10, 10, false)] // At max, can't add more
    [InlineData(11, 10, false)] // Over max
    public void ValidateConcurrentPositions_ShouldEnforceLimit(
        int currentPositions, int maxPositions, bool canAddMore)
    {
        // Act
        var isValid = CanAddNewPosition(currentPositions, maxPositions);

        // Assert
        isValid.Should().Be(canAddMore);
    }

    [Fact]
    public void ValidateConcurrentPositions_DefaultMax_ShouldBe10()
    {
        // Arrange
        var currentPositions = 10;
        var defaultMax = 10;

        // Act
        var canAdd = CanAddNewPosition(currentPositions, defaultMax);

        // Assert
        canAdd.Should().BeFalse();
    }

    #endregion

    #region Daily Loss Limit (REQ-2.5.5)

    [Theory]
    [InlineData(10000, 9600, 5, true)]  // 4% loss, under 5% limit
    [InlineData(10000, 9500, 5, false)] // Exactly 5%, at limit
    [InlineData(10000, 9400, 5, false)] // 6% loss, exceeds limit
    public void ValidateDailyLoss_ShouldEnforceLimit(
        decimal dayStartValue, decimal currentValue, decimal maxLossPercent, bool canContinueTrading)
    {
        // Act
        var result = CanContinueTradingToday(dayStartValue, currentValue, maxLossPercent);

        // Assert
        result.Should().Be(canContinueTrading);
    }

    [Fact]
    public void ValidateDailyLoss_AtWarningThreshold_ShouldWarn()
    {
        // Arrange - 80% of 5% limit = 4% loss
        var dayStartValue = 10000m;
        var currentValue = 9600m; // 4% loss
        var warningThreshold = 80m; // Warn at 80% of limit
        var maxLossPercent = 5m;

        // Act
        var shouldWarn = ShouldWarnDailyLoss(dayStartValue, currentValue, maxLossPercent, warningThreshold);

        // Assert
        shouldWarn.Should().BeTrue();
    }

    #endregion

    #region Daily Trade Limit (REQ-2.5.5)

    [Theory]
    [InlineData(15, 20, true)]  // Under limit
    [InlineData(19, 20, true)]  // One more allowed
    [InlineData(20, 20, false)] // At limit
    [InlineData(21, 20, false)] // Over limit
    public void ValidateDailyTradeCount_ShouldEnforceLimit(
        int tradesToday, int maxTrades, bool canTradeMore)
    {
        // Act
        var result = CanExecuteMoreTrades(tradesToday, maxTrades);

        // Assert
        result.Should().Be(canTradeMore);
    }

    #endregion

    #region Pattern Day Trader Rules (REQ-2.5.6)

    [Theory]
    [InlineData(2, 25000, false)] // 2 day trades, equity over 25k - no restriction
    [InlineData(3, 25000, false)] // 3 day trades, equity over 25k - no restriction
    [InlineData(4, 25000, false)] // 4 day trades, equity over 25k - no restriction (PDT allowed)
    [InlineData(2, 20000, false)] // 2 day trades, equity under 25k - OK
    [InlineData(3, 20000, true)]  // 3 day trades, equity under 25k - WARNING (one more = PDT)
    [InlineData(4, 20000, true)]  // 4 day trades, equity under 25k - BLOCKED (PDT triggered)
    public void ValidatePDTRule_ShouldProtectSmallAccounts(
        int dayTradesInLast5Days, decimal accountEquity, bool shouldBlock)
    {
        // Act
        var result = ShouldBlockForPDT(dayTradesInLast5Days, accountEquity);

        // Assert
        result.Should().Be(shouldBlock);
    }

    [Fact]
    public void ValidatePDTRule_ApproachingLimit_ShouldWarn()
    {
        // Arrange
        var dayTradesInLast5Days = 3;
        var accountEquity = 20000m; // Under $25k threshold

        // Act
        var shouldWarn = ShouldWarnPDT(dayTradesInLast5Days, accountEquity);

        // Assert
        shouldWarn.Should().BeTrue();
    }

    #endregion

    #region Drawdown Thresholds (REQ-2.5.1, REQ-2.5.2, REQ-2.5.3)

    [Theory]
    [InlineData(-10, false, false)] // 10% drawdown - no action
    [InlineData(-14, false, false)] // 14% drawdown - no action
    [InlineData(-15, true, false)]  // 15% drawdown - warning
    [InlineData(-19, true, false)]  // 19% drawdown - warning
    [InlineData(-20, true, true)]   // 20% drawdown - HALT
    [InlineData(-25, true, true)]   // 25% drawdown - HALT
    public void ValidateDrawdownThresholds_ShouldTriggerCorrectActions(
        decimal drawdownPercent, bool shouldWarn, bool shouldHalt)
    {
        // Act
        var warnResult = ShouldWarnDrawdown(drawdownPercent);
        var haltResult = ShouldHaltForDrawdown(drawdownPercent);

        // Assert
        warnResult.Should().Be(shouldWarn);
        haltResult.Should().Be(shouldHalt);
    }

    #endregion

    #region Order Validation (REQ-2.5.6)

    [Fact]
    public void ValidateOrder_DuplicateOrder_ShouldReject()
    {
        // Arrange
        var pendingOrders = new[] { "AAPL", "MSFT", "GOOGL" };
        var newOrderSymbol = "AAPL";

        // Act
        var isDuplicate = IsDuplicateOrder(pendingOrders, newOrderSymbol);

        // Assert
        isDuplicate.Should().BeTrue();
    }

    [Fact]
    public void ValidateOrder_NewSymbol_ShouldAllow()
    {
        // Arrange
        var pendingOrders = new[] { "AAPL", "MSFT", "GOOGL" };
        var newOrderSymbol = "TSLA";

        // Act
        var isDuplicate = IsDuplicateOrder(pendingOrders, newOrderSymbol);

        // Assert
        isDuplicate.Should().BeFalse();
    }

    [Theory]
    [InlineData(100, 50, 150, true)]   // Price within range
    [InlineData(100, 50, 150, true)]   // At lower bound
    [InlineData(100, 100, 150, true)]  // At lower bound
    [InlineData(100, 50, 100, true)]   // At upper bound
    [InlineData(100, 101, 150, false)] // Below lower bound
    [InlineData(100, 50, 99, false)]   // Above upper bound
    public void ValidateOrder_PriceRange_ShouldEnforce(
        decimal orderPrice, decimal minPrice, decimal maxPrice, bool isValid)
    {
        // Act
        var result = IsPriceInRange(orderPrice, minPrice, maxPrice);

        // Assert
        result.Should().Be(isValid);
    }

    [Fact]
    public void ValidateOrder_InsufficientBuyingPower_ShouldReject()
    {
        // Arrange
        var buyingPower = 1000m;
        var orderValue = 1500m;

        // Act
        var hasSufficientFunds = HasSufficientBuyingPower(buyingPower, orderValue);

        // Assert
        hasSufficientFunds.Should().BeFalse();
    }

    [Fact]
    public void ValidateOrder_SufficientBuyingPower_ShouldAllow()
    {
        // Arrange
        var buyingPower = 2000m;
        var orderValue = 1500m;

        // Act
        var hasSufficientFunds = HasSufficientBuyingPower(buyingPower, orderValue);

        // Assert
        hasSufficientFunds.Should().BeTrue();
    }

    #endregion

    #region Cash Reserve (REQ-2.5.4 Optional)

    [Theory]
    [InlineData(10000, 1000, 5, true)]  // Has 10% cash, need 5% - OK
    [InlineData(10000, 500, 5, true)]   // Has exactly 5% cash - OK
    [InlineData(10000, 400, 5, false)]  // Has 4% cash, need 5% - BLOCKED
    public void ValidateCashReserve_ShouldMaintainMinimum(
        decimal portfolioValue, decimal cashBalance, decimal minCashPercent, bool meetsReserve)
    {
        // Act
        var result = MeetsCashReserveRequirement(portfolioValue, cashBalance, minCashPercent);

        // Assert
        result.Should().Be(meetsReserve);
    }

    #endregion

    #region Comprehensive Trade Validation

    [Fact]
    public void ValidateTrade_AllRulesPassing_ShouldApprove()
    {
        // Arrange
        var context = new TradeValidationContext
        {
            PortfolioValue = 10000m,
            OrderValue = 500m,
            MaxPositionPercent = 10m,
            CurrentPositionCount = 5,
            MaxPositions = 10,
            DayStartValue = 10000m,
            CurrentValue = 9800m,
            MaxDailyLossPercent = 5m,
            TradesToday = 10,
            MaxDailyTrades = 20,
            DrawdownPercent = -10m,
            BuyingPower = 1000m,
            PendingOrderSymbols = new[] { "MSFT" },
            NewOrderSymbol = "AAPL"
        };

        // Act
        var result = ValidateTradeComprehensive(context);

        // Assert
        result.IsApproved.Should().BeTrue();
        result.RejectionReasons.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTrade_MultipleViolations_ShouldRejectWithAllReasons()
    {
        // Arrange
        var context = new TradeValidationContext
        {
            PortfolioValue = 10000m,
            OrderValue = 2000m,          // Exceeds 10% position limit
            MaxPositionPercent = 10m,
            CurrentPositionCount = 10,    // At max positions
            MaxPositions = 10,
            DayStartValue = 10000m,
            CurrentValue = 9400m,         // 6% daily loss, exceeds limit
            MaxDailyLossPercent = 5m,
            TradesToday = 20,             // At max daily trades
            MaxDailyTrades = 20,
            DrawdownPercent = -22m,       // Exceeds 20% halt threshold
            BuyingPower = 1000m,          // Insufficient for $2000 order
            PendingOrderSymbols = new[] { "AAPL" },
            NewOrderSymbol = "AAPL"       // Duplicate order
        };

        // Act
        var result = ValidateTradeComprehensive(context);

        // Assert
        result.IsApproved.Should().BeFalse();
        result.RejectionReasons.Should().HaveCountGreaterThan(3);
    }

    #endregion

    #region Helper Methods

    private bool ValidatePositionSize(decimal portfolioValue, decimal positionValue, decimal maxPercent)
    {
        var positionPercent = (positionValue / portfolioValue) * 100;
        return positionPercent <= maxPercent;
    }

    private bool CanAddNewPosition(int currentPositions, int maxPositions)
    {
        return currentPositions < maxPositions;
    }

    private bool CanContinueTradingToday(decimal dayStartValue, decimal currentValue, decimal maxLossPercent)
    {
        var lossPercent = ((currentValue - dayStartValue) / dayStartValue) * 100;
        return lossPercent > -maxLossPercent;
    }

    private bool ShouldWarnDailyLoss(decimal dayStartValue, decimal currentValue, decimal maxLossPercent, decimal warningThreshold)
    {
        var lossPercent = Math.Abs(((currentValue - dayStartValue) / dayStartValue) * 100);
        var warningLevel = maxLossPercent * (warningThreshold / 100);
        return lossPercent >= warningLevel;
    }

    private bool CanExecuteMoreTrades(int tradesToday, int maxTrades)
    {
        return tradesToday < maxTrades;
    }

    private bool ShouldBlockForPDT(int dayTradesInLast5Days, decimal accountEquity)
    {
        const decimal PDT_THRESHOLD = 25000m;
        const int PDT_LIMIT = 4;

        if (accountEquity >= PDT_THRESHOLD)
            return false; // No PDT restrictions for accounts >= $25k

        return dayTradesInLast5Days >= PDT_LIMIT - 1; // Block if at or over 3
    }

    private bool ShouldWarnPDT(int dayTradesInLast5Days, decimal accountEquity)
    {
        const decimal PDT_THRESHOLD = 25000m;
        const int PDT_WARNING = 3;

        if (accountEquity >= PDT_THRESHOLD)
            return false;

        return dayTradesInLast5Days >= PDT_WARNING;
    }

    private bool ShouldWarnDrawdown(decimal drawdownPercent)
    {
        const decimal WARNING_THRESHOLD = -15m;
        return drawdownPercent <= WARNING_THRESHOLD;
    }

    private bool ShouldHaltForDrawdown(decimal drawdownPercent)
    {
        const decimal HALT_THRESHOLD = -20m;
        return drawdownPercent <= HALT_THRESHOLD;
    }

    private bool IsDuplicateOrder(string[] pendingOrders, string newSymbol)
    {
        return pendingOrders.Contains(newSymbol);
    }

    private bool IsPriceInRange(decimal price, decimal minPrice, decimal maxPrice)
    {
        return price >= minPrice && price <= maxPrice;
    }

    private bool HasSufficientBuyingPower(decimal buyingPower, decimal orderValue)
    {
        return buyingPower >= orderValue;
    }

    private bool MeetsCashReserveRequirement(decimal portfolioValue, decimal cashBalance, decimal minCashPercent)
    {
        var cashPercent = (cashBalance / portfolioValue) * 100;
        return cashPercent >= minCashPercent;
    }

    private TradeValidationResult ValidateTradeComprehensive(TradeValidationContext ctx)
    {
        var reasons = new List<string>();

        if (!ValidatePositionSize(ctx.PortfolioValue, ctx.OrderValue, ctx.MaxPositionPercent))
            reasons.Add("Position size exceeds maximum allowed percentage");

        if (!CanAddNewPosition(ctx.CurrentPositionCount, ctx.MaxPositions))
            reasons.Add("Maximum concurrent positions reached");

        if (!CanContinueTradingToday(ctx.DayStartValue, ctx.CurrentValue, ctx.MaxDailyLossPercent))
            reasons.Add("Daily loss limit exceeded");

        if (!CanExecuteMoreTrades(ctx.TradesToday, ctx.MaxDailyTrades))
            reasons.Add("Maximum daily trades reached");

        if (ShouldHaltForDrawdown(ctx.DrawdownPercent))
            reasons.Add("Trading halted due to drawdown threshold");

        if (!HasSufficientBuyingPower(ctx.BuyingPower, ctx.OrderValue))
            reasons.Add("Insufficient buying power");

        if (IsDuplicateOrder(ctx.PendingOrderSymbols, ctx.NewOrderSymbol))
            reasons.Add("Duplicate order for same symbol");

        return new TradeValidationResult
        {
            IsApproved = reasons.Count == 0,
            RejectionReasons = reasons
        };
    }

    #endregion

    #region Test Helper Classes

    private class TradeValidationContext
    {
        public decimal PortfolioValue { get; set; }
        public decimal OrderValue { get; set; }
        public decimal MaxPositionPercent { get; set; }
        public int CurrentPositionCount { get; set; }
        public int MaxPositions { get; set; }
        public decimal DayStartValue { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal MaxDailyLossPercent { get; set; }
        public int TradesToday { get; set; }
        public int MaxDailyTrades { get; set; }
        public decimal DrawdownPercent { get; set; }
        public decimal BuyingPower { get; set; }
        public string[] PendingOrderSymbols { get; set; } = Array.Empty<string>();
        public string NewOrderSymbol { get; set; } = string.Empty;
    }

    private class TradeValidationResult
    {
        public bool IsApproved { get; set; }
        public List<string> RejectionReasons { get; set; } = new();
    }

    #endregion
}
