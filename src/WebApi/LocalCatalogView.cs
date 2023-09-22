using NkodSk.Abstractions;

namespace WebApi
{
    public class LocalCatalogView
    {
        public Guid Id { get; set; }

        public bool IsPublic { get; set; }

        public string? Name { get; set; }

        public Dictionary<string, string>? NameAll { get; set; }

        public string? Description { get; set; }

        public Dictionary<string, string>? DescriptionAll { get; set; }

        public string? PublisherId { get; set; }

        public PublisherView? Publisher { get; set; }

        public CardView? ContactPoint { get; set; }

        public Uri? HomePage { get; set; }

        public static async Task<LocalCatalogView> MapFromRdf(FileMetadata metadata, DcatCatalog catalogRdf, string language, bool fetchAllLanguages)
        {
            VcardKind? contactPoint = catalogRdf.ContactPoint;

            LocalCatalogView view = new LocalCatalogView
            {
                Id = metadata.Id,
                IsPublic = metadata.IsPublic,
                Name = catalogRdf.GetTitle(language),
                Description = catalogRdf.GetDescription(language),
                PublisherId = metadata.Publisher,
                ContactPoint = contactPoint is not null ? CardView.MapFromRdf(contactPoint, language, fetchAllLanguages) : null,
                HomePage = catalogRdf.HomePage
            };

            if (fetchAllLanguages)
            {
                view.NameAll = catalogRdf.Title;
                view.DescriptionAll = catalogRdf.Description;
            }

            return view;
        }
    }
}
