using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class UserInfoQuery
    {
        public string? Id { get; set; }

        public int? Page { get; set; }

        public int? PageSize { get; set; }
    }
}
