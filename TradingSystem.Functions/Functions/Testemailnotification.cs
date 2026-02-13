using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Functions;

/// <summary>
/// HTTP-triggered function to test email notification configuration.
/// Use this to verify SMTP settings are working before Phase 4.
/// </summary>
public class TestEmailNotification
{
    private readonly ILogger<TestEmailNotification> _logger;
    private readonly IEmailService _emailService;

    public TestEmailNotification(
        ILogger<TestEmailNotification> logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    /// <summary>
    /// Test endpoint to verify email configuration.
    /// POST /api/TestEmailNotification
    /// </summary>
    [Function("TestEmailNotification")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("TestEmailNotification triggered at: {Time}", DateTime.UtcNow);

        var response = req.CreateResponse();

        try
        {
            // Build test email content
            var subject = "🧪 Trading System Email Test";
            var body = BuildTestEmailBody();

            _logger.LogInformation("Attempting to send test email...");

            // Send the test email
            await _emailService.SendEmailAsync(subject, body, isHtml: true);

            _logger.LogInformation("✅ Test email sent successfully!");

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = "Test email sent successfully!",
                timestamp = DateTime.UtcNow,
                details = new
                {
                    subject = subject,
                    note = "Check your inbox (and spam folder) for the test email."
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send test email: {Message}", ex.Message);

            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Failed to send test email",
                error = ex.Message,
                innerError = ex.InnerException?.Message,
                timestamp = DateTime.UtcNow,
                troubleshooting = new[]
                {
                    "1. Verify EmailSmtpServer secret is 'smtp-mail.outlook.com'",
                    "2. Verify EmailSmtpPort secret is '587'",
                    "3. Verify EmailSmtpUsername is your full Outlook email address",
                    "4. Verify EmailSmtpPassword is a 16-character app password (no spaces)",
                    "5. Verify EmailFromAddress and EmailToAddress are set in Function App settings",
                    "6. Check that 2FA is enabled on your Microsoft account",
                    "7. Verify the app password was generated correctly at account.microsoft.com"
                }
            });
        }

        return response;
    }

    private string BuildTestEmailBody()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        var etTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("America/New_York")
        ).ToString("yyyy-MM-dd hh:mm:ss tt ET");

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .success {{ background: #d4edda; border: 1px solid #c3e6cb; color: #155724; padding: 15px; border-radius: 4px; margin: 15px 0; }}
        .info {{ background: #d1ecf1; border: 1px solid #bee5eb; color: #0c5460; padding: 15px; border-radius: 4px; margin: 15px 0; }}
        .footer {{ background: #f1f1f1; padding: 15px; text-align: center; font-size: 12px; color: #666; border-radius: 0 0 8px 8px; }}
        table {{ width: 100%; border-collapse: collapse; margin: 15px 0; }}
        th, td {{ padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }}
        th {{ background: #f5f5f5; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0;'>🧪 Email Configuration Test</h1>
            <p style='margin: 5px 0 0 0;'>Azure Trading System</p>
        </div>
        
        <div class='content'>
            <div class='success'>
                <strong>✅ SUCCESS!</strong> Your email configuration is working correctly.
            </div>
            
            <h2>Test Details</h2>
            <table>
                <tr>
                    <th>Property</th>
                    <th>Value</th>
                </tr>
                <tr>
                    <td>Timestamp (UTC)</td>
                    <td>{timestamp}</td>
                </tr>
                <tr>
                    <td>Timestamp (ET)</td>
                    <td>{etTime}</td>
                </tr>
                <tr>
                    <td>SMTP Server</td>
                    <td>smtp-mail.outlook.com:587</td>
                </tr>
                <tr>
                    <td>Encryption</td>
                    <td>TLS/STARTTLS</td>
                </tr>
            </table>
            
            <div class='info'>
                <strong>ℹ️ What This Means:</strong>
                <ul style='margin: 10px 0 0 0;'>
                    <li>SMTP connection successful</li>
                    <li>Authentication working</li>
                    <li>Email delivery pipeline operational</li>
                    <li>Ready for Phase 4 trade notifications</li>
                </ul>
            </div>
            
            <h2>Email Types You'll Receive</h2>
            <ul>
                <li><strong>Trade Notifications:</strong> Buy/sell order confirmations</li>
                <li><strong>Daily Summaries:</strong> End-of-day portfolio reports</li>
                <li><strong>Risk Alerts:</strong> Drawdown warnings at 15% and 20%</li>
                <li><strong>System Alerts:</strong> Error notifications and health checks</li>
            </ul>
        </div>
        
        <div class='footer'>
            <p>Azure Autonomous Trading System | Pre-Phase 4 Verification</p>
            <p>This is an automated test message.</p>
        </div>
    </div>
</body>
</html>";
    }
}