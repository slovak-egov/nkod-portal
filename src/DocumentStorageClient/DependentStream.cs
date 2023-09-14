using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentStorageClient
{
    public class DependentStream : Stream
    {
        private readonly Stream source;

        private readonly HttpResponseMessage response;

        public DependentStream(Stream source, HttpResponseMessage response)
        {
            this.source = source;
            this.response = response;
        }

        public override bool CanRead => source.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => source.Length;

        public override long Position { get => source.Position; set => source.Position = value; }

        public override void Flush()
        {
            source.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) => source.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => source.Seek(offset, origin);   

        public override void SetLength(long value) => source.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            source.Dispose();
            response.Dispose();
        }
    }
}
