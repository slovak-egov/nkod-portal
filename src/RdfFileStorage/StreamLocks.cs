using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.RdfFileStorage
{
    class StreamLocks : IDisposable
    {
        private readonly string path;

        private bool deleteFileOnClose;

        private int disposed;

        private readonly object lockObject = new object();

        private HashSet<FileStreamWrap>? streams;

        public StreamLocks(string path)
        {
            this.path = path;
        }

        public bool IsOpen
        {
            get
            {
                lock (lockObject)
                {
                    return streams != null && streams.Count > 0;
                }
            }
        }

        public bool IsOpenExclusively
        {
            get
            {
                lock (lockObject)
                {
                    return streams != null && streams.Any(s => s.CanWrite);
                }
            }
        }

        public void DeleteFile()
        {
            lock (lockObject)
            {
                deleteFileOnClose = true;

                if (streams == null || streams.Count == 0)
                {
                    File.Delete(path);
                    Dispose();
                }
            }
        }

        private void OnStreamDispose(FileStreamWrap stream)
        {
            lock (lockObject)
            {
                if (streams != null)
                {
                    streams.Remove(stream);

                    if (deleteFileOnClose && streams.Count == 0)
                    {
                        File.Delete(path);
                        Dispose();
                    }
                }
            }
        }

        private FileStreamWrap OpenStream(FileMode mode, FileAccess access, FileShare share)
        {
            if (deleteFileOnClose)
            {
                throw new Exception("File is marked for deletion.");
            }

            if (disposed != 0)
            {
                throw new ObjectDisposedException("StreamLock is already disposed");
            }

            FileStreamWrap stream = new FileStreamWrap(path, mode, access, share);
            streams ??= new HashSet<FileStreamWrap>();
            streams.Add(stream);
            stream.Disposed += (s, e) =>
            {
                OnStreamDispose(stream);
            };
            return stream;
        }

        public FileStreamWrap? OpenReadStream()
        {
            lock (lockObject)
            {
                streams ??= new HashSet<FileStreamWrap>();

                if (streams.Count == 0 || streams.All(s => !s.CanWrite))
                {
                    return OpenStream(FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                else
                {
                    return null;
                }
            }
        }

        public FileStreamWrap? OpenWriteStream()
        {
            lock (lockObject)
            {
                streams ??= new HashSet<FileStreamWrap>();

                if (streams.Count == 0)
                {
                    return OpenStream(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                else
                {
                    return null;
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                lock (lockObject)
                {
                    if (streams != null)
                    {
                        foreach (FileStreamWrap stream in streams)
                        {
                            stream.Dispose();
                        }
                    }
                }
            }
        }
    }
}
