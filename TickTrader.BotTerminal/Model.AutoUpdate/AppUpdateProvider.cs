﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TickTrader.Algo.AppCommon.Update;

namespace TickTrader.BotTerminal.Model.AutoUpdate
{
    internal enum UpdateAssetTypes
    {
        Setup = 0,
        TerminalUpdate = 1,
        ServerUpdate = 2,
    }

    internal class AppUpdateEntry
    {
        public string SrcId { get; set; }

        public string VersionId { get; set; }

        public UpdateInfo Info { get; set; }

        public List<UpdateAssetTypes> AvailableAssets { get; set; }
    }

    internal interface IAppUpdateProvider
    {
        Task<List<AppUpdateEntry>> GetUpdates();

        Task Download(string versionId, UpdateAssetTypes assetType, string dstPath);
    }


    internal static class AppUpdateProvider
    {
        public static IAppUpdateProvider Create(UpdateDownloadSource updateSrc)
        {
            var uri = updateSrc.Uri;

            if (uri.StartsWith("https://github.com"))
                return new GithubAppUpdateProvider(updateSrc);

            if (Directory.Exists(uri))
                return new DirectoryAppUpdateProvider(updateSrc);

            throw new NotSupportedException("Uri is not recognised as supported");
        }
    }
}
