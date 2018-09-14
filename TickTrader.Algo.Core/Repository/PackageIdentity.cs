﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TickTrader.Algo.Core.Repository
{
    public class PackageIdentity
    {
        public string FileName { get; set; }

        public string FilePath { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime LastModifiedUtc { get; set; }

        public long Size { get; set; }

        public string Hash { get; set; }

        public bool IsValid => Hash.Length == 64;


        public PackageIdentity() { }

        public PackageIdentity(string fileName, string filePath, DateTime createdUtc, DateTime lastModifiedUtc, long size, string hash)
        {
            FileName = fileName;
            FilePath = filePath;
            CreatedUtc = createdUtc;
            LastModifiedUtc = lastModifiedUtc;
            Size = size;
            Hash = hash;
        }


        public static PackageIdentity Create(FileInfo info)
        {
            byte[] hashBytes;
            using (var file = info.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                hashBytes = SHA256.Create().ComputeHash(file);
            }
            var sb = new StringBuilder();
            foreach (var b in hashBytes)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return new PackageIdentity(info.Name, info.FullName, info.CreationTimeUtc, info.LastWriteTimeUtc, info.Length, sb.ToString());
        }

        public static PackageIdentity CreateInvalid(FileInfo info)
        {
            return new PackageIdentity(info.Name, info.FullName, info.CreationTimeUtc, info.LastWriteTimeUtc, info.Length, "");
        }

        public static PackageIdentity CreateInvalid(string fileName, string filePath)
        {
            return new PackageIdentity(fileName, filePath, DateTime.UtcNow, DateTime.UtcNow, -1, "");
        }
    }
}
