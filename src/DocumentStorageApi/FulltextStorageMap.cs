﻿using NkodSk.Abstractions;
using NkodSk.RdfFulltextIndex;
using System.IO;

namespace DocumentStorageApi
{
    public class FulltextStorageMap : FulltextIndex
    {
        public FulltextStorageMap(IFileStorage fileStorage, ILanguagesSource languages) : base(languages)
        {
            Initialize(fileStorage);
        }
    }
}
