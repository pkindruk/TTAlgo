﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TickTrader.Algo.AppCommon.Update
{
    public static class UpdateHelper
    {
        public const string UpdaterFileName = "TickTrader.Algo.Updater.exe";
        public const string TerminalFileName = "TickTrader.AlgoTerminal.exe";
        public const string ServerFileName = "TickTrader.AlgoServer.exe";
        public const string StateFileName = "update-state.json";
        public const string LogFileName = "update.log";
        public const int UpdateFailTimeout = 1_000;
        public const int ShutdownTimeout = 30_000;
        public const int UpdateHistoryMaxRecords = 5;


        public static string GetAppExeFileName(UpdateAppTypes appType)
        {
            return appType switch
            {
                UpdateAppTypes.Terminal => TerminalFileName,
                UpdateAppTypes.Server => ServerFileName,
                _ => string.Empty
            };
        }

        public static string GetUpdateBinFolder(string updatePath) => Path.Combine(updatePath, "update");


        public static async Task<bool> StartUpdate(string updateWorkDir, UpdateParams updateParams)
        {
            CreateUpdateHistoryRecord(updateWorkDir);

            var updateState = new UpdateState { Params = updateParams };
            SaveUpdateState(updateWorkDir, updateState);

            var updaterExe = Path.Combine(updateParams.UpdatePath, UpdaterFileName);
            var startInfo = new ProcessStartInfo(updaterExe) { UseShellExecute = false, WorkingDirectory = updateWorkDir };

            var proc = Process.Start(startInfo);
            await Task.WhenAny(Task.Delay(UpdateFailTimeout), proc.WaitForExitAsync()); // wait in case of any issues with update

            if (proc.HasExited)
                return false;

            return true;
        }

        public static UpdateState LoadUpdateState(string workDir)
        {
            var statePath = Path.Combine(workDir, StateFileName);
            return JsonSerializer.Deserialize<UpdateState>(statePath);
        }

        public static void SaveUpdateState(string workDir, UpdateState state)
        {
            var statePath = Path.Combine(workDir, StateFileName);
            File.WriteAllText(statePath, JsonSerializer.Serialize(state));
        }


        private static void CreateUpdateHistoryRecord(string updateWorkDir)
        {
            var statePath = Path.Combine(updateWorkDir, StateFileName);
            var logPath = Path.Combine(updateWorkDir, LogFileName);

            if (File.Exists(statePath))
            {
                var files = Directory.GetFiles(updateWorkDir, "UpdateHistory*.zip");
                if (files.Length >= UpdateHistoryMaxRecords)
                {
                    // Cleanup old files
                    files = files.OrderBy(f => File.GetCreationTimeUtc(f)).ToArray();
                    for (var i = 0; files.Length + i >= UpdateHistoryMaxRecords; i++)
                    {
                        try
                        {
                            File.Delete(files[i]);
                        }
                        catch (Exception) { }
                    }
                }

                var historyFilePath = Path.Combine(updateWorkDir, $"UpdateHistory-{File.GetCreationTimeUtc(statePath):yyyy-MM-dd-hh-mm-ss}.zip");
                using (var archiveStream = new FileStream(historyFilePath, FileMode.Create))
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(statePath, StateFileName);
                    File.Delete(statePath);

                    if (File.Exists(logPath))
                    {
                        archive.CreateEntryFromFile(logPath, LogFileName);
                        File.Delete(logPath);
                    }
                }
            }
        }
    }
}
