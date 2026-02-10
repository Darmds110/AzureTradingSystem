using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TradingSystem.Functions.Models;

namespace TradingSystem.Functions.Data;

public class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Portfolio> Portfolios { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<StrategyConfiguration> StrategyConfigurations { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<TradeHistory> TradeHistory { get; set; }
    public DbSet<MarketData> MarketData { get; set; }
    public DbSet<PerformanceMetrics> PerformanceMetrics { get; set; }
    public DbSet<AuditLog> AuditLog { get; set; }
    public DbSet<NotificationHistory> NotificationHistory { get; set; }

    // Alias for code that uses AuditLogs (plural)
    public DbSet<AuditLog> AuditLogs => AuditLog;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Portfolio configuration
        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.HasIndex(e => e.PortfolioName).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("GETUTCDATE()");
        });

        // Position configuration
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasIndex(e => new { e.PortfolioId, e.Symbol }).IsUnique();
            entity.Property(e => e.OpenedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("GETUTCDATE()");
        });

        // MarketData configuration
        modelBuilder.Entity<MarketData>(entity =>
        {
            entity.HasIndex(e => new { e.Symbol, e.DataTimestamp }).IsUnique();
            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => e.DataTimestamp);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // StrategyConfiguration configuration
        modelBuilder.Entity<StrategyConfiguration>(entity =>
        {
            entity.HasIndex(e => new { e.StrategyName, e.Version }).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.LastModified).HasDefaultValueSql("GETUTCDATE()");
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.AlpacaOrderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SubmittedAt);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // TradeHistory configuration
        modelBuilder.Entity<TradeHistory>(entity =>
        {
            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => e.ExitDate);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // PerformanceMetrics configuration
        modelBuilder.Entity<PerformanceMetrics>(entity =>
        {
            entity.HasIndex(e => new { e.PortfolioId, e.MetricDate, e.PeriodType }).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
        });

        // NotificationHistory configuration
        modelBuilder.Entity<NotificationHistory>(entity =>
        {
            entity.HasIndex(e => e.SentAt);
            entity.HasIndex(e => e.NotificationType);
            entity.Property(e => e.SentAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }
}