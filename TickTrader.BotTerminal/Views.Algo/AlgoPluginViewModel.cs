﻿using System;
using System.IO;
using Caliburn.Micro;
using Machinarium.Qnil;
using TickTrader.Algo.Common.Info;
using TickTrader.Algo.Core.Lib;
using TickTrader.Algo.Core.Repository;
using TickTrader.Algo.Core.Metadata;


namespace TickTrader.BotTerminal
{
    internal class AlgoPluginViewModel : PropertyChangedBase
    {
        public const string GroupLevelHeader = nameof(CurrentGroup);
        public const string PackageLevelHeader = nameof(PackageNameWithoutExtension);
        public const string BotLevelHeader = nameof(DisplayName);

        public enum FolderType { Common, Local, Embedded }

        public enum GroupType { Unknown, Indicators, Bots }

        public PluginInfo PluginInfo { get; }

        public PackageInfo PackageInfo { get; }

        public AlgoAgentViewModel Agent { get; }

        public PluginKey Key => PluginInfo.Key;

        public PluginDescriptor Descriptor => PluginInfo.Descriptor;

        public string DisplayName => PluginInfo.Descriptor.UiDisplayName;

        public string PackageDisplayName { get; }

        public string PackageNameWithoutExtension { get; }

        public string FullPackagePath { get; }

        public string Category => PluginInfo.Descriptor.Category;

        public AlgoTypes Type => PluginInfo.Descriptor.Type;

        public FolderType Folder { get; }

        public string Description { get; }

        public GroupType CurrentGroup { get; }

        public bool IsRemote => Agent.Model.IsRemote;

        public bool IsLocal => !Agent.Model.IsRemote;


        public AlgoPluginViewModel(PluginInfo info, AlgoAgentViewModel agent)
        {
            PluginInfo = info;
            Agent = agent;

            PackageDisplayName = PluginInfo.Key.PackageName;

            var packagePath = "Unknown path";

            if (Agent.Model.Packages.Snapshot.TryGetValue(info.Key.GetPackageKey(), out var packageInfo))
            {
                PackageInfo = packageInfo;
                PackageDisplayName = packageInfo.Identity.FileName;
                packagePath = Path.GetDirectoryName(packageInfo.Identity.FilePath);
                FullPackagePath = $"Full path: {packageInfo.Identity.FilePath}";
            }

            PackageNameWithoutExtension = GetPathWithoutExtension(PackageDisplayName);
            Description = string.Join(Environment.NewLine, PluginInfo.Descriptor.Description, string.Empty, $"Package {PackageDisplayName} at {packagePath}").Trim();

            CurrentGroup = (GroupType)Type;

            switch (PluginInfo.Key.PackageLocation)
            {
                case RepositoryLocation.LocalRepository:
                case RepositoryLocation.LocalExtensions:
                    Folder = FolderType.Local;
                    break;
                case RepositoryLocation.Embedded:
                    Folder = FolderType.Embedded;
                    break;
                default:
                    Folder = FolderType.Common;
                    break;
            }
        }

        public void RemovePackage()
        {
            Agent.RemovePackage(PackageInfo.Key).Forget();
        }

        public void UploadPackage()
        {
            Agent.OpenUploadPackageDialog(PackageInfo.Key);
        }

        public void DownloadPackage()
        {
            Agent.OpenDownloadPackageDialog(PackageInfo.Key);
        }

        private string GetPathWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);
    }
}
