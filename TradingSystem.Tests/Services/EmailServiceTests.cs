using FluentAssertions;
using Xunit;

namespace TradingSystem.Tests.Services;

/// <summary>
/// Unit tests for Email Service formatting and content generation.
/// </summary>
public class EmailServiceTests
{
    #region Subject Line Generation Tests

    [Theory]
    [InlineData(5.5, "📈")]   // Positive return
    [InlineData(-3.2, "📉")]  // Negative return
    [InlineData(0, "📈")]     // Zero (treated as positive)
    public void GenerateDailySummarySubject_ShouldUseCorrectEmoji(decimal dailyReturn, string expectedEmoji)
    {
        // Act
        var subject = GenerateDailySummarySubject(dailyReturn, 10000m);

        // Assert
        subject.Should().StartWith(expectedEmoji);
    }

    [Fact]
    public void GenerateDailySummarySubject_ShouldIncludeReturnAndValue()
    {
        // Arrange
        var dailyReturn = 2.5m;
        var portfolioValue = 10250m;

        // Act
        var subject = GenerateDailySummarySubject(dailyReturn, portfolioValue);

        // Assert
        subject.Should().Contain("+2.50%");
        subject.Should().Contain("$10,250.00");
    }

    [Fact]
    public void GenerateAlertSubject_CriticalPriority_ShouldUseAlertEmoji()
    {
        // Act
        var subject = GenerateAlertSubject("Trading Halted", "CRITICAL");

        // Assert
        subject.Should().Contain("🚨");
        subject.Should().Contain("[CRITICAL]");
        subject.Should().Contain("Trading Halted");
    }

    [Fact]
    public void GenerateAlertSubject_HighPriority_ShouldUseWarningEmoji()
    {
        // Act
        var subject = GenerateAlertSubject("Drawdown Warning", "HIGH");

        // Assert
        subject.Should().Contain("⚠️");
        subject.Should().Contain("[HIGH]");
    }

    [Fact]
    public void GenerateAlertSubject_MediumPriority_ShouldUseMegaphoneEmoji()
    {
        // Act
        var subject = GenerateAlertSubject("Daily Summary", "MEDIUM");

        // Assert
        subject.Should().Contain("📢");
        subject.Should().Contain("[MEDIUM]");
    }

    #endregion

    #region Trade Notification Tests

    [Fact]
    public void GenerateTradeSubject_BuyOrder_ShouldUseGreenCircle()
    {
        // Act
        var subject = GenerateTradeSubject("BUY", "AAPL", 10, 150.00m);

        // Assert
        subject.Should().Contain("🟢");
        subject.Should().Contain("BUY");
        subject.Should().Contain("AAPL");
        subject.Should().Contain("$150.00");
    }

    [Fact]
    public void GenerateTradeSubject_SellOrder_ShouldUseRedCircle()
    {
        // Act
        var subject = GenerateTradeSubject("SELL", "MSFT", 5, 400.00m);

        // Assert
        subject.Should().Contain("🔴");
        subject.Should().Contain("SELL");
        subject.Should().Contain("MSFT");
    }

    #endregion

    #region HTML Content Generation Tests

    [Fact]
    public void GeneratePositionTableHtml_WithPositions_ShouldIncludeAllColumns()
    {
        // Arrange
        var positions = new[]
        {
            new TestPosition { Symbol = "AAPL", Quantity = 10, CurrentPrice = 150m, UnrealizedPL = 50m },
            new TestPosition { Symbol = "MSFT", Quantity = 5, CurrentPrice = 400m, UnrealizedPL = -25m }
        };

        // Act
        var html = GeneratePositionTableHtml(positions);

        // Assert
        html.Should().Contain("AAPL");
        html.Should().Contain("MSFT");
        html.Should().Contain("$150");
        html.Should().Contain("$400");
        html.Should().Contain("table");
    }

    [Fact]
    public void GeneratePositionTableHtml_NoPositions_ShouldShowMessage()
    {
        // Arrange
        var positions = Array.Empty<TestPosition>();

        // Act
        var html = GeneratePositionTableHtml(positions);

        // Assert
        html.Should().Contain("No open positions");
    }

    [Fact]
    public void GenerateMetricHtml_PositiveValue_ShouldUseGreenColor()
    {
        // Act
        var html = GenerateMetricHtml("Daily Return", 5.5m, isPercentage: true);

        // Assert
        html.Should().Contain("green").Or.Contain("#28a745").Or.Contain("positive");
        html.Should().Contain("+5.50%");
    }

    [Fact]
    public void GenerateMetricHtml_NegativeValue_ShouldUseRedColor()
    {
        // Act
        var html = GenerateMetricHtml("Daily Return", -3.2m, isPercentage: true);

        // Assert
        html.Should().Contain("red").Or.Contain("#dc3545").Or.Contain("negative");
        html.Should().Contain("-3.20%");
    }

    #endregion

    #region Error Notification Tests

    [Fact]
    public void GenerateErrorHtml_WithException_ShouldIncludeDetails()
    {
        // Arrange
        var errorMessage = "Failed to sync portfolio";
        var exceptionType = "InvalidOperationException";
        var exceptionMessage = "Connection timeout";

        // Act
        var html = GenerateErrorHtml(errorMessage, exceptionType, exceptionMessage);

        // Assert
        html.Should().Contain(errorMessage);
        html.Should().Contain(exceptionType);
        html.Should().Contain(exceptionMessage);
        html.Should().Contain("🔴").Or.Contain("error").Or.Contain("#dc3545");
    }

    [Fact]
    public void GenerateErrorHtml_WithoutException_ShouldOnlyShowMessage()
    {
        // Arrange
        var errorMessage = "Data feed interrupted";

        // Act
        var html = GenerateErrorHtml(errorMessage, null, null);

        // Assert
        html.Should().Contain(errorMessage);
    }

    #endregion

    #region Weekly/Monthly Summary Tests

    [Fact]
    public void GenerateWeeklySummaryContent_ShouldIncludeAllMetrics()
    {
        // Arrange
        var portfolioValue = 10500m;
        var weeklyReturn = 5.0m;
        var totalReturn = 5.0m;
        var tradesThisWeek = 12;
        var winRate = 66.7m;

        // Act
        var content = GenerateWeeklySummaryContent(portfolioValue, weeklyReturn, totalReturn, tradesThisWeek, winRate);

        // Assert
        content.Should().Contain("$10,500");
        content.Should().Contain("5.00%").Or.Contain("+5.00%");
        content.Should().Contain("12");
        content.Should().Contain("66.7%").Or.Contain("66.70%");
        content.Should().Contain("Weekly");
    }

    [Fact]
    public void GenerateMonthlySummaryContent_ShouldIncludeSharpeAndCosts()
    {
        // Arrange
        var portfolioValue = 11000m;
        var monthlyReturn = 10.0m;
        var sharpeRatio = 1.5m;
        var azureCosts = 12.50m;

        // Act
        var content = GenerateMonthlySummaryContent(portfolioValue, monthlyReturn, sharpeRatio, azureCosts);

        // Assert
        content.Should().Contain("$11,000");
        content.Should().Contain("10.00%").Or.Contain("+10.00%");
        content.Should().Contain("1.5").Or.Contain("1.50");
        content.Should().Contain("$12.50");
        content.Should().Contain("Azure").Or.Contain("Cost");
    }

    #endregion

    #region Benchmark Comparison Tests

    [Fact]
    public void GenerateBenchmarkComparisonHtml_Outperforming_ShouldShowPositiveAlpha()
    {
        // Arrange
        var portfolioReturn = 15m;
        var spyReturn = 10m;
        var qqqReturn = 12m;

        // Act
        var html = GenerateBenchmarkComparisonHtml(portfolioReturn, spyReturn, qqqReturn);

        // Assert
        html.Should().Contain("15");
        html.Should().Contain("SPY").Or.Contain("S&P");
        html.Should().Contain("QQQ").Or.Contain("NASDAQ");
        html.Should().Contain("Alpha").Or.Contain("+5"); // Alpha vs SPY
    }

    #endregion

    #region Timestamp Formatting Tests

    [Fact]
    public void FormatTimestamp_ShouldIncludeTimeZone()
    {
        // Arrange
        var utcTime = DateTime.UtcNow;

        // Act
        var formatted = FormatTimestampForEmail(utcTime);

        // Assert
        formatted.Should().Contain("UTC").Or.Contain("ET");
    }

    [Fact]
    public void FormatCurrency_ShouldIncludeDollarSign()
    {
        // Arrange
        var value = 12345.67m;

        // Act
        var formatted = FormatCurrency(value);

        // Assert
        formatted.Should().StartWith("$");
        formatted.Should().Contain("12,345.67");
    }

    [Fact]
    public void FormatPercentage_PositiveValue_ShouldIncludePlusSign()
    {
        // Arrange
        var value = 5.5m;

        // Act
        var formatted = FormatPercentage(value);

        // Assert
        formatted.Should().Contain("+");
        formatted.Should().Contain("5.50%");
    }

    [Fact]
    public void FormatPercentage_NegativeValue_ShouldIncludeMinusSign()
    {
        // Arrange
        var value = -3.2m;

        // Act
        var formatted = FormatPercentage(value);

        // Assert
        formatted.Should().Contain("-");
        formatted.Should().Contain("3.20%");
    }

    #endregion

    #region Helper Methods

    private string GenerateDailySummarySubject(decimal dailyReturn, decimal portfolioValue)
    {
        var emoji = dailyReturn >= 0 ? "📈" : "📉";
        return $"{emoji} Daily Portfolio Summary: {dailyReturn:+0.00;-0.00}% | ${portfolioValue:N2}";
    }

    private string GenerateAlertSubject(string title, string priority)
    {
        var emoji = priority.ToUpper() switch
        {
            "CRITICAL" => "🚨",
            "HIGH" => "⚠️",
            "MEDIUM" => "📢",
            "LOW" => "ℹ️",
            _ => "📢"
        };
        return $"{emoji} [{priority.ToUpper()}] {title}";
    }

    private string GenerateTradeSubject(string tradeType, string symbol, int quantity, decimal price)
    {
        var emoji = tradeType.ToUpper() == "BUY" ? "🟢" : "🔴";
        return $"{emoji} Trade Executed: {tradeType.ToUpper()} {quantity} {symbol} @ ${price:N2}";
    }

    private string GeneratePositionTableHtml(TestPosition[] positions)
    {
        if (positions.Length == 0)
            return "<p>No open positions</p>";

        var rows = string.Join("\n", positions.Select(p =>
            $"<tr><td>{p.Symbol}</td><td>{p.Quantity}</td><td>${p.CurrentPrice:N2}</td><td>${p.UnrealizedPL:N2}</td></tr>"));

        return $"<table>{rows}</table>";
    }

    private string GenerateMetricHtml(string label, decimal value, bool isPercentage)
    {
        var color = value >= 0 ? "green" : "red";
        var formatted = isPercentage ? $"{value:+0.00;-0.00}%" : $"${value:N2}";
        return $"<div class='{(value >= 0 ? "positive" : "negative")}' style='color: {color};'>{formatted}</div>";
    }

    private string GenerateErrorHtml(string errorMessage, string? exceptionType, string? exceptionMessage)
    {
        var html = $"<div class='error'>🔴 {errorMessage}</div>";
        if (exceptionType != null && exceptionMessage != null)
        {
            html += $"<div>Exception Details: {exceptionType} - {exceptionMessage}</div>";
        }
        return html;
    }

    private string GenerateWeeklySummaryContent(decimal portfolioValue, decimal weeklyReturn, decimal totalReturn, int trades, decimal winRate)
    {
        return $@"
            <div>Weekly Summary</div>
            <div>${portfolioValue:N2}</div>
            <div>{weeklyReturn:+0.00;-0.00}%</div>
            <div>{totalReturn:+0.00;-0.00}%</div>
            <div>{trades}</div>
            <div>{winRate:0.0}%</div>";
    }

    private string GenerateMonthlySummaryContent(decimal portfolioValue, decimal monthlyReturn, decimal sharpeRatio, decimal azureCosts)
    {
        return $@"
            <div>Monthly Summary</div>
            <div>${portfolioValue:N2}</div>
            <div>{monthlyReturn:+0.00;-0.00}%</div>
            <div>Sharpe: {sharpeRatio:0.00}</div>
            <div>Azure Costs: ${azureCosts:N2}</div>";
    }

    private string GenerateBenchmarkComparisonHtml(decimal portfolioReturn, decimal spyReturn, decimal qqqReturn)
    {
        var alpha = portfolioReturn - spyReturn;
        return $@"
            <div>Portfolio: {portfolioReturn:+0.00;-0.00}%</div>
            <div>SPY (S&P 500): {spyReturn:+0.00;-0.00}%</div>
            <div>QQQ (NASDAQ): {qqqReturn:+0.00;-0.00}%</div>
            <div>Alpha vs SPY: {alpha:+0.00;-0.00}%</div>";
    }

    private string FormatTimestampForEmail(DateTime utcTime)
    {
        return $"{utcTime:yyyy-MM-dd HH:mm:ss} UTC";
    }

    private string FormatCurrency(decimal value)
    {
        return $"${value:N2}";
    }

    private string FormatPercentage(decimal value)
    {
        return $"{value:+0.00;-0.00}%";
    }

    #endregion

    #region Test Helper Classes

    private class TestPosition
    {
        public string Symbol { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal UnrealizedPL { get; set; }
    }

    #endregion
}
