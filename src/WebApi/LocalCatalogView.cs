using NkodSk.Abstractions;

namespace WebApi
{
    public class LocalCatalogView
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? PublisherId { get; set; }

        public PublisherView? Publisher { get; set; }

        public CardView? ContactPoint { get; set; }

        public Uri? HomePage { get; set; }

        public static async Task<LocalCatalogView> MapFromRdf(FileMetadata metadata, DcatCatalog catalogRdf, string language)
        {
            VcardKind? contactPoint = catalogRdf.ContactPoint;

            LocalCatalogView view = new LocalCatalogView
            {
                Id = metadata.Id,
                Name = catalogRdf.GetTitle(language),
                Description = catalogRdf.GetDescription(language),
                PublisherId = metadata.Publisher,
                ContactPoint = contactPoint is not null ? new CardView { Name = contactPoint.GetName(language), Email = contactPoint.Email } : null,
                HomePage = catalogRdf.HomePage
            };

            return view;
        }
    }
}
