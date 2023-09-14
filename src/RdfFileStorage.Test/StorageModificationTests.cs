using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnauthorizedAccessException = NkodSk.RdfFileStorage.UnauthorizedAccessException;

namespace RdfFileStorage.Test
{
    public class StorageModificationTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public StorageModificationTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void NewPublicFileShouldBeInsertedWithContent()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";

            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);

        }

        [Fact]
        public void NewNonPublicFileShouldBeInsertedWithContent()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";

            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void NewPublicFileShouldBeInsertedWithStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            using (Stream stream = storage.OpenWriteStream(metadata, false, StaticAccessPolicy.Allow))
            {
                stream.Write(bytes);
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);

        }

        [Fact]
        public void NewNonPublicFileShouldBeInsertedWithStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            using (Stream stream = storage.OpenWriteStream(metadata, false, StaticAccessPolicy.Allow))
            {
                stream.Write(bytes);
            }                

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void NewPublicFileShouldNotBeInsertedWithContentWithoutPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";

            Assert.Throws<UnauthorizedAccessException>(() => storage.InsertFile(content, metadata, false, StaticAccessPolicy.Deny));

            FileState ? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Null(state);

        }

        [Fact]
        public void NewNonPublicFileShouldNotBeInsertedWithContentWithoutPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";

            Assert.Throws<UnauthorizedAccessException>(() => storage.InsertFile(content, metadata, false, StaticAccessPolicy.Deny));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Null(state);
        }

        [Fact]
        public void NewPublicFileShouldNotBeInsertedWithStreamWithoutPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            Assert.Throws<UnauthorizedAccessException>(() => storage.OpenWriteStream(metadata, false, StaticAccessPolicy.Deny));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Null(state);

        }

        [Fact]
        public void NewNonPublicFileShouldNotBeInsertedWithStreamWithoutPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            Assert.Throws<UnauthorizedAccessException>(() => storage.OpenWriteStream(metadata, false, StaticAccessPolicy.Deny));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Null(state);
        }

        [Fact]
        public void PublicFileShouldBeOverwrittenWithContent()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            
            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);

        }

        [Fact]
        public void NonPublicFileShouldBeOverwrittenWithContent()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";

            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void PublicFileShouldBeOverwrittenWithStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            using (Stream stream = storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Allow))
            {
                stream.Write(bytes);
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);

        }

        [Fact]
        public void NonPublicFileShouldBeOverwrittenWithStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            using (Stream stream = storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Allow))
            {
                stream.Write(bytes);
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void PublicFileShouldBeOverwrittenWithMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(existingState.Content, state.Content);
            Assert.Equal(metadata, state.Metadata);

        }

        [Fact]
        public void NonPublicFileShouldBeOverwrittenWithMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(existingState.Content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithContent()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";

            Assert.Throws<Exception>(() => storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);

        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithContent()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";

            Assert.Throws<Exception>(() => storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            Assert.Throws<Exception>(() => storage.OpenWriteStream(metadata, false, StaticAccessPolicy.Allow));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);

        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            Assert.Throws<Exception>(() => storage.OpenWriteStream(metadata, false, StaticAccessPolicy.Allow));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithContentWithoutPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";

            Assert.Throws<UnauthorizedAccessException>(() => storage.InsertFile(content, metadata, true, StaticAccessPolicy.Deny));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithContentWithoutPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            Assert.Throws<UnauthorizedAccessException>(() => storage.InsertFile(content, metadata, true, StaticAccessPolicy.Deny));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithStreamWithoutPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            Assert.Throws<UnauthorizedAccessException>(() => storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Deny));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithStreamWithoutPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            Assert.Throws<UnauthorizedAccessException>(() => storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Deny));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithMetadataWithoutPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            Assert.Throws<UnauthorizedAccessException>(() => storage.UpdateMetadata(metadata, StaticAccessPolicy.Deny));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithMetadataWithoutPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            Assert.Throws<UnauthorizedAccessException>(() => storage.UpdateMetadata(metadata, StaticAccessPolicy.Deny));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithContentWithoutOriginalPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";

            Assert.Throws<UnauthorizedAccessException>(() => storage.InsertFile(content, metadata, true, new DynamicAccessPolicy(m => m.Publisher == metadata.Publisher)));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithContentWithoutOriginalPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            Assert.Throws<UnauthorizedAccessException>(() => storage.InsertFile(content, metadata, true, new DynamicAccessPolicy(m => m.Publisher == metadata.Publisher)));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithStreamWithoutOriginalPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            Assert.Throws<UnauthorizedAccessException>(() => storage.OpenWriteStream(metadata, true, new DynamicAccessPolicy(m => m.Publisher == metadata.Publisher)));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithStreamWithoutOriginalPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            Assert.Throws<UnauthorizedAccessException>(() => storage.OpenWriteStream(metadata, true, new DynamicAccessPolicy(m => m.Publisher == metadata.Publisher)));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithMetadataWithoutOriginalPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            Assert.Throws<UnauthorizedAccessException>(() => storage.UpdateMetadata(metadata, new DynamicAccessPolicy(m => m.Publisher == metadata.Publisher)));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithMetadataWithoutOriginalPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            Assert.Throws<UnauthorizedAccessException>(() => storage.UpdateMetadata(metadata, new DynamicAccessPolicy(m => m.Publisher == metadata.Publisher)));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithContentWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);

            Assert.Throws<Exception>(() => storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithContentWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);

            Assert.Throws<Exception>(() => storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithStreamWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);

            Assert.Throws<Exception>(() => storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Allow));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithStreamWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            using Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(stream);

            Assert.Throws<Exception>(() => storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Allow));

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.Equal(existingState, state);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithMetadataWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            using (Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow))
            {
                Assert.NotNull(stream);

                storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);
            }
            
            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(existingState.Content, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithMetadataWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            using (Stream? stream = storage.OpenReadStream(metadata.Id, StaticAccessPolicy.Allow))
            {
                Assert.NotNull(stream);

                storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(metadata, state.Metadata);
            Assert.Equal(existingState.Content, state.Content);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithContentWhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";

            using (Stream stream = storage.OpenWriteStream(existingState.Metadata, true, StaticAccessPolicy.Allow))
            {
                Assert.Throws<Exception>(() => storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow));
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(existingState.Metadata, state.Metadata);
            Assert.Equal(string.Empty, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithContentWhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";

            using (Stream stream = storage.OpenWriteStream(existingState.Metadata, true, StaticAccessPolicy.Allow))
            {
                Assert.Throws<Exception>(() => storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow));
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(existingState.Metadata, state.Metadata);
            Assert.Equal(string.Empty, state.Content);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithStreamWhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            using (Stream stream = storage.OpenWriteStream(existingState.Metadata, true, StaticAccessPolicy.Allow))
            {
                Assert.Throws<Exception>(() => storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Allow));
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(existingState.Metadata, state.Metadata);
            Assert.Equal(string.Empty, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithStreamWhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            using (Stream stream = storage.OpenWriteStream(existingState.Metadata, true, StaticAccessPolicy.Allow))
            {
                Assert.Throws<Exception>(() => storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Allow));
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(existingState.Metadata, state.Metadata);
            Assert.Equal(string.Empty, state.Content);
        }

        [Fact]
        public void PublicFileShouldNotBeOverwrittenWithMetadataWhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.PublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            using (Stream stream = storage.OpenWriteStream(existingState.Metadata, true, StaticAccessPolicy.Allow))
            {
                Assert.Throws<Exception>(() => storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow));
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(existingState.Metadata, state.Metadata);
            Assert.Equal(string.Empty, state.Content);
        }

        [Fact]
        public void NonPublicFileShouldNotBeOverwrittenWithMetadataWhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileState existingState = fixture.NonPublicFile;
            FileMetadata metadata = existingState.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            using (Stream stream = storage.OpenWriteStream(existingState.Metadata, true, StaticAccessPolicy.Allow))
            {
                Assert.Throws<Exception>(() => storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow));
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(existingState.Metadata, state.Metadata);
            Assert.Equal(string.Empty, state.Content);
        }

        [Fact]
        public void PublicFileShouldBeWrittenAfterDeleteWithContent()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";

            storage.DeleteFile(metadata.Id, StaticAccessPolicy.Allow);

            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void NonPublicFileShouldBeWrittenAfterDeleteWithContent()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";

            storage.DeleteFile(metadata.Id, StaticAccessPolicy.Allow);

            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void PublicFileShouldBeWrittenAfterDeleteWithStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            storage.DeleteFile(metadata.Id, StaticAccessPolicy.Allow);

            using (Stream stream = storage.OpenWriteStream(metadata, false, StaticAccessPolicy.Allow))
            {
                stream.Write(bytes);
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void NonPublicFileShouldBeWrittenAfterDeleteWithStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            string content = "content";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            storage.DeleteFile(metadata.Id, StaticAccessPolicy.Allow);

            using (Stream stream = storage.OpenWriteStream(metadata, false, StaticAccessPolicy.Allow))
            {
                stream.Write(bytes);
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void PublicFileShoulBeOverwrittenAfterInsertWithContent()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";

            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            metadata = fixture.PublicFile.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            content = "content2";

            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void NonPublicFileShouldBeWrittenAfterInsertWithContent()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";

            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            metadata = fixture.PublicFile.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            content = "content2";

            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void PublicFileShouldBeWrittenAfterInsertWithStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";

            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            metadata = fixture.PublicFile.Metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            content = "content2";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            using (Stream stream = storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Allow))
            {
                stream.Write(bytes);
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void NonPublicFileShouldBeWrittenAfterInsertWithStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";

            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            metadata = fixture.PublicFile.Metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };
            content = "content2";
            byte[] bytes = Encoding.UTF8.GetBytes(content);

            storage.InsertFile(content, metadata, true, StaticAccessPolicy.Allow);

            using (Stream stream = storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Allow))
            {
                stream.Write(bytes);
            }

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void PublicFileShoulBeOverwrittenAfterInsertWithMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";

            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            metadata = metadata with { Publisher = "test1", IsPublic = false, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }

        [Fact]
        public void NonPublicFileShouldBeWrittenAfterInsertWithMetadata()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.DatasetRegistration, null, null, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            string content = "content";

            storage.InsertFile(content, metadata, false, StaticAccessPolicy.Allow);

            metadata = metadata with { Publisher = "test1", IsPublic = true, OriginalFileName = "test1.ttf", Type = FileType.LocalCatalogRegistration, Created = DateTimeOffset.UtcNow, LastModified = DateTimeOffset.UtcNow };

            storage.UpdateMetadata(metadata, StaticAccessPolicy.Allow);

            FileState? state = storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow);
            Assert.NotNull(state);
            Assert.Equal(content, state.Content);
            Assert.Equal(metadata, state.Metadata);
        }
    }
}
