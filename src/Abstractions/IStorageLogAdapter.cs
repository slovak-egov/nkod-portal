﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public interface IStorageLogAdapter
    {
        void LogFileCreated(string path);
    }
}
