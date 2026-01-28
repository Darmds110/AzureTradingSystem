using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingSystem.Functions.Config;

public class StorageConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string MarketScheduleTableName { get; set; } = "MarketSchedule";
    public string LatestQuotesTableName { get; set; } = "LatestQuotes";
}