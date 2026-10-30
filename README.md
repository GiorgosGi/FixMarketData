# FixMarketData

Local FIX market data demo built on .NET 10.

## Projects

- `FixMarketData.Simulator` (Console): QuickFIX/n FIX 4.4 acceptor that publishes random snapshots.
- `FixMarketData.Backend` (project folder: `FixMarketData.ApiService`): ASP.NET Core backend acting as FIX initiator, SQL Server persistence, and SignalR hub.
- `FixMarketData.Shared`: Shared EF Core model and DbContext.
- `FixMarketData.Ui` (project folder: `FixMarketData.Web`): Blazor Server dashboard that streams live prices from SignalR.

## Prerequisites

- .NET 10 SDK
- SQL Server LocalDB (default connection string uses `(localdb)\\MSSQLLocalDB`)

## Configuration

### Backend (`FixMarketData.ApiService/appsettings*.json`)

- `ConnectionStrings:MarketDataDb` - SQL Server connection string
- `UiBaseUrl` - allowed CORS origin for the UI

### UI (`FixMarketData.Web/appsettings*.json`)

- `BackendBaseUrl` - backend base URL used for SignalR connection (default: `https://localhost:7544`)

### FIX Settings

- Simulator acceptor config: `FixMarketData.Simulator/acceptor.cfg` (port `5001`)
- Backend initiator config: `FixMarketData.ApiService/initiator.cfg` (connects to `localhost:5001`)

## Run

Open separate terminals from repository root:

1. Start simulator:
   - `dotnet run --project FixMarketData.Simulator`
2. Start backend:
   - `dotnet run --project FixMarketData.ApiService`
3. Start UI:
   - `dotnet run --project FixMarketData.Web`

Then open the UI URL (from launch settings) and view live symbol updates.
