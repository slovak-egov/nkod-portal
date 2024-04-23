namespace CMS.Suggestions
{
    public class SuggestionQueryDto
    {
        public ContentTypes Type { get; set; }

        [RequiredIfCustom(nameof(Type), ContentTypes.DQ, ContentTypes.MQ)]
        public string Id { get; set; }
    }
}