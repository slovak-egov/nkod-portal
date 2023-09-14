using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfFileStorage.Test
{
    public class StorageReadTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public StorageReadTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void NonExistingFileShouldNotBeReadAsState()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            Assert.Null(storage.GetFileState(Guid.NewGuid(), StaticAccessPolicy.Allow));
        }

        [Fact]
        public void NonExistingFileShouldNotBeReadAsStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            Assert.Null(storage.OpenReadStream(Guid.NewGuid(), StaticAccessPolicy.Allow));
        }

        [Fact]
        public void PersistentPublicFileShoulBeReadAsState()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;

            FileState? state = storage.GetFileState(expectedState.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(expectedState, state);
        }

        [Fact]
        public void PersistentPublicFileShoulBeReadAsStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;

            using Stream? stream = storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(expectedState.Content, reader.ReadToEnd());
        }

        [Fact]
        public void PersistentPublicFileShoulBeReadAsList()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(expectedState, response.Files);
        }

        [Fact]
        public void PersistentNonPublicFileShoulBeReadAsState()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;

            FileState? state = storage.GetFileState(expectedState.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(expectedState, state);
        }

        [Fact]
        public void PersistentNonPublicFileShoulBeReadAsStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;

            using Stream? stream = storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(expectedState.Content, reader.ReadToEnd());
        }

        [Fact]
        public void PersistentNonPublicFileShoulBeReadAsList()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(expectedState, response.Files);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStateWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;
            using Stream? stream = storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(expectedState.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(expectedState, state);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStreamWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;
            using Stream? stream = storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow);

            using (Stream? secordStream = storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow))
            {
                Assert.NotNull(secordStream);
                using (StreamReader reader = new StreamReader(secordStream))
                {
                    Assert.Equal(expectedState.Content, reader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void PublicFileShouldBeReadAsListWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;
            using Stream? stream = storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(expectedState, response.Files);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStateWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;
            using Stream? stream = storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(expectedState.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(expectedState, state);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStreamWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;
            using Stream? stream = storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(expectedState.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(expectedState, state);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsListWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;
            using Stream? stream = storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(expectedState, response.Files);
        }

        [Fact]
        public void PublicFileShouldNotBeReadAsStateWhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;

            FileState? state;
            using (Stream stream = storage.OpenWriteStream(expectedState.Metadata, true, StaticAccessPolicy.Allow))
            {
                state = storage.GetFileState(expectedState.Metadata.Id, StaticAccessPolicy.Allow);
                Assert.Null(state);
            }

            state = storage.GetFileState(expectedState.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(expectedState.Metadata, state.Metadata);
            Assert.Equal(string.Empty, state.Content);
        }

        [Fact]
        public void PublicFileShouldNotBeReadAsStreamWhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;

            using (Stream stream = storage.OpenWriteStream(expectedState.Metadata, true, StaticAccessPolicy.Allow))
            {
                Assert.Null(storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow));
            }

            using (Stream? secordStream = storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow))
            {
                Assert.NotNull(secordStream);
                using (StreamReader reader = new StreamReader(secordStream))
                {
                    Assert.Equal(string.Empty, reader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void PublicFileShouldNotBeReadAsListhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;

            FileStorageResponse response;
            using (Stream stream = storage.OpenWriteStream(expectedState.Metadata, true, StaticAccessPolicy.Allow))
            {
                response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
                Assert.DoesNotContain(response.Files, f => f.Metadata.Id == expectedState.Metadata.Id);
            }

            response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(expectedState.Metadata, string.Empty), response.Files);
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsStateWhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;

            FileState? state;
            using (Stream stream = storage.OpenWriteStream(expectedState.Metadata, true, StaticAccessPolicy.Allow))
            {
                state = storage.GetFileState(expectedState.Metadata.Id, StaticAccessPolicy.Allow);
                Assert.Null(state);
            }

            state = storage.GetFileState(expectedState.Metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(expectedState.Metadata, state.Metadata);
            Assert.Equal(string.Empty, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsStreamWhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;

            using (Stream stream = storage.OpenWriteStream(expectedState.Metadata, true, StaticAccessPolicy.Allow))
            {
                Assert.Null(storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow));
            }

            using (Stream? secordStream = storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow))
            {
                Assert.NotNull(secordStream);
                using (StreamReader reader = new StreamReader(secordStream))
                {
                    Assert.Equal(string.Empty, reader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsListWhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;

            FileStorageResponse response;
            using (Stream stream = storage.OpenWriteStream(expectedState.Metadata, true, StaticAccessPolicy.Allow))
            {
                response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
                Assert.DoesNotContain(response.Files, f => f.Metadata.Id == expectedState.Metadata.Id);
            }

            response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(expectedState.Metadata, string.Empty), response.Files);
        }

        [Fact]
        public void PublicFileShouldNotBeReadAsStateWhileWritingStreamAsNewFile()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            using (Stream stream = storage.OpenWriteStream(metadata, false, StaticAccessPolicy.Allow))
            {
                Assert.Null(storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow));
            }

            Assert.Equal(new FileState(metadata, string.Empty), storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void PublicFileShouldNotBeReadAsStreamWhileWritingStreamAsNewFile()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            using (Stream stream = storage.OpenWriteStream(metadata, false, StaticAccessPolicy.Allow))
            {
                Assert.Null(storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow));
            }

            using (Stream? secordStream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow))
            {
                Assert.NotNull(secordStream);
                using (StreamReader reader = new StreamReader(secordStream))
                {
                    Assert.Equal(string.Empty, reader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void PublicFileShouldNotBeReadAsListWhileWritingStreamAsNewFile()
        {
            Storage storage = new Storage(fixture.GetStoragePath());            
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            FileStorageResponse response;
            using (Stream stream = storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Allow))
            {
                response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
                Assert.DoesNotContain(response.Files, f => f.Metadata.Id == metadata.Id);
            }

            response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, string.Empty), response.Files);
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsStateWhileWritingStreamAsNewFile()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            using (Stream stream = storage.OpenWriteStream(metadata, false, StaticAccessPolicy.Allow))
            {
                Assert.Null(storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow));
            }

            Assert.Equal(new FileState(metadata, string.Empty), storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsStreamWhileWritingStreamAsNewFile()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            using (Stream stream = storage.OpenWriteStream(metadata, false, StaticAccessPolicy.Allow))
            {
                Assert.Null(storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow));
            }

            using (Stream? secordStream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow))
            {
                Assert.NotNull(secordStream);
                using (StreamReader reader = new StreamReader(secordStream))
                {
                    Assert.Equal(string.Empty, reader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsListWhileWritingStreamAsNewFile()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            FileStorageResponse response;
            using (Stream stream = storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Allow))
            {
                response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
                Assert.DoesNotContain(response.Files, f => f.Metadata.Id == metadata.Id);
            }

            response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, string.Empty), response.Files);
        }

        [Fact]
        public void PublicFileShouldNotBeReadAsStateAfterDelete()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;
            storage.DeleteFile(expectedState.Metadata.Id, StaticAccessPolicy.Allow);

            Assert.Null(storage.GetFileState(expectedState.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void PublicFileShouldNotBeReadAsStreamAfterDelete()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;
            storage.DeleteFile(expectedState.Metadata.Id, StaticAccessPolicy.Allow);

            Assert.Null(storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void PublicFileShouldNotBeReadAsListAfterDelete()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;
            storage.DeleteFile(expectedState.Metadata.Id, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.DoesNotContain(response.Files, f => f.Metadata.Id == expectedState.Metadata.Id);
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsStateAfterDelete()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;
            storage.DeleteFile(expectedState.Metadata.Id, StaticAccessPolicy.Allow);

            Assert.Null(storage.GetFileState(expectedState.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsStreamAfterDelete()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;
            storage.DeleteFile(expectedState.Metadata.Id, StaticAccessPolicy.Allow);

            Assert.Null(storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsListAfterDelete()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;
            storage.DeleteFile(expectedState.Metadata.Id, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.DoesNotContain(response.Files, f => f.Metadata.Id == expectedState.Metadata.Id);
        }

        [Fact]
        public void PublicFileShouldNotBeReadAsStateAfterFreshDelete()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            storage.DeleteFile(metadata.Id, StaticAccessPolicy.Allow);

            Assert.Null(storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void PublicFileShouldNotBeReadAsStreamAfterFreshDelete()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            storage.DeleteFile(metadata.Id, StaticAccessPolicy.Allow);

            Assert.Null(storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void PublicFileShouldNotBeReadAsListAfterFreshDelete()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            storage.DeleteFile(metadata.Id, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.DoesNotContain(response.Files, f => f.Metadata.Id == metadata.Id);
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsStateAfterFreshDelete()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            storage.DeleteFile(metadata.Id, StaticAccessPolicy.Allow);

            Assert.Null(storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsStreamAfterFreshDelete()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            storage.DeleteFile(metadata.Id, StaticAccessPolicy.Allow);

            Assert.Null(storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsListAfterFreshDelete()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            storage.DeleteFile(metadata.Id, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.DoesNotContain(response.Files, f => f.Metadata.Id == metadata.Id);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStateAfterFreshInsert()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStreamAfterFreshInsert()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void PublicFileShouldBeReadAsListAfterFreshInsert()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStateAfterFreshInsert()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStreamAfterFreshInsert()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsListAfterFreshInsert()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStateAfterFreshModify()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content + "-old", metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { ParentFile = null, Publisher = "Pub2", OriginalFileName = "test2.ttl", Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStreamAfterFreshModify()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content + "-old", metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { ParentFile = null, Publisher = "Pub2", OriginalFileName = "test2.ttl", Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void PublicFileShouldBeReadAsListAfterFreshModify()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content + "-old", metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { ParentFile = null, Publisher = "Pub2", OriginalFileName = "test2.ttl", Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStateAfterFreshModify()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content + "-old", metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { ParentFile = null, Publisher = "Pub2", OriginalFileName = "test2.ttl", Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStreamAfterFreshModify()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content + "-old", metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { ParentFile = null, Publisher = "Pub2", OriginalFileName = "test2.ttl", Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsListAfterFreshModify()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content + "-old", metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { ParentFile = null, Publisher = "Pub2", OriginalFileName = "test2.ttl", Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStateAfterFreshModifyFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content + "-old", metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { IsPublic = true };
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStreamAfterFreshModifyFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content + "-old", metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { IsPublic = true };
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void PublicFileShouldBeReadAsListAfterFreshModifyFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content + "-old", metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { IsPublic = true };
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStateAfterFreshModifyFromPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content + "-old", metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { IsPublic = false };
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStreamAfterFreshModifyFromPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content + "-old", metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { IsPublic = false };
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsListAfterFreshModifyFromPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content + "-old", metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { IsPublic = false };
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStateAfterModify()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { Publisher = "Pub1", Type = FileType.DatasetRegistration, ParentFile = fixture.NonPublicFile.Metadata.Id, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "test";
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStreamAfterModify()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { Publisher = "Pub1", Type = FileType.DatasetRegistration, ParentFile = fixture.NonPublicFile.Metadata.Id, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "test";
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void PublicFileShouldBeReadAsListAfterModify()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { Publisher = "Pub1", Type = FileType.DatasetRegistration, ParentFile = fixture.NonPublicFile.Metadata.Id, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "test";
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStateAfterModify()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { Publisher = "Pub1", Type = FileType.DatasetRegistration, ParentFile = fixture.PublicFile.Metadata.Id, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "test";
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStreamAfterModify()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { Publisher = "Pub1", Type = FileType.DatasetRegistration, ParentFile = fixture.PublicFile.Metadata.Id, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "test";
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsListAfterModify()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { Publisher = "Pub1", Type = FileType.DatasetRegistration, ParentFile = fixture.PublicFile.Metadata.Id, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "test";
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStateAfterModifyFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { IsPublic = true };
            string content = "test";
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStreamAfterModifyFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { IsPublic = true };
            string content = "test";
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void PublicFileShouldBeReadAsListAfterModifyFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { IsPublic = true };
            string content = "test";
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStateAfterModifyFromPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { IsPublic = false };
            string content = "test";
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStreamAfterModifyFromPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { IsPublic = false };
            string content = "test";
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsListAfterModifyFromPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { IsPublic = false };
            string content = "test";
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStateAfterFreshModifyMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { ParentFile = null, Publisher = "Pub2", OriginalFileName = "test2.ttl", Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStreamAfterFreshModifyMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { ParentFile = null, Publisher = "Pub2", OriginalFileName = "test2.ttl", Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void PublicFileShouldBeReadAsListAfterFreshModifyMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { ParentFile = null, Publisher = "Pub2", OriginalFileName = "test2.ttl", Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStateAfterFreshModifyMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { ParentFile = null, Publisher = "Pub2", OriginalFileName = "test2.ttl", Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStreamAfterFreshModifyMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { ParentFile = null, Publisher = "Pub2", OriginalFileName = "test2.ttl", Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsListAfterFreshModifyMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { ParentFile = null, Publisher = "Pub2", OriginalFileName = "test2.ttl", Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStateAfterModifyMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { Publisher = "Pub1", Type = FileType.DatasetRegistration, ParentFile = fixture.NonPublicFile.Metadata.Id, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = fixture.PublicFile.Content ?? string.Empty;
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStreamAfterModifyMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { Publisher = "Pub1", Type = FileType.DatasetRegistration, ParentFile = fixture.NonPublicFile.Metadata.Id, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = fixture.PublicFile.Content ?? string.Empty;
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void PublicFileShouldBeReadAsListAfterModifyMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { Publisher = "Pub1", Type = FileType.DatasetRegistration, ParentFile = fixture.NonPublicFile.Metadata.Id, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = fixture.PublicFile.Content ?? string.Empty;
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStateAfterModifyMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { Publisher = "Pub1", Type = FileType.DatasetRegistration, ParentFile = fixture.PublicFile.Metadata.Id, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = fixture.NonPublicFile.Content ?? string.Empty;
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStreamAfterModifyMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { Publisher = "Pub1", Type = FileType.DatasetRegistration, ParentFile = fixture.PublicFile.Metadata.Id, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = fixture.NonPublicFile.Content ?? string.Empty;
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsListAfterModifyMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { Publisher = "Pub1", Type = FileType.DatasetRegistration, ParentFile = fixture.PublicFile.Metadata.Id, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = fixture.NonPublicFile.Content ?? string.Empty;
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStateAfterModifyMetadataFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { IsPublic = true };
            string content = fixture.NonPublicFile.Content ?? string.Empty;
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStreamAfterModifyMetadataFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { IsPublic = true };
            string content = fixture.NonPublicFile.Content ?? string.Empty;
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void PublicFileShouldBeReadAsListAfterModifyMetadataFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { IsPublic = true };
            string content = fixture.NonPublicFile.Content ?? string.Empty;
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStateAfterModifyMetadataFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { IsPublic = false };
            string content = fixture.PublicFile.Content ?? string.Empty;
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStreamAfterModifyMetadataFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { IsPublic = false };
            string content = fixture.PublicFile.Content ?? string.Empty;
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsListAfterModifyMetadataFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { IsPublic = false };
            string content = fixture.PublicFile.Content ?? string.Empty;
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStateAfterModifyMetadataFreshFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { IsPublic = true };
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStreamAfterModifyMetadataFreshFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { IsPublic = true };
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void PublicFileShouldBeReadAsListAfterModifyMetadataFreshFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", false, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { IsPublic = true };
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStateAfterModifyMetadataFreshFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { IsPublic = false };
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(content, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsStreamAfterModifyMetadataFreshFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { IsPublic = false };
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(content, reader.ReadToEnd());
        }

        [Fact]
        public void NonPublicFileShouldBeReadAsListAfterModifyMetadataFreshFromNonPublic()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, fixture.PublicFile.Metadata.ParentFile, "Pub1", true, "test.ttl", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "test";
            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);
            metadata = metadata with { IsPublic = false };
            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Allow);
            Assert.Contains(new FileState(metadata, content), response.Files);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStateEventWithDenyPolicy()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;

            FileState? state = storage.GetFileState(expectedState.Metadata.Id, StaticAccessPolicy.Deny);
            Assert.NotNull(state);
            Assert.Equal(expectedState, state);
        }

        [Fact]
        public void PublicFileShouldBeReadAsStreamEventWithDenyPolicy()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;

            using Stream? stream = storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Deny);
            Assert.NotNull(stream);
            using StreamReader reader = new StreamReader(stream);
            Assert.Equal(expectedState.Content, reader.ReadToEnd());
        }

        [Fact]
        public void PublicFileShouldBeReadAsListEventWithDenyPolicy()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.PublicFile;

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Deny);
            Assert.Contains(expectedState, response.Files);
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsStateEventWithDenyPolicy()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;

            Assert.Null(storage.GetFileState(expectedState.Metadata.Id, StaticAccessPolicy.Deny));
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsStreamEventWithDenyPolicy()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;

            Assert.Null(storage.OpenReadStream(expectedState.Metadata.Id, StaticAccessPolicy.Deny));
        }

        [Fact]
        public void NonPublicFileShouldNotBeReadAsListEventWithDenyPolicy()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState expectedState = fixture.NonPublicFile;

            FileStorageResponse response = storage.GetFileStates(new FileStorageQuery(), StaticAccessPolicy.Deny);
            Assert.DoesNotContain(response.Files, f => f.Metadata.Id == expectedState.Metadata.Id);
        }

        [Fact]
        public void AllFilesShouldBeReturnedInList()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery();
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates
                .OrderByDescending(f => f.Metadata.LastModified)
                .ToList();

            Assert.Equal(expectedFiles.Count, response.TotalCount);
            Assert.Equal(expectedFiles.Count, response.Files.Count);
            for (int i = 0; i < expectedFiles.Count; i++)
            {
                Assert.Equal(expectedFiles[i], response.Files[i]);
            }
        }

        [Fact]
        public void FilesShouldBeFilteredByPublisher()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            string publisher = fixture.PublicFile.Metadata.Publisher!;

            FileStorageQuery query = new FileStorageQuery { OnlyPublishers = new List<string> { publisher } };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates
                .Where(f => f.Metadata.Publisher == publisher)
                .OrderByDescending(f => f.Metadata.LastModified)
                .ToList();

            Assert.Equal(expectedFiles.Count, response.TotalCount);
            Assert.Equal(expectedFiles.Count, response.Files.Count);
            for (int i = 0; i < expectedFiles.Count; i++)
            {
                Assert.Equal(expectedFiles[i], response.Files[i]);
            }
        }

        [Fact]
        public void FilesShouldBeFilteredByParentFile()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            Guid parentId = fixture.PublicFile.Metadata.Id;

            FileStorageQuery query = new FileStorageQuery { ParentFile = parentId };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates
                .Where(f => f.Metadata.ParentFile == parentId)
                .OrderByDescending(f => f.Metadata.LastModified)
                .ToList();

            Assert.Equal(expectedFiles.Count, response.TotalCount);
            Assert.Equal(expectedFiles.Count, response.Files.Count);
            for (int i = 0; i < expectedFiles.Count; i++)
            {
                Assert.Equal(expectedFiles[i], response.Files[i]);
            }
        }

        [Fact]
        public void FilesShouldBeFilteredBySingleType()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileType fileType = fixture.NonPublicFile.Metadata.Type;

            FileStorageQuery query = new FileStorageQuery { OnlyTypes = new List<FileType> { fileType } };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates
                .Where(f => f.Metadata.Type == fileType)
                .OrderByDescending(f => f.Metadata.LastModified)
                .ToList();

            Assert.Equal(expectedFiles.Count, response.TotalCount);
            Assert.Equal(expectedFiles.Count, response.Files.Count);
            for (int i = 0; i < expectedFiles.Count; i++)
            {
                Assert.Equal(expectedFiles[i], response.Files[i]);
            }
        }

        [Fact]
        public void FilesShouldBeFilteredByMultiType()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileType fileType1 = fixture.PublicFile.Metadata.Type;
            FileType fileType2 = fixture.NonPublicFile.Metadata.Type;

            FileStorageQuery query = new FileStorageQuery { OnlyTypes = new List<FileType> { fileType1, fileType2 } };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates
                .Where(f => f.Metadata.Type == fileType1 || f.Metadata.Type == fileType2)
                .OrderByDescending(f => f.Metadata.LastModified)
                .ToList();

            Assert.Equal(expectedFiles.Count, response.TotalCount);
            Assert.Equal(expectedFiles.Count, response.Files.Count);
            for (int i = 0; i < expectedFiles.Count; i++)
            {
                Assert.Equal(expectedFiles[i], response.Files[i]);
            }
        }

        [Fact]
        public void FilesShouldBeFilteredByPublished()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery { OnlyPublished = true };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates
                .Where(f => f.Metadata.IsPublic)
                .OrderByDescending(f => f.Metadata.LastModified)
                .ToList();

            Assert.Equal(expectedFiles.Count, response.TotalCount);
            Assert.Equal(expectedFiles.Count, response.Files.Count);
            for (int i = 0; i < expectedFiles.Count; i++)
            {
                Assert.Equal(expectedFiles[i], response.Files[i]);
            }
        }

        [Fact]
        public void FilesShouldBeFilteredByIds()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery { OnlyIds = fixture.ExistingStates.Skip(1).Select(s => s.Metadata.Id).ToList() };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates
                .Skip(1)
                .OrderByDescending(f => f.Metadata.LastModified)
                .ToList();

            Assert.Equal(expectedFiles.Count, response.TotalCount);
            Assert.Equal(expectedFiles, response.Files);
        }

        [Fact]
        public void FilesShouldBeOrderedByCreatedAsc()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery
            {
                OrderDefinitions = new List<FileStorageOrderDefinition> { new FileStorageOrderDefinition(FileStorageOrderProperty.Created, false) }
            };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates
                .OrderBy(f => f.Metadata.Created)
                .ThenByDescending(f => f.Metadata.LastModified)
                .ToList();

            Assert.Equal(expectedFiles.Count, response.TotalCount);
            Assert.Equal(expectedFiles, response.Files);
        }

        [Fact]
        public void FilesShouldBeOrderedByCreatedDesc()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery
            {
                OrderDefinitions = new List<FileStorageOrderDefinition> { new FileStorageOrderDefinition(FileStorageOrderProperty.Created, true) }
            };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates
                .OrderByDescending(f => f.Metadata.Created)
                .ThenByDescending(f => f.Metadata.LastModified)
                .ToList();

            Assert.Equal(expectedFiles.Count, response.TotalCount);
            Assert.Equal(expectedFiles, response.Files);
        }

        [Fact]
        public void FilesShouldBeOrderedByLastModifiedAsc()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery
            {
                OrderDefinitions = new List<FileStorageOrderDefinition> { new FileStorageOrderDefinition(FileStorageOrderProperty.LastModified, false) }
            };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates
                .OrderBy(f => f.Metadata.LastModified)
                .ToList();

            Assert.Equal(expectedFiles.Count, response.TotalCount);
            Assert.Equal(expectedFiles, response.Files);
        }

        [Fact]
        public void FilesShouldBeOrderedByLastModifiedDesc()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery
            {
                OrderDefinitions = new List<FileStorageOrderDefinition> { new FileStorageOrderDefinition(FileStorageOrderProperty.LastModified, true) }
            };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates
                .OrderByDescending(f => f.Metadata.LastModified)
                .ToList();

            Assert.Equal(expectedFiles.Count, response.TotalCount);
            Assert.Equal(expectedFiles, response.Files);
        }

        [Fact]
        public void FilesShouldBeOrderedByNameAsc()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery
            {
                OrderDefinitions = new List<FileStorageOrderDefinition> {
                    new FileStorageOrderDefinition(FileStorageOrderProperty.Name, false),
                    new FileStorageOrderDefinition(FileStorageOrderProperty.LastModified, true)
                }
            };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates
                .OrderBy(f => f.Metadata.Name.GetText("sk"), StringComparer.CurrentCultureIgnoreCase)
                .ThenByDescending(f => f.Metadata.LastModified)
                .ToList();

            Assert.Equal(expectedFiles.Count, response.TotalCount);
            Assert.Equal(expectedFiles, response.Files);
        }

        [Fact]
        public void FilesShouldBeOrderedByNameDesc()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery
            {
                OrderDefinitions = new List<FileStorageOrderDefinition>
                {
                    new FileStorageOrderDefinition(FileStorageOrderProperty.Name, true),
                    new FileStorageOrderDefinition(FileStorageOrderProperty.LastModified, true)
                }
            };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates
                .OrderByDescending(f => f.Metadata.Name.GetText("sk"), StringComparer.CurrentCultureIgnoreCase)
                .ThenByDescending(f => f.Metadata.LastModified)
                .ToList();

            Assert.Equal(expectedFiles.Count, response.TotalCount);
            Assert.Equal(expectedFiles, response.Files);
        }

        [Fact]
        public void FilesShouldBeSkipped()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery { SkipResults = 1 };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates.OrderByDescending(f => f.Metadata.LastModified).Skip(1).ToList();

            Assert.Equal(fixture.ExistingStates.Length, response.TotalCount);
            Assert.Equal(expectedFiles, response.Files);
        }

        [Fact]
        public void AllFilesShouldBeSkippedWithSkipResults()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery { SkipResults = 3 };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates.OrderByDescending(f => f.Metadata.LastModified).Skip(1).ToList();

            Assert.Equal(fixture.ExistingStates.Length, response.TotalCount);
            Assert.Empty(response.Files);
        }

        [Fact]
        public void AllilesShouldBeSkippedWithMaxValue()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery { SkipResults = int.MaxValue };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates.OrderByDescending(f => f.Metadata.LastModified).Skip(1).ToList();

            Assert.Equal(fixture.ExistingStates.Length, response.TotalCount);
            Assert.Empty(response.Files);
        }

        [Fact]
        public void FilesShouldBeLimitedWitxMaxResults()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery { MaxResults = 2 };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates.OrderByDescending(f => f.Metadata.LastModified).Take(2).ToList();

            Assert.Equal(fixture.ExistingStates.Length, response.TotalCount);
            Assert.Equal(expectedFiles, response.Files);
        }

        [Fact]
        public void NoFileShouldBeInResultWithMaxResultsZero()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery { MaxResults = 0 };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            Assert.Equal(fixture.ExistingStates.Length, response.TotalCount);
            Assert.Empty(response.Files);
        }

        [Fact]
        public void AllFilesShouldBeInResultWithMaxResultsMaxValue()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery { MaxResults = int.MaxValue };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates.OrderByDescending(f => f.Metadata.LastModified).ToList();

            Assert.Equal(fixture.ExistingStates.Length, response.TotalCount);
            Assert.Equal(expectedFiles, response.Files);
        }

        [Fact]
        public void FilesShouldBeLimitedByPage()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery { MaxResults = 1, SkipResults = 1 };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            List<FileState> expectedFiles = fixture.ExistingStates.OrderByDescending(f => f.Metadata.LastModified).Skip(1).Take(1).ToList();

            Assert.Equal(fixture.ExistingStates.Length, response.TotalCount);
            Assert.Equal(expectedFiles, response.Files);
        }

        [Fact]
        public void FilesShouldNotHaveIncludedChilds()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery { IncludeDependentFiles = false };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            Assert.All(response.Files, f => Assert.Null(f.DependentFiles));
        }

        [Fact]
        public void FilesShouldHaveIncludedChilds()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery { IncludeDependentFiles = true };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            FileState[] expectedFiles = fixture.ExistingStates.ToArray();
            expectedFiles[0] = expectedFiles[0] with { DependentFiles = new[] { expectedFiles[1] } };
            expectedFiles[1] = expectedFiles[1] with { DependentFiles = new FileState[0] };
            expectedFiles[2] = expectedFiles[2] with { DependentFiles = new FileState[0] };

            Array.Sort(expectedFiles, (a, b) => -a.Metadata.LastModified.CompareTo(b.Metadata.LastModified));

            Assert.Equal(fixture.ExistingStates.Length, response.TotalCount);
            Assert.Equal(expectedFiles.Length, response.Files.Count);
            for (int i = 0; i < expectedFiles.Length; i++)
            {
                Assert.Equal(expectedFiles[i].Metadata, response.Files[i].Metadata);
                Assert.Equal(expectedFiles[i].Content, response.Files[i].Content);
                Assert.Equal(expectedFiles[i].DependentFiles, response.Files[i].DependentFiles);
            }
        }

        [Fact]
        public void FilesShouldBeGroupedByPublisher()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileStorageQuery query = new FileStorageQuery
            {
                OrderDefinitions = new List<FileStorageOrderDefinition>()
            };
            FileStorageGroupResponse response;
            List<FileStorageGroup> expectedGroups = fixture.ExistingStates.Where(f => !string.IsNullOrEmpty(f.Metadata.Publisher)).GroupBy(f => f.Metadata.Publisher).Select(g => new FileStorageGroup(g.Key!, 
                storage.GetFileStates(new FileStorageQuery { OnlyTypes = new List<FileType> { FileType.PublisherRegistration }, OnlyPublishers = new List<string> { g.Key! } }, StaticAccessPolicy.Allow).Files.FirstOrDefault(), 
                g.Count(), new Dictionary<string, int>())).ToList();

            expectedGroups.Sort((a, b) => -a.Count.CompareTo(b.Count));
            response = storage.GetFileStatesByPublisher(query, StaticAccessPolicy.Allow);
            Assert.Equal(expectedGroups.Count, response.TotalCount);
            Assert.Equal(expectedGroups, response.Groups);

            query.OrderDefinitions.Clear();
            query.OrderDefinitions.Add(new FileStorageOrderDefinition(FileStorageOrderProperty.Revelance, false));
            response = storage.GetFileStatesByPublisher(query, StaticAccessPolicy.Allow);
            Assert.Equal(expectedGroups.Count, response.TotalCount);
            Assert.Equal(expectedGroups, response.Groups);

            expectedGroups.Sort((a, b) => a.Count.CompareTo(b.Count));
            query.OrderDefinitions.Clear();
            query.OrderDefinitions.Add(new FileStorageOrderDefinition(FileStorageOrderProperty.Revelance, true));
            response = storage.GetFileStatesByPublisher(query, StaticAccessPolicy.Allow);
            Assert.Equal(expectedGroups.Count, response.TotalCount);
            Assert.Equal(expectedGroups, response.Groups);
        }

        [Fact]
        public void FilesShouldBeFilteredByOneAdditionalFilter()
        {
            Storage storage = new Storage(fixture.GetStoragePath(insertContent: false));

            string content = "test";
            FileMetadata metadata1 = new FileMetadata(Guid.NewGuid(), "Test1", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata metadata2 = new FileMetadata(Guid.NewGuid(), "Test2", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new Dictionary<string, string[]>
            {
                { "test", new[] {"test1" } }
            });
            FileMetadata metadata3 = new FileMetadata(Guid.NewGuid(), "Test3", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new Dictionary<string, string[]>
            {
                { "test", new[] {"test2" } }
            });
            FileMetadata metadata4 = new FileMetadata(Guid.NewGuid(), "Test4", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new Dictionary<string, string[]>
            {
                { "test2", new[] {"test1" } }
            });

            storage.InsertFile(content, metadata1, false, StaticAccessPolicy.Allow);
            storage.InsertFile(content, metadata2, false, StaticAccessPolicy.Allow);
            storage.InsertFile(content, metadata3, false, StaticAccessPolicy.Allow);
            storage.InsertFile(content, metadata4, false, StaticAccessPolicy.Allow);

            FileStorageQuery query = new FileStorageQuery
            {
                AdditionalFilters = new Dictionary<string, string[]>
                {
                    { "test", new[] { "test1" } }
                }
            };
            FileStorageResponse response = storage.GetFileStates(query, StaticAccessPolicy.Allow);

            Assert.Equal(1, response.TotalCount);
            Assert.Equal(new[] { new FileState(metadata2, content) }, response.Files);
        }
    }
}
