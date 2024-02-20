using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;

namespace Frontend.Test
{
    class StorageFixture
    {
        private readonly string path;

        private static int index;

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
            string metadataPath = Path.Combine(path, "protected", state.Metadata.Id.ToString("N") + ".metadata");
            File.WriteAllText(filePath, state.Content);
            state.Metadata.SaveTo(metadataPath);
        }

        private Uri CreateUri()
        {
            int index = Interlocked.Increment(ref StorageFixture.index);
            return new Uri($"http://example.com/dataset/{index}");
        }

        public Guid CreateDataset(string name, string publisher)
        {
            DcatDataset dataset = DcatDataset.Create();
            dataset.SetTitle(new Dictionary<string, string> { { "sk", name } });
            dataset.Publisher = new Uri(publisher);
            return CreateDataset(dataset);
        }

        public Guid CreateDataset(DcatDataset dataset)
        {
            FileMetadata metadata = dataset.UpdateMetadata(true);
            CreateFile(new FileState(metadata, dataset.ToString()));
            return metadata.Id;
        }

        public Guid CreateDistribution(Guid datasetId, string publisher)
        {
            DcatDistribution distribution = DcatDistribution.Create(datasetId);
            distribution.SetTermsOfUse(
                new Uri("http://publications.europa.eu/resource/authority/licence/CC_BY_4_0"),
                new Uri("http://publications.europa.eu/resource/authority/licence/CC_BY_4_0"),
                new Uri("http://publications.europa.eu/resource/authority/licence/CC_BY_4_0"),
                new Uri("https://data.gov.sk/def/personal-data-occurence-type/2"),
                string.Empty,
                string.Empty);
            distribution.DownloadUrl = new Uri("http://example.com/download");
            distribution.AccessUrl = distribution.DownloadUrl;
            distribution.Format = new Uri("http://publications.europa.eu/resource/dataset/file-type/1");
            distribution.MediaType = new Uri("http://www.iana.org/assignments/media-types/text/csv");
            distribution.CompressFormat = new Uri("http://www.iana.org/assignments/media-types/application/zip");
            distribution.PackageFormat = distribution.CompressFormat;
            return CreateDistribution(datasetId, publisher, distribution);
        }

        public Guid CreateDistribution(Guid datasetId, string publisher, DcatDistribution distribution)
        {
            FileMetadata metadata = distribution.UpdateMetadata(datasetId, publisher);
            CreateFile(new FileState(metadata, distribution.ToString()));
            return metadata.Id;
        }

        public Guid CreateLocalCatalog(string name, string publisher)
        {
            DcatCatalog catalog = DcatCatalog.Create();
            catalog.SetTitle(new Dictionary<string, string> { { "sk", name } });
            catalog.Publisher = new Uri(publisher);
            return CreateLocalCatalog(catalog);
        }

        public Guid CreateLocalCatalog(DcatCatalog catalog)
        {
            FileMetadata metadata = catalog.UpdateMetadata();
            CreateFile(new FileState(metadata, catalog.ToString()));
            return metadata.Id;
        }

        public Guid CreatePublisher(string publisher, bool isPublic, string name = "Test Publisher")
        {
            FoafAgent agent = FoafAgent.Create(new Uri(publisher));
            agent.SetNames(new Dictionary<string, string> { { "sk", name } });
            return CreatePublisher(agent, isPublic);
        }

        public Guid CreatePublisher(FoafAgent agent, bool isPublic)
        {
            FileMetadata metadata = agent.UpdateMetadata();
            metadata = metadata with { IsPublic = isPublic };
            CreateFile(new FileState(metadata, agent.ToString()));
            return metadata.Id;
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
                { "http://publications.europa.eu/resource/dataset/frequency/1", new LanguageDependedTexts{ { "sk", "frequency1sk" }, { "en", "frequency1en" } } },
                { "http://publications.europa.eu/resource/dataset/frequency/2", new LanguageDependedTexts{ { "sk", "frequency2sk" }, { "en", "frequency2en" } } }
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
                { "https://data.gov.sk/def/authors-work-type/1", new LanguageDependedTexts{ { "sk", "work1sk" }, { "en", "work1en" } } },
                { "https://data.gov.sk/def/original-database-type/1", new LanguageDependedTexts{ { "sk", "type1sk" }, { "en", "type1en" } } },
                { "https://data.gov.sk/def/codelist/database-creator-special-rights-type/2", new LanguageDependedTexts{ { "sk", "rights1sk" }, { "en", "rights1en" } } },
                { "https://creativecommons.org/publicdomain/zero/1.0/", new LanguageDependedTexts{ { "sk", "CC sk" }, { "en", "CC en" } } },
                { "http://publications.europa.eu/resource/authority/licence/CC_BY_4_0", new LanguageDependedTexts{ { "sk", "CC sk" }, { "en", "CC en" } } }
            });
            CreateCodelistFile(DcatDistribution.PersonalDataContainmentTypeCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "https://data.gov.sk/def/personal-data-occurence-type/1", new LanguageDependedTexts{ { "sk", "personal1sk" }, { "en", "personal1en" } } },
                { "https://data.gov.sk/def/personal-data-occurence-type/2", new LanguageDependedTexts{ { "sk", "personal2sk" }, { "en", "personal2en" } } },
            });
            CreateCodelistFile(DcatDistribution.FormatCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "http://publications.europa.eu/resource/dataset/file-type/1", new LanguageDependedTexts{ { "sk", "fileType1sk" }, { "en", "fileType1en" } } },
                { "http://publications.europa.eu/resource/dataset/file-type/2", new LanguageDependedTexts{ { "sk", "fileType2sk" }, { "en", "fileType2en" } } },
                { "http://publications.europa.eu/resource/authority/file-type/CSV", new LanguageDependedTexts{ { "sk", "csv" }, { "en", "csv" } } },
            });
            CreateCodelistFile(DcatDistribution.MediaTypeCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { "http://www.iana.org/assignments/media-types/text/csv", new LanguageDependedTexts{ { "sk", "CSV" }, { "en", "CSV" } } },
                { "http://www.iana.org/assignments/media-types/text/xml", new LanguageDependedTexts{ { "sk", "XML" }, { "en", "XML" } } },
                { "http://www.iana.org/assignments/media-types/application/zip", new LanguageDependedTexts{ { "sk", "ZIP" }, { "en", "ZIP" } } },
                { "http://www.iana.org/assignments/media-types/application/rar", new LanguageDependedTexts{ { "sk", "RAR" }, { "en", "RAR" } } },
            });
        }

        public void CreateLocalCatalogCodelists()
        {
            CreateCodelistFile(DcatCatalog.LocalCatalogTypeCodelist, new Dictionary<string, LanguageDependedTexts>
            {
                { DcatCatalog.LocalCatalogTypeCodelist + "/1", new LanguageDependedTexts{ { "sk", "DCAT Dokumenty" }, { "en", "DCAT Documents" } } },
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
