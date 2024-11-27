﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public record FileMetadata(Guid Id, LanguageDependedTexts Name, FileType Type, Guid? ParentFile, string? Publisher, bool IsPublic, string? OriginalFileName, DateTimeOffset Created, DateTimeOffset LastModified, Dictionary<string, string[]>? AdditionalValues = null)
    {
        public static FileMetadata LoadFrom(string path)
        {
            if (File.Exists(path))
            {
                return JsonConvert.DeserializeObject<FileMetadata>(File.ReadAllText(path))
                    ?? throw new Exception($"Unable to read metadata content from file {path}");
            }
            else
            {
                throw new Exception($"Unable to read metadata content from file {path} because file does not exist");
            }
        }

        public bool IsHarvested => AdditionalValues is not null && AdditionalValues.TryGetValue("Harvested", out string[]? harvestedValues) && harvestedValues is not null && harvestedValues.Length == 1 && harvestedValues[0] == "true";

        public void SaveTo(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }

        public ContentDisposition CreateAttachmentHeader(string language)
        {
            string originalFileName = OriginalFileName ?? Name.GetText(language) ?? Id.ToString();
            StringBuilder fileNameBuilder = new StringBuilder(originalFileName.Length);
            for (int i = 0; i < originalFileName.Length; i++)
            {
                Char c = originalFileName[i];
                if (c >= 0x7f || c < 0x20)
                {
                    c = '_';
                }
                fileNameBuilder.Append(c);
            }

            return new ContentDisposition
            {
                DispositionType = "attachment",
                FileName = fileNameBuilder.ToString()
            };
        }
    }
}
