using FixMarketData.Shared;
using Microsoft.AspNetCore.SignalR;

namespace FixMarketData.Backend;

public sealed class MarketDataHub : Hub
{
    public async Task BroadcastMarketData(MarketDataTick tick)
    {
        await Clients.All.SendAsync("ReceiveMarketData", tick);
    }
}
