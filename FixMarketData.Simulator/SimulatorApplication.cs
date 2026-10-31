using System.Collections.Concurrent;
using QuickFix;
using QuickFix.Fields;
using Message = QuickFix.Message;

namespace FixMarketData.Simulator;

public sealed class SimulatorApplication : IApplication, IDisposable
{
    private readonly ConcurrentDictionary<SessionID, byte> _sessions = [];
    private readonly Dictionary<string, decimal> _basePrices = new()
    {
        ["AAPL"] = 195.25m,
        ["MSFT"] = 421.10m,
        ["EURUSD"] = 1.0850m
    };

    private readonly Random _random = new();
    private readonly object _priceLock = new();
    private System.Timers.Timer? _timer;

    public void OnCreate(SessionID sessionID)
    {
    }

    public void OnLogon(SessionID sessionID)
    {
        _sessions.TryAdd(sessionID, 0);
        EnsureTimerStarted();
        Console.WriteLine($"Session logon: {sessionID}");
    }

    public void OnLogout(SessionID sessionID)
    {
        _sessions.TryRemove(sessionID, out _);
        Console.WriteLine($"Session logout: {sessionID}");
    }

    public void ToAdmin(Message message, SessionID sessionID)
    {
    }

    public void ToApp(Message message, SessionID sessionID)
    {
    }

    public void FromAdmin(Message message, SessionID sessionID)
    {
    }

    public void FromApp(Message message, SessionID sessionID)
    {
    }

    private void EnsureTimerStarted()
    {
        if (_timer is not null)
        {
            return;
        }

        _timer = new System.Timers.Timer(750);
        _timer.AutoReset = true;
        _timer.Elapsed += (_, _) => PublishSnapshots();
        _timer.Start();
    }

    private void PublishSnapshots()
    {
        if (_sessions.IsEmpty)
        {
            return;
        }

        List<SessionID> activeSessions = [.. _sessions.Keys];

        foreach (var symbol in _basePrices.Keys)
        {
            var (bidSnapshot, askSnapshot) = NextTicks(symbol);

            foreach (var sessionId in activeSessions)
            {
                Session.SendToTarget(bidSnapshot, sessionId);
                Session.SendToTarget(askSnapshot, sessionId);
            }
        }
    }

    private (Message BidSnapshot, Message AskSnapshot) NextTicks(string symbol)
    {
        decimal mid;
        lock (_priceLock)
        {
            var current = _basePrices[symbol];
            var change = (decimal)(_random.NextDouble() - 0.5d) * 0.08m;
            mid = Math.Round(current + change, 5, MidpointRounding.AwayFromZero);
            _basePrices[symbol] = mid;
        }

        var spread = Math.Round(mid * 0.0002m, 5, MidpointRounding.AwayFromZero);
        var bid = Math.Round(mid - spread, 5, MidpointRounding.AwayFromZero);
        var ask = Math.Round(mid + spread, 5, MidpointRounding.AwayFromZero);
        var bidSize = Math.Round(100 + (decimal)_random.NextDouble() * 900, 5, MidpointRounding.AwayFromZero);
        var askSize = Math.Round(100 + (decimal)_random.NextDouble() * 900, 5, MidpointRounding.AwayFromZero);

        var bidSnapshot = CreateSnapshot(symbol, MDEntryType.BID, bid, bidSize);
        var askSnapshot = CreateSnapshot(symbol, MDEntryType.OFFER, ask, askSize);

        return (bidSnapshot, askSnapshot);
    }

    private static Message CreateSnapshot(string symbol, char entryType, decimal entryPrice, decimal entrySize)
    {
        var snapshot = new Message();
        snapshot.Header.SetField(new MsgType(MsgType.MARKET_DATA_SNAPSHOT_FULL_REFRESH));
        snapshot.SetField(new Symbol(symbol));
        snapshot.SetField(new NoMDEntries(1));

        var entry = new Group(Tags.NoMDEntries, Tags.MDEntryType);
        entry.SetField(new MDEntryType(entryType));
        entry.SetField(new MDEntryPx(entryPrice));
        entry.SetField(new MDEntrySize(entrySize));
        snapshot.AddGroup(entry);

        return snapshot;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
