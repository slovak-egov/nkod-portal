using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.RdfFileStorage
{
    public interface IOrderProvider
    {
        List<string> GetOrder(bool reverseOrder);
    }
}
