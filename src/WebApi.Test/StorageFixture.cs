﻿using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Algebra;

namespace WebApi.Test
{
    public class StorageFixture : IDisposable
    {
        private readonly string path;

        private static int index = 0;

        public StorageFixture()
        {
            path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        public void CreateFile(FileState state)
        {
            bool isPublic = Storage.ShouldBePublic(state.Metadata);
            string folder = Path.Combine(path, Storage.GetDefaultSubfolderName(state.Metadata));
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string filePath = Path.Combine(folder, state.Metadata.Id.ToString("N") + (isPublic ? ".ttl" : string.Empty));
            File.WriteAllText(filePath, state.Content);
            UpdateMetadata(state.Metadata);
        }

        public void UpdateMetadata(FileMetadata metadata)
        {
            string metadataPath = Path.Combine(path, "protected", metadata.Id.ToString("N") + ".metadata");
            metadata.SaveTo(metadataPath);
        }

        public Uri CreateUri()
        {
            int index = Interlocked.Increment(ref StorageFixture.index);
            return new Uri($"http://example.com/dataset/{index}");
        }

        public Guid CreateDataset(string nameSk, string publisher, bool isPublic = true, Uri? type = null, string[]? keywordsSk = null, string? nameEn = null, string[]? keywordsEn = null, Guid? parent = null)
        {
            DcatDataset dataset = DcatDataset.Create();

            Dictionary<string, string> names = new Dictionary<string, string> { { "sk", nameSk } };
            if (nameEn is not null)
            {
                names["en"] = nameEn;
            }
            dataset.SetTitle(names);
            dataset.Type = type is not null ? new[] { type } : Enumerable.Empty<Uri>();
            dataset.Publisher = new Uri(publisher);
            dataset.SetKeywords(new Dictionary<string, List<string>> { { "sk", keywordsSk?.ToList() ?? new List<string>() }, { "en", keywordsEn?.ToList() ?? new List<string>() } });

            FileMetadata metadata = dataset.UpdateMetadata(true);
            if (parent is not null)
            {
                metadata = metadata with { ParentFile = parent };
            }
            CreateFile(new FileState(metadata, dataset.ToString()));
            return metadata.Id;
        }

        public Guid CreatePublisher(string nameSk, string? id = null, string? nameEn = null, bool isPublic = true)
        {
            FoafAgent agent = FoafAgent.Create(id is not null ? new Uri(id) : CreateUri());

            Dictionary<string, string> names = new Dictionary<string, string> { { "sk", nameSk } };
            if (nameEn is not null)
            {
                names["en"] = nameEn;
            }
            agent.SetNames(names);

            FileMetadata metadata = agent.UpdateMetadata();
            metadata = metadata with { IsPublic = isPublic };
            CreateFile(new FileState(metadata, agent.ToString()));
            return metadata.Id;
        }

        public Guid CreateLocalCatalog(string name, string publisher, string? nameEn = null, string? description = null, string? descriptionEn = null)
        {
            DcatCatalog catalog = DcatCatalog.Create();

            Dictionary<string, string> names = new Dictionary<string, string> { { "sk", name } };
            if (nameEn is not null)
            {
                names["en"] = nameEn;
            }
            catalog.SetTitle(names);

            names.Clear();
            if (description is not null)
            {
                names["sk"] = description;
            }
            if (descriptionEn is not null)
            {
                names["en"] = descriptionEn;
            }
            catalog.SetDescription(names);

            catalog.Publisher = new Uri(publisher);

            FileMetadata metadata = catalog.UpdateMetadata();
            CreateFile(new FileState(metadata, catalog.ToString()));
            return metadata.Id;
        }

        public Guid CreateDistrbution(FileMetadata datasetMetadata, Uri? authorsWorkType, Uri? originalDatabaseType, Uri? databaseProtectedBySpecialRightsType, Uri? personalDataContainmentType,
            Uri? downloadUri, Uri? accessUri, Uri? format, Uri? mediaType, Uri? conformsTo = null, Uri? compressFormat = null, Uri? packageFormat = null, string? title = null, string? titleEn = null, Uri? accessService = null)
        {
            DcatDistribution distribution = DcatDistribution.Create(datasetMetadata.Id);

            distribution.SetTermsOfUse(authorsWorkType, originalDatabaseType, databaseProtectedBySpecialRightsType, personalDataContainmentType);
            distribution.DownloadUrl = downloadUri;
            distribution.AccessUrl = accessUri;
            distribution.Format = format;
            distribution.MediaType = mediaType;
            distribution.ConformsTo = conformsTo;
            distribution.CompressFormat = compressFormat;
            distribution.PackageFormat = packageFormat;
            distribution.AccessService = accessService;

            LanguageDependedTexts titles = new LanguageDependedTexts();
            if (title is not null)
            {
                titles["sk"] = title;
            }
            if (titleEn is not null)
            {
                titles["en"] = titleEn;
            }
            distribution.SetTitle(titles);

            datasetMetadata = distribution.UpdateDatasetMetadata(datasetMetadata);
            UpdateMetadata(datasetMetadata);

            FileMetadata metadata = distribution.UpdateMetadata(datasetMetadata);
            CreateFile(new FileState(metadata, distribution.ToString()));

            return metadata.Id;
        }

        public (Guid, Guid, Guid[]) CreateFullDataset(string? publisher = null)
        {
            DcatDataset dataset = DcatDataset.Create();

            publisher ??= "https://data.gov.sk/id/publisher/full";
            Guid publisherId = CreatePublisher("Ministerstvo hospodárstva SR", id: publisher, nameEn: "Ministry of economy");

            dataset.SetTitle(new Dictionary<string, string> { { "sk", "titleSk" }, { "en", "titleEn" } });
            dataset.SetDescription(new Dictionary<string, string> { { "sk", "descriptionSk" }, { "en", "descriptionEn" } });
            dataset.Publisher = new Uri(publisher);
            dataset.Themes = new[] { 
                new Uri("http://publications.europa.eu/resource/dataset/data-theme/1"), 
                new Uri("http://publications.europa.eu/resource/dataset/data-theme/2"),
                new Uri(DcatDataset.EuroVocPrefix + "6409"), 
                new Uri(DcatDataset.EuroVocPrefix +"6410")};
            dataset.AccrualPeriodicity = new Uri("http://publications.europa.eu/resource/dataset/frequency/1");
            dataset.SetKeywords(new Dictionary<string, List<string>> { { "sk", new List<string> { "keyword1Sk", "keyword2Sk" } }, { "en", new List<string> { "keyword1En", "keyword2En" } } });
            dataset.Type = new[] { new Uri("https://data.gov.sk/set/codelist/dataset-type/1") };
            dataset.Spatial = new[] { new Uri("http://publications.europa.eu/resource/dataset/country/1"), new Uri("http://publications.europa.eu/resource/dataset/country/2") };
            dataset.SetTemporal(new DateOnly(2023, 8, 16), new DateOnly(2023, 9, 10));
            dataset.SetContactPoint(new LanguageDependedTexts { { "sk", "nameSk" }, { "en", "nameEn" } }, "test@example.com");
            dataset.LandingPage = new Uri("http://example.com/documentation");
            dataset.Specification = new Uri("http://example.com/specification");
            dataset.SpatialResolutionInMeters = 10;
            dataset.TemporalResolution = "P2D";
            dataset.IsPartOf = new Uri("http://example.com/test-dataset");
            dataset.IsPartOfInternalId = "XXX";
            dataset.SetEuroVocLabelThemes(new Dictionary<string, List<string>> {
                { "sk", new List<string> { "nepovolená likvidácia odpadu", "chemický odpad" } },
                { "en", new List<string> { "unauthorised dumping", "chemical waste" } }
            });
            dataset.Issued = new DateTimeOffset(2023, 8, 16, 0, 0, 0, TimeSpan.Zero);
            dataset.Modified = new DateTimeOffset(2023, 8, 16, 0, 0, 0, TimeSpan.Zero);

            FileMetadata metadata = dataset.UpdateMetadata(true);
            CreateFile(new FileState(metadata, dataset.ToString()));

            Guid distributionId = CreateDistrbution(metadata, 
                new Uri("https://data.gov.sk/def/ontology/law/authorsWorkType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/originalDatabaseType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1"),
                new Uri("https://data.gov.sk/def/ontology/law/personalDataContainmentType/1"),
                new Uri("http://data.gov.sk/download"),
                new Uri("http://data.gov.sk/"),
                new Uri("http://publications.europa.eu/resource/dataset/file-type/1"),
                new Uri("http://www.iana.org/assignments/media-types/text/csv"),
                new Uri("http://data.gov.sk/specification"),
                new Uri("http://www.iana.org/assignments/media-types/application/zip"),
                new Uri("http://www.iana.org/assignments/media-types/application/zip"),
                "TitleSk",
                "TitleEn",
                new Uri("http://example.com/access-service")
            );

            return (metadata.Id, publisherId, new[] { distributionId });
        }

        public (Guid, Guid) CreateFullLocalCatalog(string? publisher = null)
        {
            DcatCatalog catalog = DcatCatalog.Create();

            publisher ??= "https://data.gov.sk/id/publisher/full";
            Guid publisherId = CreatePublisher("Ministerstvo hospodárstva SR", id: publisher, nameEn: "Ministry of economy");

            catalog.SetTitle(new Dictionary<string, string> { { "sk", "titleSk" }, { "en", "titleEn" } });
            catalog.SetDescription(new Dictionary<string, string> { { "sk", "descriptionSk" }, { "en", "descriptionEn" } });
            catalog.Publisher = new Uri(publisher);
            catalog.SetContactPoint(new LanguageDependedTexts { { "sk", "nameSk" }, { "en", "nameEn" } }, "test@example.com");
            catalog.HomePage = new Uri("http://data.gov.sk/");
            catalog.Type = new Uri(DcatCatalog.LocalCatalogTypeCodelist + "/2");
            catalog.EndpointUrl = new Uri("http://data.gov.sk/sparql");

            FileMetadata metadata = catalog.UpdateMetadata();
            CreateFile(new FileState(metadata, catalog.ToString()));

            return (metadata.Id, publisherId);
        }

        public void CreateCodelistFile(string id, Dictionary<string, LanguageDependedTexts> values)
        {
            SkosConceptScheme conceptScheme = SkosConceptScheme.Create(new Uri(id));
            foreach ((string conceptId, LanguageDependedTexts texts) in values)
            {
                IUriNode subject = conceptScheme.CreateSubject("skos:Concept", "skos:Concept", new Uri(conceptId));
                SkosConcept concept = new SkosConcept(conceptScheme.Graph, subject);
                concept.SetTexts("skos:prefLabel", texts);
            }

            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), id, FileType.Codelist, null, null, true, id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            CreateFile(new FileState(metadata, conceptScheme.ToString()));
        }

        public void CreateDatasetCodelists()
        {
            CreateCodelistFile(DcatDataset.ThemeCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "http://publications.europa.eu/resource/dataset/data-theme/1", new LanguageDependedTexts{ { "sk", "theme1sk" }, { "en", "theme1en" } } },
                { "http://publications.europa.eu/resource/dataset/data-theme/2", new LanguageDependedTexts{ { "sk", "theme2sk" }, { "en", "theme2en" } } }
            });
            CreateCodelistFile(DcatDataset.AccrualPeriodicityCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "http://publications.europa.eu/resource/dataset/frequency/1", new LanguageDependedTexts{ { "sk", "frequency1sk" }, { "en", "frequency1en" } } }
            });
            CreateCodelistFile(DcatDataset.TypeCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "https://data.gov.sk/set/codelist/dataset-type/1", new LanguageDependedTexts{ { "sk", "type1sk" }, { "en", "type1en" } } }
            });
            CreateCodelistFile(DcatDataset.SpatialCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "http://publications.europa.eu/resource/dataset/country/1", new LanguageDependedTexts{ { "sk", "country1sk" }, { "en", "country1en" } } },
                { "http://publications.europa.eu/resource/dataset/country/2", new LanguageDependedTexts{ { "sk", "country2sk" }, { "en", "country2en" } } }
            });
            CreateCodelistFile(DcatDataset.EuroVocThemeCodelist, new Dictionary<string, LanguageDependedTexts>());
        }

        public void CreateDistributionCodelists()
        {
            CreateCodelistFile(DcatDistribution.LicenseCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "https://data.gov.sk/def/ontology/law/authorsWorkType/1", new LanguageDependedTexts{ { "sk", "work1sk" }, { "en", "work1en" } } },
                { "https://data.gov.sk/def/ontology/law/originalDatabaseType/1", new LanguageDependedTexts{ { "sk", "type1sk" }, { "en", "type1en" } } },
                { "https://data.gov.sk/def/ontology/law/databaseProtectedBySpecialRightsType/1", new LanguageDependedTexts{ { "sk", "rights1sk" }, { "en", "rights1en" } } }
            });
            CreateCodelistFile(DcatDistribution.PersonalDataContainmentTypeCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "https://data.gov.sk/def/ontology/law/personalDataContainmentType/1", new LanguageDependedTexts{ { "sk", "personal1sk" }, { "en", "personal1en" } } },
            });
            CreateCodelistFile(DcatDistribution.FormatCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "http://publications.europa.eu/resource/dataset/file-type/1", new LanguageDependedTexts{ { "sk", "fileType1sk" }, { "en", "fileType1en" } } },
                { "http://publications.europa.eu/resource/dataset/file-type/2", new LanguageDependedTexts{ { "sk", "fileType2sk" }, { "en", "fileType2en" } } },
            });
            CreateCodelistFile(DcatDistribution.MediaTypeCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "http://www.iana.org/assignments/media-types/text/csv", new LanguageDependedTexts{ { "sk", "CSV" }, { "en", "CSV" } } },
                { "http://www.iana.org/assignments/media-types/application/zip", new LanguageDependedTexts{ { "sk", "ZIP" }, { "en", "ZIP" } } },
            });
        }

        public void CreateLocalCatalogCodelists()
        {
            CreateCodelistFile(DcatCatalog.LocalCatalogTypeCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { DcatCatalog.LocalCatalogTypeCodelist + "/1", new LanguageDependedTexts{ { "sk", "DCAT Dokumenty" }, { "en", "DCAT Dokuments" } } },
                { DcatCatalog.LocalCatalogTypeCodelist + "/2", new LanguageDependedTexts{ { "sk", "SPARQL" }, { "en", "SPARQL" } } },
            });
        }

        public void CreatePublisherCodelists()
        {
            CreateCodelistFile(FoafAgent.LegalFormCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "https://data.gov.sk/def/legal-form-type/321", new LanguageDependedTexts{ { "sk", "Rozpočtová organizácia" }, { "en", "Rozpočtová organizácia EN" } } },
                { "https://data.gov.sk/def/legal-form-type/331", new LanguageDependedTexts{ { "sk", "Príspevková organizácia" }, { "en", "Príspevková organizácia EN" } } },
            });
        }

        public Guid CreateDistributionFile(string name, string content, bool isPublic = true)
        {
            Guid id = Guid.NewGuid();
            FileMetadata metadata = new FileMetadata(id, id.ToString(), FileType.DistributionFile, null, null, isPublic, name, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            CreateFile(new FileState(metadata, content));
            return id;
        }

        public string GetStoragePath(bool includeFiles = true)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (Directory.Exists(path))
            {
                foreach (string p in Directory.EnumerateDirectories(path))
                {
                    Directory.Delete(p, true);
                }
                foreach (string p in Directory.EnumerateFiles(path))
                {
                    File.Delete(p);
                }

                if (includeFiles)
                {
                    string folderPath = Path.Combine(path, "public");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    folderPath = Path.Combine(path, "protected");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                }
            }

            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
