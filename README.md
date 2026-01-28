\# Azure Autonomous Trading System



!\[Status](https://img.shields.io/badge/Phase-2%20Complete-brightgreen)

!\[Azure Functions](https://img.shields.io/badge/Azure-Functions-0078D4)

!\[.NET](https://img.shields.io/badge/.NET-8.0-512BD4)

!\[C#](https://img.shields.io/badge/C%23-12-239120)



An educational autonomous stock trading system built on Microsoft Azure to learn cloud architecture, algorithmic trading, and portfolio management.



\## 🎯 Project Overview



\*\*Goal:\*\* Build a fully autonomous trading system that monitors stock markets, executes trades based on configurable strategies, and manages a portfolio with minimal manual intervention.



\- \*\*Initial Capital:\*\* $1,000 (paper trading → live trading after validation)

\- \*\*Platform:\*\* Microsoft Azure

\- \*\*Broker:\*\* Alpaca Markets API

\- \*\*Monthly Budget:\*\* <$25 Azure costs

\- \*\*Current Cost:\*\* $9-14/month ✅ (44-64% under budget)



\## 📊 Current Status



\*\*✅ Phase 1 Complete\*\* - Foundation (January 26, 2026)

\- Azure infrastructure deployed (Functions, SQL, Key Vault, Storage)

\- External integrations configured (Alpaca API, Outlook SMTP)

\- Security configured (secrets in Key Vault, TDE enabled)



\*\*✅ Phase 2 Complete\*\* - Market Data Collection (January 27, 2026)

\- Real-time data collection every 5 minutes during market hours

\- 9 securities monitored with full technical analysis

\- 1,845+ historical records populated (200+ days per symbol)

\- NYSE trading calendar synchronized



\*\*🔄 Phase 3 In Progress\*\* - Portfolio Management

\- Account synchronization with Alpaca

\- Performance metrics calculation

\- Drawdown monitoring (20% halt threshold)



\## 📈 Market Data Collection



\### Securities Monitored

\- \*\*Tech Stocks:\*\* AAPL, MSFT, GOOGL, AMZN, TSLA, NVDA, META

\- \*\*Market Indices:\*\* SPY (S\&P 500), QQQ (NASDAQ-100)



\### Technical Indicators Implemented

\- \*\*RSI (14-period)\*\* - Relative Strength Index for momentum

\- \*\*SMA (20/50/200-day)\*\* - Simple Moving Averages for trend

\- \*\*EMA (12/26-period)\*\* - Exponential Moving Averages for MACD

\- \*\*MACD\*\* - Moving Average Convergence Divergence for signals



\### Latest Market Snapshot (Example)

```

Symbol  Price     RSI    Signal

AAPL    $259.90   19.83  OVERSOLD ⚠️ (potential buy)

MSFT    $482.08   52.84  NEUTRAL

GOOGL   $335.53   55.76  NEUTRAL

SPY     $696.23   48.95  NEUTRAL

```



\## 🏗️ Architecture

```

┌──────────────────────────────────────────────────┐

│         Azure Function App (Flex Consumption)    │

│  • MarketScheduleChecker    (Daily at 6 AM ET)  │

│  • MarketDataCollector       (Every 5 minutes)   │

│  • HistoricalDataBackfill   (Manual HTTP)       │

└────────────────────┬─────────────────────────────┘

&nbsp;                    │

&nbsp;                    ▼

┌──────────────────────────────────────────────────┐

│       Alpaca Markets API (Paper Trading)         │

│  • Latest Trades/Quotes (15-min delayed)        │

│  • Historical Daily Bars (OHLCV)                │

│  • Market Calendar \& Clock                      │

└────────────────────┬─────────────────────────────┘

&nbsp;                    │

&nbsp;                    ▼

┌──────────────────────────────────────────────────┐

│      Azure SQL Database (Basic Tier, 5 DTU)      │

│  • MarketData (1,845+ records with indicators)  │

│  • Portfolios \& Positions                       │

│  • Orders \& TradeHistory                        │

│  • StrategyConfigurations                       │

│  • PerformanceMetrics \& AuditLog                │

└──────────────────────────────────────────────────┘

&nbsp;        │                              │

&nbsp;        ▼                              ▼

┌─────────────────┐          ┌──────────────────────┐

│ Azure Table     │          │ Application Insights │

│ Storage         │          │ (Monitoring)         │

│ • MarketSchedule│          │ • Function telemetry │

│ • LatestQuotes  │          │ • Performance metrics│

└─────────────────┘          └──────────────────────┘

```



\## 🛠️ Technology Stack



\*\*Backend:\*\*

\- .NET 8.0 (Isolated Worker Model)

\- C# 12

\- Entity Framework Core 8.0



\*\*Azure Services:\*\*

\- Azure Functions (Flex Consumption Plan)

\- Azure SQL Database (Basic Tier, 5 DTU)

\- Azure Key Vault (Secrets Management)

\- Azure Table Storage (Caching)

\- Azure Blob Storage (Logs, backups)

\- Application Insights (Monitoring)



\*\*External APIs:\*\*

\- Alpaca Markets SDK 7.0.5 (Brokerage \& Market Data)

\- MailKit 4.3.0 (Email Notifications)



\## 💰 Cost Breakdown



| Service | Tier/Plan | Monthly Cost |

|---------|-----------|--------------|

| Azure Functions | Flex Consumption | $3-7 |

| Azure SQL Database | Basic (5 DTU) | $5 |

| Storage Account | Standard LRS | $1-2 |

| Application Insights | Free (5GB/month) | $0 |

| Azure Key Vault | Standard | $0.03 |

| Alpaca Markets API | Paper Trading (Free) | $0 |

| Outlook SMTP | Free | $0 |

| \*\*Total\*\* | | \*\*$9-14/month\*\* |



\*\*Budget Status:\*\* ✅ 44-64% under $25/month limit



\## 🚀 Project Phases



\- \[x] \*\*Phase 1:\*\* Foundation - Azure infrastructure setup

\- \[x] \*\*Phase 2:\*\* Market Data Collection - Real-time data \& indicators

\- \[ ] \*\*Phase 3:\*\* Portfolio Management - Performance tracking \& risk

\- \[ ] \*\*Phase 4:\*\* Trading Strategy Engine - Configurable strategies

\- \[ ] \*\*Phase 5:\*\* Trade Execution - Order management \& fills

\- \[ ] \*\*Phase 6:\*\* Notifications - Email alerts \& summaries

\- \[ ] \*\*Phase 7:\*\* Dashboard - Web UI for monitoring

\- \[ ] \*\*Phase 8:\*\* Backtesting - Historical strategy validation

\- \[ ] \*\*Phase 9:\*\* Paper Trading Validation - 30-day live test

\- \[ ] \*\*Phase 10:\*\* Production Readiness - Live trading deployment



\*\*Timeline:\*\* 21 weeks total | \*\*Current Progress:\*\* Week 4 (19% complete)



\## 🔐 Security



\- ✅ All secrets stored in Azure Key Vault

\- ✅ No hardcoded credentials in source code

\- ✅ SQL Database encrypted at rest (TDE enabled)

\- ✅ HTTPS/TLS 1.2+ for all communications

\- ✅ Managed Identity for Azure service authentication

\- ✅ `.gitignore` protecting `local.settings.json`



\## 📚 Documentation



Comprehensive documentation for each phase:

\- \[Requirements Specification](docs/Stock\_Trading\_App\_Requirements.docx)

\- \[Phase 1 Completion](docs/Phase\_1\_Completion\_Documentation.md)

\- \[Phase 2 Completion](docs/Phase\_2\_Completion\_Documentation.md)



\## 🧪 Local Development Setup



\### Prerequisites

\- .NET 8 SDK

\- Visual Studio 2022 or VS Code

\- Azure subscription

\- Alpaca Markets account (Paper Trading)



\### Getting Started



1\. \*\*Clone the repository:\*\*

```bash

git clone https://github.com/Darmds110/AzureTradingSystem.git

cd AzureTradingSystem

```



2\. \*\*Create `local.settings.json`\*\* (in `TradingSystem.Functions` folder):

```json

{

&nbsp; "IsEncrypted": false,

&nbsp; "Values": {

&nbsp;   "AzureWebJobsStorage": "UseDevelopmentStorage=true",

&nbsp;   "FUNCTIONS\_WORKER\_RUNTIME": "dotnet-isolated",

&nbsp;   "AlpacaApiKey": "YOUR\_ALPACA\_API\_KEY",

&nbsp;   "AlpacaSecretKey": "YOUR\_ALPACA\_SECRET\_KEY",

&nbsp;   "AlpacaBaseUrl": "https://paper-api.alpaca.markets",

&nbsp;   "SqlConnectionString": "YOUR\_SQL\_CONNECTION\_STRING",

&nbsp;   "EmailSmtpServer": "smtp-mail.outlook.com",

&nbsp;   "EmailSmtpPort": "587",

&nbsp;   "EmailSmtpUsername": "YOUR\_EMAIL",

&nbsp;   "EmailSmtpPassword": "YOUR\_APP\_PASSWORD"

&nbsp; }

}

```

⚠️ \*\*NEVER commit `local.settings.json` to Git!\*\*



3\. \*\*Run locally:\*\*

```bash

cd TradingSystem.Functions

func start

```



Or press \*\*F5\*\* in Visual Studio 2022.



\## 📊 Database Schema



\*\*10 Tables:\*\*

\- `Portfolios` - Portfolio metadata and state

\- `Positions` - Current holdings

\- `Orders` - All order activity

\- `TradeHistory` - Completed trades

\- `StrategyConfigurations` - Trading strategy definitions

\- `MarketData` - Cached prices and indicators

\- `PerformanceMetrics` - Daily/weekly/monthly stats

\- `AuditLog` - System events

\- `BacktestResults` - Backtest outcomes

\- `NotificationHistory` - Email tracking



\*\*Sample Query:\*\*

```sql

SELECT Symbol, ClosePrice, RSI, SMA20, SMA50

FROM MarketData

WHERE DataDate = (SELECT MAX(DataDate) FROM MarketData)

ORDER BY RSI;

```



\## ⚠️ Disclaimer



\*\*This is an educational project for learning purposes only.\*\*



\- ❌ NOT financial advice

\- ❌ NOT investment advice

\- ❌ NOT intended for commercial use

\- ⚠️ Trading involves substantial risk of loss

\- ⚠️ Past performance does not guarantee future results

\- ⚠️ Only trade with money you can afford to lose



\## 📄 License



For educational purposes only. All rights reserved.



\## 🤝 Contributing



This is a personal learning project. Not accepting contributions at this time.



\## 📧 Contact



For questions about this educational project, please open an issue.



---



\*\*Repository:\*\* https://github.com/Darmds110/AzureTradingSystem  

\*\*Last Updated:\*\* January 28, 2026  

\*\*Current Phase:\*\* 3 of 10 (Portfolio Management)  

\*\*Status:\*\* Market data flowing ✅ | Ready for portfolio sync

