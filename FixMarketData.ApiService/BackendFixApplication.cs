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
    private readonly object _bookLock = new();
    private readonly Dictionary<string, PartialBookState> _bookBySymbol = [];
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

    private MarketDataTick? MapSnapshot(Message snapshot)
    {
        if (!snapshot.IsSetField(Tags.Symbol))
        {
            return null;
        }

        var symbol = snapshot.GetString(Tags.Symbol);

        var declaredEntries = snapshot.IsSetField(Tags.NoMDEntries)
            ? snapshot.GetInt(Tags.NoMDEntries)
            : 0;

        var actualGroups = snapshot.GroupCount(Tags.NoMDEntries);
        var entriesToRead = Math.Min(declaredEntries, actualGroups);

        decimal? bidPrice = null;
        decimal? askPrice = null;
        decimal? bidSize = null;
        decimal? askSize = null;

        if (entriesToRead > 0)
        {
            for (var i = 1; i <= entriesToRead; i++)
            {
                if (!TryReadEntryFromGroup(snapshot, i, out var entryType, out var entryPx, out var entrySize))
                {
                    continue;
                }

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
        }
        else if (TryReadEntryFromFields(snapshot, out var entryType, out var entryPx, out var entrySize))
        {
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

        lock (_bookLock)
        {
            if (!_bookBySymbol.TryGetValue(symbol, out var state))
            {
                state = new PartialBookState();
                _bookBySymbol[symbol] = state;
            }

            if (bidPrice is not null && bidSize is not null)
            {
                state.BidPrice = bidPrice.Value;
                state.BidSize = bidSize.Value;
            }

            if (askPrice is not null && askSize is not null)
            {
                state.AskPrice = askPrice.Value;
                state.AskSize = askSize.Value;
            }

            if (state.BidPrice is null || state.BidSize is null || state.AskPrice is null || state.AskSize is null)
            {
                return null;
            }

            return new MarketDataTick
            {
                Symbol = symbol,
                BidPrice = state.BidPrice.Value,
                AskPrice = state.AskPrice.Value,
                BidSize = state.BidSize.Value,
                AskSize = state.AskSize.Value,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    private static bool TryReadEntryFromGroup(Message snapshot, int groupIndex, out char entryType, out decimal entryPx, out decimal entrySize)
    {
        entryType = default;
        entryPx = default;
        entrySize = default;

        if (groupIndex < 1 || groupIndex > snapshot.GroupCount(Tags.NoMDEntries))
        {
            return false;
        }

        var group = new Group(Tags.NoMDEntries, Tags.MDEntryType);
        snapshot.GetGroup(groupIndex, group);

        if (!group.IsSetField(Tags.MDEntryType) || !group.IsSetField(Tags.MDEntryPx) || !group.IsSetField(Tags.MDEntrySize))
        {
            return false;
        }

        entryType = group.GetChar(Tags.MDEntryType);
        entryPx = group.GetDecimal(Tags.MDEntryPx);
        entrySize = group.GetDecimal(Tags.MDEntrySize);
        return true;
    }
    private static bool TryReadEntryFromFields(Message snapshot, out char entryType, out decimal entryPx, out decimal entrySize)
    {
        if (!snapshot.IsSetField(Tags.MDEntryType) || !snapshot.IsSetField(Tags.MDEntryPx) || !snapshot.IsSetField(Tags.MDEntrySize))
        {
            entryType = default;
            entryPx = default;
            entrySize = default;
            return false;
        }

        entryType = snapshot.GetChar(Tags.MDEntryType);
        entryPx = snapshot.GetDecimal(Tags.MDEntryPx);
        entrySize = snapshot.GetDecimal(Tags.MDEntrySize);
        return true;
    }

    private sealed class PartialBookState
    {
        public decimal? BidPrice { get; set; }
        public decimal? AskPrice { get; set; }
        public decimal? BidSize { get; set; }
        public decimal? AskSize { get; set; }
    }
}
