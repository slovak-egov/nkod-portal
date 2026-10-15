namespace WebApi
{
    public record CodelistItemView(string Id, string Label, decimal Priority = 0, string? ShortId = null)
    {
    }
}
