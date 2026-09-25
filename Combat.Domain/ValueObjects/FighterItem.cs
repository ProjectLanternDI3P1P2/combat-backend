namespace Combat.Domain.ValueObjects;

public sealed record FighterItem
{
    public FighterItem(Guid itemId, string name, string category, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        ItemId = itemId;
        Name = name;
        Category = category;
        Quantity = quantity;
    }

    public Guid ItemId { get; }
    public string Name { get; }
    public string Category { get; }
    public int Quantity { get; }
}
