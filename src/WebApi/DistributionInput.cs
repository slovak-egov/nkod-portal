using NkodSk.Abstractions;

namespace WebApi
{
    public class DistributionInput
    {
        public string? Id { get; set; }

        public TermsOfUseInput? TermsOfUse { get; set; }

        public string? DownloadUrl { get; set; }

        public string? AccessUrl { get; set; }

        public string? Format { get; set; }

        public string? MediaType { get; set; }

        public string? ConformsTo { get; set; }

        public string? CompressFormat { get; set; }

        public string? PackageFormat { get; set; }

        public Dictionary<string, string>? Title { get; set; }

        public string? FileId { get; set; }

        public DcatDistribution? MapToRdf(out Dictionary<string, string>? errors)
        {
            errors = null;
            return null;
        }
    }
}
