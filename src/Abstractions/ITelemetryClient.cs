using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public interface ITelemetryClient
    {
        void TrackException(Exception exception);
    }
}
