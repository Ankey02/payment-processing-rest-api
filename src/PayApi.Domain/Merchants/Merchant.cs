namespace PayApi.Domain.Merchants;

public class Merchant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }

    private Merchant() { }

    public static Merchant Create(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CreatedAt = DateTime.UtcNow
    };
}
