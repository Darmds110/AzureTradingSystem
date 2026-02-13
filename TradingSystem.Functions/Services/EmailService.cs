using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services;

/// <summary>
/// Email service implementation using MailKit and Outlook SMTP.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromAddress;
    private readonly string _toAddress;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;

        // Read configuration from environment variables
        _smtpServer = Environment.GetEnvironmentVariable("EmailSmtpServer")
            ?? throw new InvalidOperationException("EmailSmtpServer environment variable not set");

        var portString = Environment.GetEnvironmentVariable("EmailSmtpPort") ?? "587";
        _smtpPort = int.Parse(portString);

        _smtpUsername = Environment.GetEnvironmentVariable("EmailSmtpUsername")
            ?? throw new InvalidOperationException("EmailSmtpUsername environment variable not set");

        _smtpPassword = Environment.GetEnvironmentVariable("EmailSmtpPassword")
            ?? throw new InvalidOperationException("EmailSmtpPassword environment variable not set");

        _fromAddress = Environment.GetEnvironmentVariable("EmailFromAddress")
            ?? throw new InvalidOperationException("EmailFromAddress environment variable not set");

        _toAddress = Environment.GetEnvironmentVariable("EmailToAddress")
            ?? throw new InvalidOperationException("EmailToAddress environment variable not set");

        _logger.LogInformation("EmailService initialized. From: {From}, To: {To}, Server: {Server}:{Port}",
            _fromAddress, _toAddress, _smtpServer, _smtpPort);
    }

    #region Core Email Methods

    public async Task SendEmailAsync(string subject, string body, bool isHtml = false)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Trading System", _fromAddress));
        message.To.Add(new MailboxAddress("", _toAddress));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        if (isHtml)
        {
            bodyBuilder.HtmlBody = body;
        }
        else
        {
            bodyBuilder.TextBody = body;
        }
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            _logger.LogInformation("Connecting to SMTP server {Server}:{Port}...", _smtpServer, _smtpPort);

            await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);

            _logger.LogInformation("Authenticating with SMTP server...");
            await client.AuthenticateAsync(_smtpUsername, _smtpPassword);

            _logger.LogInformation("Sending email: {Subject}", subject);
            await client.SendAsync(message);

            _logger.LogInformation("Email sent successfully to {To}", _toAddress);
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }

    public async Task SendSummaryEmailAsync(string subject, string htmlBody)
    {
        var wrappedBody = WrapInEmailTemplate(htmlBody);
        await SendEmailAsync(subject, wrappedBody, isHtml: true);
    }

    #endregion

    #region Alert Methods

    /// <summary>
    /// Send alert with string priority (CRITICAL, HIGH, MEDIUM, LOW)
    /// </summary>
    public async Task SendAlertAsync(string title, string message, string priority)
    {
        var emoji = priority.ToUpper() switch
        {
            "CRITICAL" => "🚨",
            "HIGH" => "⚠️",
            "MEDIUM" => "📢",
            "LOW" => "ℹ️",
            _ => "📢"
        };

        var color = priority.ToUpper() switch
        {
            "CRITICAL" => "#dc3545",
            "HIGH" => "#fd7e14",
            "MEDIUM" => "#ffc107",
            "LOW" => "#17a2b8",
            _ => "#6c757d"
        };

        var subject = $"{emoji} [{priority.ToUpper()}] {title}";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .alert-header {{ background: {color}; color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 8px 8px; }}
        .message {{ white-space: pre-wrap; background: #fff; padding: 15px; border-radius: 4px; border-left: 4px solid {color}; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #666; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='alert-header'>
            <h1 style='margin: 0;'>{emoji} {title}</h1>
            <p style='margin: 5px 0 0 0;'>Priority: {priority.ToUpper()}</p>
        </div>
        
        <div class='content'>
            <div class='message'>{message}</div>
            <p class='footer'>Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC<br/>Azure Autonomous Trading System</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(subject, body, isHtml: true);
    }

    #endregion

    #region Error Notification Methods

    /// <summary>
    /// Send error notification with just a message
    /// </summary>
    public async Task SendErrorNotificationAsync(string errorMessage)
    {
        var subject = "🔴 Trading System Error";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #dc3545; color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 8px 8px; }}
        .error-box {{ background: #ffebee; padding: 15px; border-radius: 4px; border-left: 4px solid #dc3545; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #666; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0;'>🔴 System Error</h1>
        </div>
        
        <div class='content'>
            <div class='error-box'>
                <p>{errorMessage}</p>
            </div>
            <p class='footer'>Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC<br/>Please investigate this error.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(subject, body, isHtml: true);
    }

    /// <summary>
    /// Send error notification with message and exception
    /// </summary>
    public async Task SendErrorNotificationAsync(string errorMessage, Exception exception)
    {
        var subject = "🔴 Trading System Error";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #dc3545; color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 8px 8px; }}
        .error-box {{ background: #ffebee; padding: 15px; border-radius: 4px; border-left: 4px solid #dc3545; margin-bottom: 15px; }}
        .exception {{ background: #f5f5f5; padding: 15px; border-radius: 4px; overflow-x: auto; }}
        .exception pre {{ margin: 0; white-space: pre-wrap; word-wrap: break-word; font-size: 12px; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #666; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0;'>🔴 System Error</h1>
        </div>
        
        <div class='content'>
            <div class='error-box'>
                <p><strong>Error:</strong> {errorMessage}</p>
            </div>
            
            <h3>Exception Details</h3>
            <p><strong>Type:</strong> {exception.GetType().Name}</p>
            <p><strong>Message:</strong> {exception.Message}</p>
            
            <div class='exception'>
                <pre>{exception.StackTrace ?? "No stack trace available"}</pre>
            </div>
            
            <p class='footer'>Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC<br/>Please investigate this error promptly.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(subject, body, isHtml: true);
    }

    #endregion

    #region Summary Email Methods

    public async Task SendDailySummaryAsync(
        decimal portfolioValue,
        decimal cashBalance,
        decimal dailyReturnPercent,
        decimal totalReturnPercent,
        IEnumerable<PositionSummary>? positions = null)
    {
        var etTime = GetEasternTime();
        var emoji = dailyReturnPercent >= 0 ? "📈" : "📉";
        var subject = $"{emoji} Daily Portfolio Summary: {dailyReturnPercent:+0.00;-0.00}% | ${portfolioValue:N2}";

        var positionsHtml = BuildPositionsHtml(positions);

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .metric {{ display: inline-block; margin: 10px 20px 10px 0; }}
        .metric-value {{ font-size: 24px; font-weight: bold; }}
        .metric-label {{ font-size: 12px; color: #666; }}
        .positive {{ color: #28a745; }}
        .negative {{ color: #dc3545; }}
        .footer {{ background: #f1f1f1; padding: 15px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 8px 8px; }}
        table {{ width: 100%; border-collapse: collapse; margin: 15px 0; }}
        th, td {{ padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0;'>📊 Daily Portfolio Summary</h1>
            <p style='margin: 5px 0 0 0;'>{etTime:dddd, MMMM d, yyyy}</p>
        </div>
        
        <div class='content'>
            <div class='metric'>
                <div class='metric-value'>${portfolioValue:N2}</div>
                <div class='metric-label'>Portfolio Value</div>
            </div>
            <div class='metric'>
                <div class='metric-value'>${cashBalance:N2}</div>
                <div class='metric-label'>Cash Balance</div>
            </div>
            <div class='metric'>
                <div class='metric-value {(dailyReturnPercent >= 0 ? "positive" : "negative")}'>{dailyReturnPercent:+0.00;-0.00}%</div>
                <div class='metric-label'>Today's Return</div>
            </div>
            <div class='metric'>
                <div class='metric-value {(totalReturnPercent >= 0 ? "positive" : "negative")}'>{totalReturnPercent:+0.00;-0.00}%</div>
                <div class='metric-label'>Total Return</div>
            </div>
            
            {positionsHtml}
        </div>
        
        <div class='footer'>
            <p>Azure Autonomous Trading System</p>
            <p>Report generated at {etTime:h:mm tt} ET</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(subject, body, isHtml: true);
    }

    public async Task SendWeeklySummaryAsync(
        decimal portfolioValue,
        decimal weeklyReturnPercent,
        decimal totalReturnPercent,
        int tradesThisWeek,
        decimal winRate)
    {
        var etTime = GetEasternTime();
        var emoji = weeklyReturnPercent >= 0 ? "📈" : "📉";
        var subject = $"{emoji} Weekly Summary: {weeklyReturnPercent:+0.00;-0.00}% | ${portfolioValue:N2}";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .metric {{ display: inline-block; margin: 10px 20px 10px 0; }}
        .metric-value {{ font-size: 24px; font-weight: bold; }}
        .metric-label {{ font-size: 12px; color: #666; }}
        .positive {{ color: #28a745; }}
        .negative {{ color: #dc3545; }}
        .footer {{ background: #f1f1f1; padding: 15px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 8px 8px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0;'>📊 Weekly Portfolio Summary</h1>
            <p style='margin: 5px 0 0 0;'>Week ending {etTime:MMMM d, yyyy}</p>
        </div>
        
        <div class='content'>
            <div class='metric'>
                <div class='metric-value'>${portfolioValue:N2}</div>
                <div class='metric-label'>Portfolio Value</div>
            </div>
            <div class='metric'>
                <div class='metric-value {(weeklyReturnPercent >= 0 ? "positive" : "negative")}'>{weeklyReturnPercent:+0.00;-0.00}%</div>
                <div class='metric-label'>Weekly Return</div>
            </div>
            <div class='metric'>
                <div class='metric-value {(totalReturnPercent >= 0 ? "positive" : "negative")}'>{totalReturnPercent:+0.00;-0.00}%</div>
                <div class='metric-label'>Total Return</div>
            </div>
            <div class='metric'>
                <div class='metric-value'>{tradesThisWeek}</div>
                <div class='metric-label'>Trades This Week</div>
            </div>
            <div class='metric'>
                <div class='metric-value'>{winRate:0.0}%</div>
                <div class='metric-label'>Win Rate</div>
            </div>
        </div>
        
        <div class='footer'>
            <p>Azure Autonomous Trading System</p>
            <p>Report generated at {etTime:h:mm tt} ET</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(subject, body, isHtml: true);
    }

    public async Task SendMonthlySummaryAsync(
        decimal portfolioValue,
        decimal monthlyReturnPercent,
        decimal totalReturnPercent,
        decimal sharpeRatio,
        decimal maxDrawdownPercent,
        int tradesThisMonth,
        decimal azureCosts)
    {
        var etTime = GetEasternTime();
        var emoji = monthlyReturnPercent >= 0 ? "📈" : "📉";
        var subject = $"{emoji} Monthly Summary: {monthlyReturnPercent:+0.00;-0.00}% | ${portfolioValue:N2}";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .metric {{ display: inline-block; margin: 10px 20px 10px 0; }}
        .metric-value {{ font-size: 24px; font-weight: bold; }}
        .metric-label {{ font-size: 12px; color: #666; }}
        .positive {{ color: #28a745; }}
        .negative {{ color: #dc3545; }}
        .cost-section {{ background: #fff3cd; padding: 15px; border-radius: 8px; margin-top: 15px; }}
        .footer {{ background: #f1f1f1; padding: 15px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 8px 8px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0;'>📊 Monthly Portfolio Summary</h1>
            <p style='margin: 5px 0 0 0;'>{etTime:MMMM yyyy}</p>
        </div>
        
        <div class='content'>
            <div class='metric'>
                <div class='metric-value'>${portfolioValue:N2}</div>
                <div class='metric-label'>Portfolio Value</div>
            </div>
            <div class='metric'>
                <div class='metric-value {(monthlyReturnPercent >= 0 ? "positive" : "negative")}'>{monthlyReturnPercent:+0.00;-0.00}%</div>
                <div class='metric-label'>Monthly Return</div>
            </div>
            <div class='metric'>
                <div class='metric-value {(totalReturnPercent >= 0 ? "positive" : "negative")}'>{totalReturnPercent:+0.00;-0.00}%</div>
                <div class='metric-label'>Total Return</div>
            </div>
            <div class='metric'>
                <div class='metric-value'>{sharpeRatio:0.00}</div>
                <div class='metric-label'>Sharpe Ratio</div>
            </div>
            <div class='metric'>
                <div class='metric-value negative'>{maxDrawdownPercent:0.00}%</div>
                <div class='metric-label'>Max Drawdown</div>
            </div>
            <div class='metric'>
                <div class='metric-value'>{tradesThisMonth}</div>
                <div class='metric-label'>Trades This Month</div>
            </div>
            
            <div class='cost-section'>
                <h3 style='margin-top: 0;'>💰 Cost Analysis</h3>
                <p><strong>Azure Costs:</strong> ${azureCosts:N2}</p>
            </div>
        </div>
        
        <div class='footer'>
            <p>Azure Autonomous Trading System</p>
            <p>Report generated at {etTime:h:mm tt} ET</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(subject, body, isHtml: true);
    }

    #endregion

    #region Trade Notification

    public async Task SendTradeNotificationAsync(
        string tradeType,
        string symbol,
        int quantity,
        decimal price,
        decimal totalValue,
        string? reason = null)
    {
        var emoji = tradeType.ToUpper() == "BUY" ? "🟢" : "🔴";
        var color = tradeType.ToUpper() == "BUY" ? "#28a745" : "#dc3545";

        var subject = $"{emoji} Trade Executed: {tradeType.ToUpper()} {quantity} {symbol} @ ${price:N2}";

        var reasonHtml = !string.IsNullOrEmpty(reason)
            ? $"<div class='trade-detail'><span><strong>Reason:</strong></span><span>{reason}</span></div>"
            : "";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: {color}; color: white; padding: 20px; border-radius: 8px 8px 0 0; text-align: center; }}
        .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .trade-detail {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #eee; }}
        .footer {{ background: #f1f1f1; padding: 15px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 8px 8px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0;'>{emoji} {tradeType.ToUpper()} ORDER EXECUTED</h1>
            <h2 style='margin: 10px 0 0 0;'>{symbol}</h2>
        </div>
        
        <div class='content'>
            <div class='trade-detail'>
                <span><strong>Symbol:</strong></span>
                <span>{symbol}</span>
            </div>
            <div class='trade-detail'>
                <span><strong>Action:</strong></span>
                <span>{tradeType.ToUpper()}</span>
            </div>
            <div class='trade-detail'>
                <span><strong>Quantity:</strong></span>
                <span>{quantity} shares</span>
            </div>
            <div class='trade-detail'>
                <span><strong>Price:</strong></span>
                <span>${price:N2}</span>
            </div>
            <div class='trade-detail'>
                <span><strong>Total Value:</strong></span>
                <span>${totalValue:N2}</span>
            </div>
            {reasonHtml}
        </div>
        
        <div class='footer'>
            <p>Azure Autonomous Trading System</p>
            <p>Executed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(subject, body, isHtml: true);
    }

    #endregion

    #region Helper Methods

    private DateTime GetEasternTime()
    {
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));
        }
        catch
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("US/Eastern"));
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }
    }

    private string BuildPositionsHtml(IEnumerable<PositionSummary>? positions)
    {
        if (positions == null || !positions.Any())
        {
            return "<p><em>No open positions</em></p>";
        }

        var positionRows = string.Join("\n", positions.Select(p => $@"
            <tr>
                <td><strong>{p.Symbol}</strong></td>
                <td>{p.Quantity}</td>
                <td>${p.AverageCost:N2}</td>
                <td>${p.CurrentPrice:N2}</td>
                <td style='color: {(p.UnrealizedPL >= 0 ? "green" : "red")}'>${p.UnrealizedPL:N2} ({p.UnrealizedPLPercent:+0.00;-0.00}%)</td>
                <td>{p.DaysHeld} days</td>
            </tr>"));

        return $@"
            <h2>Current Positions</h2>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr style='background: #f5f5f5;'>
                    <th style='padding: 10px; text-align: left;'>Symbol</th>
                    <th style='padding: 10px; text-align: left;'>Qty</th>
                    <th style='padding: 10px; text-align: left;'>Avg Cost</th>
                    <th style='padding: 10px; text-align: left;'>Current</th>
                    <th style='padding: 10px; text-align: left;'>P&L</th>
                    <th style='padding: 10px; text-align: left;'>Held</th>
                </tr>
                {positionRows}
            </table>";
    }

    private string WrapInEmailTemplate(string content)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 800px; margin: 0 auto; padding: 20px; }}
        h2 {{ color: #333; border-bottom: 2px solid #667eea; padding-bottom: 10px; }}
        h3 {{ color: #555; }}
        table {{ border-collapse: collapse; margin: 10px 0; }}
        th, td {{ padding: 8px; text-align: left; }}
        hr {{ border: none; border-top: 1px solid #ddd; margin: 20px 0; }}
    </style>
</head>
<body>
    {content}
</body>
</html>";
    }

    #endregion
}