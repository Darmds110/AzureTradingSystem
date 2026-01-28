using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Config;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services;

public class MarketScheduleEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public bool IsOpen { get; set; }
    public DateTime? OpenTime { get; set; }
    public DateTime? CloseTime { get; set; }
}

public class LatestQuoteEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public double Price { get; set; }
    public DateTime QuoteTimestamp { get; set; }
}

public class TableStorageService : ITableStorageService
{
    private readonly TableClient _marketScheduleTable;
    private readonly TableClient _latestQuotesTable;
    private readonly ILogger<TableStorageService> _logger;

    public TableStorageService(StorageConfig config, ILogger<TableStorageService> logger)
    {
        _logger = logger;

        var serviceClient = new TableServiceClient(config.ConnectionString);

        _marketScheduleTable = serviceClient.GetTableClient(config.MarketScheduleTableName);
        _latestQuotesTable = serviceClient.GetTableClient(config.LatestQuotesTableName);

        // Ensure tables exist
        _marketScheduleTable.CreateIfNotExists();
        _latestQuotesTable.CreateIfNotExists();
    }

    public async Task SaveMarketSchedule(DateTime date, bool isOpen, DateTime? openTime, DateTime? closeTime)
    {
        try
        {
            var entity = new MarketScheduleEntity
            {
                PartitionKey = "NYSE",
                RowKey = date.ToString("yyyy-MM-dd"),
                IsOpen = isOpen,
                OpenTime = openTime,
                CloseTime = closeTime
            };

            await _marketScheduleTable.UpsertEntityAsync(entity);
            _logger.LogInformation("Saved market schedule for {Date}", date.ToString("yyyy-MM-dd"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving market schedule for {Date}", date.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    public async Task<(bool isOpen, DateTime? openTime, DateTime? closeTime)> GetMarketSchedule(DateTime date)
    {
        try
        {
            var response = await _marketScheduleTable.GetEntityAsync<MarketScheduleEntity>(
                "NYSE",
                date.ToString("yyyy-MM-dd")
            );

            var entity = response.Value;
            return (entity.IsOpen, entity.OpenTime, entity.CloseTime);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("No market schedule found for {Date}", date.ToString("yyyy-MM-dd"));
            return (false, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving market schedule for {Date}", date.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    public async Task SaveLatestQuote(string symbol, decimal price, DateTime timestamp)
    {
        try
        {
            var entity = new LatestQuoteEntity
            {
                PartitionKey = "QUOTES",
                RowKey = symbol,
                Price = (double)price,
                QuoteTimestamp = timestamp
            };

            await _latestQuotesTable.UpsertEntityAsync(entity);
            _logger.LogDebug("Saved latest quote for {Symbol}: {Price}", symbol, price);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving latest quote for {Symbol}", symbol);
            throw;
        }
    }

    public async Task<(decimal price, DateTime timestamp)?> GetLatestQuote(string symbol)
    {
        try
        {
            var response = await _latestQuotesTable.GetEntityAsync<LatestQuoteEntity>(
                "QUOTES",
                symbol
            );

            var entity = response.Value;
            return ((decimal)entity.Price, entity.QuoteTimestamp);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("No cached quote found for {Symbol}", symbol);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest quote for {Symbol}", symbol);
            throw;
        }
    }
}