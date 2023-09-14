using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.RdfFileStorage
{
    class FileStreamWrap : FileStream
    {
        public event EventHandler? Disposed;

        public FileStreamWrap(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode, access, share)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Disposed?.Invoke(this, EventArgs.Empty);
        }
    }
}
