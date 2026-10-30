using FixMarketData.Shared;
using Microsoft.AspNetCore.SignalR;
using QuickFix;
using QuickFix.Fields;
using Message = QuickFix.Message;

namespace FixMarketData.Backend;

public sealed class BackendFixApplication(
    IServiceScopeFactory scopeFactory,
    IHubContext<MarketDataHub> hubContext,
    ILogger<BackendFixApplication> logger) : IApplication
{
    public void OnCreate(SessionID sessionID)
    {
        logger.LogInformation("FIX session created: {SessionId}", sessionID);
    }

    public void OnLogon(SessionID sessionID)
    {
        logger.LogInformation("FIX session logon: {SessionId}", sessionID);
    }

    public void OnLogout(SessionID sessionID)
    {
        logger.LogInformation("FIX session logout: {SessionId}", sessionID);
    }

    public void ToAdmin(Message message, SessionID sessionID)
    {
    }

    public void ToApp(Message message, SessionID sessionId)
    {
    }

    public void FromAdmin(Message message, SessionID sessionID)
    {
    }

    public void FromApp(Message message, SessionID sessionID)
    {
        if (message.Header.GetString(Tags.MsgType) != MsgType.MARKET_DATA_SNAPSHOT_FULL_REFRESH)
        {
            return;
        }

        _ = PersistAndBroadcastAsync(message);
    }

    private async Task PersistAndBroadcastAsync(Message snapshot)
    {
        try
        {
            var tick = MapSnapshot(snapshot);
            if (tick is null)
            {
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MarketDbContext>();

            dbContext.MarketDataTicks.Add(tick);
            await dbContext.SaveChangesAsync();

            await hubContext.Clients.All.SendAsync("ReceiveMarketData", tick);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process incoming market data snapshot.");
        }
    }

    private static MarketDataTick? MapSnapshot(Message snapshot)
    {
        if (!snapshot.IsSetField(Tags.Symbol) || !snapshot.IsSetField(Tags.NoMDEntries))
        {
            return null;
        }

        var symbol = snapshot.GetString(Tags.Symbol);
        var totalEntries = snapshot.GetInt(Tags.NoMDEntries);

        decimal? bidPrice = null;
        decimal? askPrice = null;
        decimal? bidSize = null;
        decimal? askSize = null;

        for (var i = 1; i <= totalEntries; i++)
        {
            var group = new Group(Tags.NoMDEntries, Tags.MDEntryType);
            snapshot.GetGroup(i, group);

            var entryType = group.GetChar(Tags.MDEntryType);
            var entryPx = group.GetDecimal(Tags.MDEntryPx);
            var entrySize = group.GetDecimal(Tags.MDEntrySize);

            if (entryType == MDEntryType.BID)
            {
                bidPrice = entryPx;
                bidSize = entrySize;
            }
            else if (entryType == MDEntryType.OFFER)
            {
                askPrice = entryPx;
                askSize = entrySize;
            }
        }

        if (bidPrice is null || askPrice is null || bidSize is null || askSize is null)
        {
            return null;
        }

        return new MarketDataTick
        {
            Symbol = symbol,
            BidPrice = bidPrice.Value,
            AskPrice = askPrice.Value,
            BidSize = bidSize.Value,
            AskSize = askSize.Value,
            Timestamp = DateTime.UtcNow
        };
    }
}
