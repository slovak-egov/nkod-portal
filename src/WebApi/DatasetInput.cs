using NkodSk.Abstractions;

namespace WebApi
{
    public class DatasetInput
    {
        public string? Id { get; set; }

        public bool IsPublic { get; set; }

        public Dictionary<string, string>? Name { get; set; }

        public Dictionary<string, string>? Description { get; set; }

        public List<string>? Themes { get; set; }

        public string? AccrualPeriodicity { get; set; }

        public Dictionary<string, string>? Kwywords { get; set; }

        public string? Type { get; set; }

        public List<string>? Spatial { get; set; }

        public TemporalInput? Temporal { get; set; }

        public CardInput? ContactPoint { get; set; }

        public string? Documentation { get; set; }

        public string? Specification { get; set; }

        public List<string>? EuroVocThemes { get; set; }

        public string? SpatialResolutionInMeters { get; set; }

        public string? TemporalResolution { get; set; }

        public string? IsPartOf { get; set; }

        public DcatDataset? MapToRdf(Uri publisher, out Dictionary<string, string>? errors)
        {
            errors = new Dictionary<string, string>();
            DcatDataset dataset = DcatDataset.Create(new Uri($"http://data.gov.sk/dataset/{Guid.NewGuid()}"));
            dataset.SetTitle(Name ?? new Dictionary<string, string>());
            dataset.Publisher = publisher;
            return dataset;
        }
    }
}
