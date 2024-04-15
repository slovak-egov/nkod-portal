using Microsoft.AspNetCore.Http.HttpResults;
using NkodSk.Abstractions;
using System.IO;

namespace WebApi
{
    public class PublisherView
    {
        public Guid Id { get; set; }

        public string? Key { get; set; }

        public string? Name { get; set; }

        public bool IsPublic { get; set; }

        public int DatasetCount { get; set; }

        public Dictionary<string, int>? Themes { get; set; }

        public Dictionary<string, string>? NameAll { get; set; } 

        public string? Website { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? LegalForm { get; set; }

        internal static PublisherView MapFromRdf(Guid id, bool isPublic, int datasetCount, FoafAgent agent, Dictionary<string, int>? themes, string language, bool includeExtendedProperties)
        {
            PublisherView publisherView = new PublisherView
            {
                Id = id,
                Name = agent.GetName(language),
                Key = agent.Uri.ToString(),
                IsPublic = isPublic,
                DatasetCount = datasetCount,
                Themes = themes,
            };

            if (includeExtendedProperties)
            {
                publisherView.NameAll = agent.Name;
                publisherView.Website = agent.HomePage?.ToString();
                publisherView.Email = agent.EmailAddress;
                publisherView.Phone = agent.Phone;
                publisherView.LegalForm = agent.LegalForm?.ToString();
            }

            return publisherView;
        }
    }
}
