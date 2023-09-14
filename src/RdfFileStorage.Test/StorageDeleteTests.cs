using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnauthorizedAccessException = NkodSk.RdfFileStorage.UnauthorizedAccessException;

namespace RdfFileStorage.Test
{
    public class StorageDeleteTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture fixture;

        public StorageDeleteTests(StorageFixture fixture)
        {
            this.fixture = fixture;
        }
        
        private static string GetFilePath(Storage storage, FileMetadata metadata)
        {
            MethodInfo getFilePathMethod = typeof(Storage).GetMethod("GetFilePath", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (string)getFilePathMethod.Invoke(storage, new object[] { metadata })!;
        }

        [Fact]
        public void NonExistingFileShouldNotBeDeleted()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            Assert.Throws<Exception>(() => storage.DeleteFile(Guid.NewGuid(), StaticAccessPolicy.Allow));
        }

        [Fact]
        public void PublicFileShouldBeDeleted()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            Guid id = fixture.PublicFile.Metadata.Id;

            storage.DeleteFile(id, StaticAccessPolicy.Allow);

            Assert.Null(storage.GetFileState(id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void NonPublicFileShouldBeDeleted()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            Guid id = fixture.NonPublicFile.Metadata.Id;

            storage.DeleteFile(id, StaticAccessPolicy.Allow);

            Assert.Null(storage.GetFileState(id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void DeletedPublicFileShouldNotBeDeleted()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            Guid id = fixture.PublicFile.Metadata.Id;

            storage.DeleteFile(id, StaticAccessPolicy.Allow);

            Assert.Throws<Exception>(() => storage.DeleteFile(id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void DeletedNonPublicFileShouldNotBeDeleted()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            Guid id = fixture.NonPublicFile.Metadata.Id;

            storage.DeleteFile(id, StaticAccessPolicy.Allow);

            Assert.Throws<Exception>(() => storage.DeleteFile(id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void PublicFileShouldBeDeletedWithChild()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata parentMetadata = fixture.PublicFile.Metadata;
            FileMetadata childMetadata1 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata2 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata3 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            storage.InsertFile("c1", childMetadata1, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c2", childMetadata2, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c3", childMetadata3, false, StaticAccessPolicy.Allow);

            storage.DeleteFile(parentMetadata.Id, StaticAccessPolicy.Allow);

            Assert.Null(storage.GetFileState(parentMetadata.Id, StaticAccessPolicy.Allow));
            Assert.Null(storage.GetFileState(childMetadata1.Id, StaticAccessPolicy.Allow));
            Assert.Null(storage.GetFileState(childMetadata2.Id, StaticAccessPolicy.Allow));
            Assert.Null(storage.GetFileState(childMetadata3.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void NonPublicFileShouldBeDeletedWithChild()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata parentMetadata = fixture.NonPublicFile.Metadata;
            FileMetadata childMetadata1 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata2 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata3 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            storage.InsertFile("c1", childMetadata1, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c2", childMetadata2, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c3", childMetadata3, false, StaticAccessPolicy.Allow);

            storage.DeleteFile(parentMetadata.Id, StaticAccessPolicy.Allow);

            Assert.Null(storage.GetFileState(parentMetadata.Id, StaticAccessPolicy.Allow));
            Assert.Null(storage.GetFileState(childMetadata1.Id, StaticAccessPolicy.Allow));
            Assert.Null(storage.GetFileState(childMetadata2.Id, StaticAccessPolicy.Allow));
            Assert.Null(storage.GetFileState(childMetadata3.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void PublicFileShouldBeDeletedWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata;
            Guid id = metadata.Id;

            using (Stream? stream = storage.OpenReadStream(id, StaticAccessPolicy.Allow))
            {
                Assert.NotNull(stream);

                storage.DeleteFile(id, StaticAccessPolicy.Allow);

                Assert.Null(storage.GetFileState(id, StaticAccessPolicy.Allow));
                Assert.True(File.Exists(GetFilePath(storage, metadata)));
            }

            Assert.False(File.Exists(GetFilePath(storage, metadata)));
        }

        [Fact]
        public void NonPublicFileShouldBeDeletedWhileReadingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata;
            Guid id = metadata.Id;

            using (Stream? stream = storage.OpenReadStream(id, StaticAccessPolicy.Allow))
            {
                Assert.NotNull(stream);

                storage.DeleteFile(id, StaticAccessPolicy.Allow);

                Assert.Null(storage.GetFileState(id, StaticAccessPolicy.Allow));
                Assert.True(File.Exists(GetFilePath(storage, metadata)));
            }

            Assert.False(File.Exists(GetFilePath(storage, metadata)));
        }

        [Fact]
        public void PublicFileShouldNotBeDeletedWhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.PublicFile.Metadata;

            using (Stream stream = storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Allow))
            {
                storage.DeleteFile(metadata.Id, StaticAccessPolicy.Allow);

                Assert.Null(storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow));
                Assert.True(File.Exists(GetFilePath(storage, metadata)));
            }

            Assert.False(File.Exists(GetFilePath(storage, metadata)));
        }

        [Fact]
        public void NonPublicFileShouldNotBeDeletedWhileWritingStream()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            FileMetadata metadata = fixture.NonPublicFile.Metadata;

            using (Stream stream = storage.OpenWriteStream(metadata, true, StaticAccessPolicy.Allow))
            {
                storage.DeleteFile(metadata.Id, StaticAccessPolicy.Allow);

                Assert.Null(storage.GetFileState(metadata.Id, StaticAccessPolicy.Allow));
                Assert.True(File.Exists(GetFilePath(storage, metadata)));
            }

            Assert.False(File.Exists(GetFilePath(storage, metadata)));
        }

        [Fact]
        public void PublicFileShouldNotBeDeletedWithoutPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            Guid id = fixture.PublicFile.Metadata.Id;

            Assert.Throws<UnauthorizedAccessException>(() => storage.DeleteFile(id, StaticAccessPolicy.Deny));

            Assert.NotNull(storage.GetFileState(id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void NonPublicFileShouldBeDeletedWithoutPermission()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            Guid id = fixture.NonPublicFile.Metadata.Id;

            Assert.Throws<UnauthorizedAccessException>(() => storage.DeleteFile(id, StaticAccessPolicy.Deny));

            Assert.NotNull(storage.GetFileState(id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void PublicFileShouldBeDeletedWhileReadingStreamOfChild()
        {
            Storage storage = new Storage(fixture.GetStoragePath());
            
            FileMetadata parentMetadata = fixture.PublicFile.Metadata;
            FileMetadata childMetadata1 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata2 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata3 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            storage.InsertFile("c1", childMetadata1, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c2", childMetadata2, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c3", childMetadata3, false, StaticAccessPolicy.Allow);

            using (Stream? stream = storage.OpenReadStream(childMetadata2.Id, StaticAccessPolicy.Allow))
            {
                Assert.NotNull(stream);

                storage.DeleteFile(parentMetadata.Id, StaticAccessPolicy.Allow);

                Assert.Null(storage.GetFileState(parentMetadata.Id, StaticAccessPolicy.Allow));
                Assert.Null(storage.GetFileState(childMetadata1.Id, StaticAccessPolicy.Allow));
                Assert.Null(storage.GetFileState(childMetadata2.Id, StaticAccessPolicy.Allow));
                Assert.Null(storage.GetFileState(childMetadata3.Id, StaticAccessPolicy.Allow));

                Assert.True(File.Exists(GetFilePath(storage, childMetadata2)));
            }

            Assert.False(File.Exists(GetFilePath(storage, childMetadata2)));
        }

        [Fact]
        public void NonPublicFileShouldBeDeletedWhileReadingStreamOfChild()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileMetadata parentMetadata = fixture.NonPublicFile.Metadata;
            FileMetadata childMetadata1 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata2 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata3 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            storage.InsertFile("c1", childMetadata1, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c2", childMetadata2, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c3", childMetadata3, false, StaticAccessPolicy.Allow);

            using (Stream? stream = storage.OpenReadStream(childMetadata2.Id, StaticAccessPolicy.Allow))
            {
                Assert.NotNull(stream);

                storage.DeleteFile(parentMetadata.Id, StaticAccessPolicy.Allow);

                Assert.Null(storage.GetFileState(parentMetadata.Id, StaticAccessPolicy.Allow));
                Assert.Null(storage.GetFileState(childMetadata1.Id, StaticAccessPolicy.Allow));
                Assert.Null(storage.GetFileState(childMetadata2.Id, StaticAccessPolicy.Allow));
                Assert.Null(storage.GetFileState(childMetadata3.Id, StaticAccessPolicy.Allow));

                Assert.True(File.Exists(GetFilePath(storage, childMetadata2)));
            }

            Assert.False(File.Exists(GetFilePath(storage, childMetadata2)));
        }

        [Fact]
        public void PublicFileShouldBeDeletedWhileWritingStreamOfChild()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileMetadata parentMetadata = fixture.PublicFile.Metadata;
            FileMetadata childMetadata1 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata2 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata3 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            storage.InsertFile("c1", childMetadata1, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c2", childMetadata2, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c3", childMetadata3, false, StaticAccessPolicy.Allow);

            using (Stream stream = storage.OpenWriteStream(childMetadata2, true, StaticAccessPolicy.Allow))
            {
                storage.DeleteFile(parentMetadata.Id, StaticAccessPolicy.Allow);

                Assert.Null(storage.GetFileState(parentMetadata.Id, StaticAccessPolicy.Allow));
                Assert.Null(storage.GetFileState(childMetadata1.Id, StaticAccessPolicy.Allow));
                Assert.Null(storage.GetFileState(childMetadata2.Id, StaticAccessPolicy.Allow));
                Assert.Null(storage.GetFileState(childMetadata3.Id, StaticAccessPolicy.Allow));

                Assert.True(File.Exists(GetFilePath(storage, childMetadata2)));
            }

            Assert.False(File.Exists(GetFilePath(storage, childMetadata2)));
        }

        [Fact]
        public void NonPublicFileShouldBeDeletedWhileWritingStreamOfChild()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileMetadata parentMetadata = fixture.NonPublicFile.Metadata;
            FileMetadata childMetadata1 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata2 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata3 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            storage.InsertFile("c1", childMetadata1, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c2", childMetadata2, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c3", childMetadata3, false, StaticAccessPolicy.Allow);

            using (Stream stream = storage.OpenWriteStream(childMetadata2, true, StaticAccessPolicy.Allow))
            {
                storage.DeleteFile(parentMetadata.Id, StaticAccessPolicy.Allow);

                Assert.Null(storage.GetFileState(parentMetadata.Id, StaticAccessPolicy.Allow));
                Assert.Null(storage.GetFileState(childMetadata1.Id, StaticAccessPolicy.Allow));
                Assert.Null(storage.GetFileState(childMetadata2.Id, StaticAccessPolicy.Allow));
                Assert.Null(storage.GetFileState(childMetadata3.Id, StaticAccessPolicy.Allow));

                Assert.True(File.Exists(GetFilePath(storage, childMetadata2)));
            }

            Assert.False(File.Exists(GetFilePath(storage, childMetadata2)));
        }

        [Fact]
        public void PublicFileShouldNotBeDeletedWithoutPermissionOfChild()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileMetadata parentMetadata = fixture.PublicFile.Metadata;
            FileMetadata childMetadata1 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata2 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata3 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            storage.InsertFile("c1", childMetadata1, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c2", childMetadata2, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c3", childMetadata3, false, StaticAccessPolicy.Allow);

            Assert.Throws<UnauthorizedAccessException>(() => storage.DeleteFile(parentMetadata.Id, new DynamicAccessPolicy(f => f.Id != childMetadata2.Id)));

            Assert.NotNull(storage.GetFileState(parentMetadata.Id, StaticAccessPolicy.Allow));
            Assert.NotNull(storage.GetFileState(childMetadata1.Id, StaticAccessPolicy.Allow));
            Assert.NotNull(storage.GetFileState(childMetadata2.Id, StaticAccessPolicy.Allow));
            Assert.NotNull(storage.GetFileState(childMetadata3.Id, StaticAccessPolicy.Allow));
        }

        [Fact]
        public void NonPublicFileShouldBeDeletedWithoutPermissionOfChild()
        {
            Storage storage = new Storage(fixture.GetStoragePath());

            FileMetadata parentMetadata = fixture.NonPublicFile.Metadata;
            FileMetadata childMetadata1 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata2 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            FileMetadata childMetadata3 = new FileMetadata(Guid.NewGuid(), "Test", FileType.DistributionRegistration, parentMetadata.Id, parentMetadata.Publisher, false, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            storage.InsertFile("c1", childMetadata1, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c2", childMetadata2, false, StaticAccessPolicy.Allow);
            storage.InsertFile("c3", childMetadata3, false, StaticAccessPolicy.Allow);

            Assert.Throws<UnauthorizedAccessException>(() => storage.DeleteFile(parentMetadata.Id, new DynamicAccessPolicy(f => f.Id != childMetadata2.Id)));

            Assert.NotNull(storage.GetFileState(parentMetadata.Id, StaticAccessPolicy.Allow));
            Assert.NotNull(storage.GetFileState(childMetadata1.Id, StaticAccessPolicy.Allow));
            Assert.NotNull(storage.GetFileState(childMetadata2.Id, StaticAccessPolicy.Allow));
            Assert.NotNull(storage.GetFileState(childMetadata3.Id, StaticAccessPolicy.Allow));
        }
    }
}
