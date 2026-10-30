namespace FixMarketData.Shared;

public sealed class MarketDataTick
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Symbol { get; set; } = string.Empty;
    public decimal BidPrice { get; set; }
    public decimal AskPrice { get; set; }
    public decimal BidSize { get; set; }
    public decimal AskSize { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
