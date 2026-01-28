using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradingSystem.Functions.Config;
using TradingSystem.Functions.Data;
using TradingSystem.Functions.Services;
using TradingSystem.Functions.Services.Interfaces;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication() // Changed from ConfigureFunctionsWorkerDefaults
    .ConfigureServices((context, services) =>
    {
        // Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Configuration
        var configuration = context.Configuration;

        // Database
        var connectionString = configuration["SqlConnectionString"];
        services.AddDbContext<TradingDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Configuration objects
        services.AddSingleton(new AlpacaConfig
        {
            ApiKey = configuration["AlpacaApiKey"] ?? "",
            SecretKey = configuration["AlpacaSecretKey"] ?? "",
            BaseUrl = configuration["AlpacaBaseUrl"] ?? "https://paper-api.alpaca.markets"
        });

        services.AddSingleton(new EmailConfig
        {
            SmtpServer = configuration["EmailSmtpServer"] ?? "",
            SmtpPort = int.Parse(configuration["EmailSmtpPort"] ?? "587"),
            SmtpUsername = configuration["EmailSmtpUsername"] ?? "",
            SmtpPassword = configuration["EmailSmtpPassword"] ?? "",
            FromAddress = configuration["EmailFromAddress"] ?? "",
            ToAddress = configuration["EmailToAddress"] ?? ""
        });

        services.AddSingleton(new StorageConfig
        {
            ConnectionString = configuration["AzureWebJobsStorage"] ?? "",
            MarketScheduleTableName = "MarketSchedule",
            LatestQuotesTableName = "LatestQuotes"
        });

        // Services
        services.AddScoped<IMarketDataService, MarketDataService>();
        services.AddScoped<ITechnicalIndicatorsService, TechnicalIndicatorsService>();
        services.AddScoped<ITableStorageService, TableStorageService>();
        services.AddScoped<IEmailService, EmailService>();
    })
    .Build();

host.Run();