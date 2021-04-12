﻿namespace TickTrader.Algo.Domain.ServerControl
{
    public partial class DownloadPluginFileRequest
    {
        public DownloadPluginFileRequest(string pluginId, PluginFolderInfo.Types.PluginFolderId folderId, string fileName)
            : this(pluginId, folderId, fileName, FileTransferSettings.Default)
        {
        }

        public DownloadPluginFileRequest(string pluginId, PluginFolderInfo.Types.PluginFolderId folderId, string fileName, FileTransferSettings transferSettings)
        {
            PluginId = pluginId;
            FolderId = folderId;
            FileName = fileName;
            TransferSettings = transferSettings;
        }
    }
}
