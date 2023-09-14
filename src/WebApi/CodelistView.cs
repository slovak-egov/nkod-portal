namespace WebApi
{
    public record CodelistView(string Id, string Label, List<CodelistItemView> Values)
    {
    }
}
