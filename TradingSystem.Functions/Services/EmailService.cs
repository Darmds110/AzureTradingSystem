using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using TradingSystem.Functions.Config;
using TradingSystem.Functions.Data;
using TradingSystem.Functions.Models;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services;

public class EmailService : IEmailService
{
    private readonly EmailConfig _config;
    private readonly TradingDbContext _dbContext;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        EmailConfig config,
        TradingDbContext dbContext,
        ILogger<EmailService> logger)
    {
        _config = config;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SendNotificationAsync(string subject, string body, string priority = "MEDIUM")
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Trading System", _config.FromAddress));
            message.To.Add(new MailboxAddress("", _config.ToAddress));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_config.SmtpServer, _config.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config.SmtpUsername, _config.SmtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            // Log notification
            await LogNotification(subject, body, priority, "EMAIL", true, null);

            _logger.LogInformation("Email sent: {Subject}", subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email: {Subject}", subject);
            await LogNotification(subject, body, priority, "EMAIL", false, ex.Message);
            throw;
        }
    }

    public async Task SendTradeNotificationAsync(string symbol, string action, int quantity, decimal price)
    {
        var subject = $"Trade Executed: {action} {quantity} {symbol}";
        var body = $@"
Trade Notification
==================

Symbol: {symbol}
Action: {action}
Quantity: {quantity}
Price: ${price:F2}
Total Value: ${quantity * price:F2}
Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

This is an automated notification from your trading system.
";

        await SendNotificationAsync(subject, body, "HIGH");
    }

    public async Task SendErrorNotificationAsync(string errorMessage, Exception? exception = null)
    {
        var subject = "Trading System Error Alert";
        var body = $@"
Error Alert
===========

Error: {errorMessage}

{(exception != null ? $@"
Exception Details:
{exception.GetType().Name}: {exception.Message}

Stack Trace:
{exception.StackTrace}
" : "")}

Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

Please check Application Insights for more details.
";

        await SendNotificationAsync(subject, body, "CRITICAL");
    }

    public async Task SendDailySummaryAsync(decimal portfolioValue, decimal dayChange, int tradesExecuted)
    {
        var subject = $"Daily Summary - Portfolio: ${portfolioValue:F2}";
        var changePercent = dayChange / portfolioValue * 100;
        var changeSymbol = dayChange >= 0 ? "+" : "";

        var body = $@"
Daily Portfolio Summary
=======================

Portfolio Value: ${portfolioValue:F2}
Day's Change: {changeSymbol}${dayChange:F2} ({changeSymbol}{changePercent:F2}%)
Trades Executed: {tradesExecuted}
Date: {DateTime.Now:yyyy-MM-dd}

Keep up the great work!

---
This is an automated summary from your trading system.
";

        await SendNotificationAsync(subject, body, "MEDIUM");
    }

    private async Task LogNotification(
        string subject,
        string body,
        string priority,
        string type,
        bool wasSuccessful,
        string? errorMessage)
    {
        try
        {
            var notification = new NotificationHistory
            {
                SentAt = DateTime.UtcNow,
                NotificationType = type,
                Priority = priority,
                Subject = subject,
                Body = body,
                Recipient = _config.ToAddress,
                WasSuccessful = wasSuccessful,
                ErrorMessage = errorMessage
            };

            _dbContext.NotificationHistory.Add(notification);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging notification to database");
        }
    }
}