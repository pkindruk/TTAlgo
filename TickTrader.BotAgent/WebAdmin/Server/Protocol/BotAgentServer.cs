﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TickTrader.Algo.Common.Info;
using TickTrader.Algo.Common.Model.Config;
using TickTrader.Algo.Core;
using TickTrader.Algo.Protocol;
using TickTrader.BotAgent.BA;
using TickTrader.BotAgent.BA.Models;
using TickTrader.BotAgent.WebAdmin.Server.Extensions;
using TickTrader.BotAgent.WebAdmin.Server.Models;

namespace TickTrader.BotAgent.WebAdmin.Server.Protocol
{
    public class BotAgentServer : IBotAgentServer
    {
        private static IAlgoCoreLogger _logger = CoreLoggerFactory.GetLogger<BotAgentServer>();
        private static readonly SetupContext _agentContext = new SetupContext();


        private IBotAgent _botAgent;
        private ServerCredentials _serverCreds;


        public event Action<UpdateInfo<PackageInfo>> PackageUpdated = delegate { };
        public event Action<UpdateInfo<AccountModelInfo>> AccountUpdated = delegate { };
        public event Action<UpdateInfo<BotModelInfo>> BotUpdated = delegate { };
        public event Action<PackageInfo> PackageStateUpdated = delegate { };
        public event Action<BotModelInfo> BotStateUpdated = delegate { };
        public event Action<AccountModelInfo> AccountStateUpdated = delegate { };


        public BotAgentServer(IBotAgent botAgent, IConfiguration serverConfig)
        {
            _botAgent = botAgent;
            _serverCreds = serverConfig.GetCredentials();

            if (_serverCreds == null)
                throw new Exception("Server credentials not found");

            _botAgent.AccountChanged += OnAccountChanged;
            _botAgent.BotChanged += OnBotChanged;
            _botAgent.PackageChanged += OnPackageChanged;
            _botAgent.BotStateChanged += OnBotStateChanged;
            _botAgent.AccountStateChanged += OnAccountStateChanged;
        }


        public bool ValidateCreds(string login, string password)
        {
            return _serverCreds.Login == login && _serverCreds.Password == password;
        }

        public List<AccountModelInfo> GetAccountList()
        {
            return _botAgent.GetAccounts();
        }

        public List<BotModelInfo> GetBotList()
        {
            return _botAgent.GetTradeBots();
        }

        public List<PackageInfo> GetPackageList()
        {
            return _botAgent.GetPackages();
        }

        public ApiMetadataInfo GetApiMetadata()
        {
            return ApiMetadataInfo.CreateCurrentMetadata();
        }

        public MappingCollectionInfo GetMappingsInfo()
        {
            return _botAgent.GetMappingsInfo();
        }

        public SetupContextInfo GetSetupContext()
        {
            return new SetupContextInfo(_agentContext.DefaultTimeFrame, new SymbolInfo(_agentContext.DefaultSymbol.Name, _agentContext.DefaultSymbol.Origin), _agentContext.DefaultMapping);
        }

        public AccountMetadataInfo GetAccountMetadata(AccountKey account)
        {
            var error = _botAgent.GetAccountMetadata(account, out var accountMetadata);
            if (error.Code != ConnectionErrorCodes.None)
                throw new Exception($"Account {account.Login} at {account.Server} failed to connect");
            return accountMetadata;
        }

        public void StartBot(string botId)
        {
            _botAgent.StartBot(botId);
        }

        public void StopBot(string botId)
        {
            _botAgent.StopBotAsync(botId);
        }

        public void AddBot(AccountKey account, PluginConfig config)
        {
            _botAgent.AddBot(account, config);
        }

        public void RemoveBot(string botId, bool cleanLog, bool cleanAlgoData)
        {
            _botAgent.RemoveBot(botId, cleanLog, cleanAlgoData);
        }

        public void ChangeBotConfig(string botId, PluginConfig newConfig)
        {
            _botAgent.ChangeBotConfig(botId, newConfig);
        }

        public void AddAccount(AccountKey account, string password, bool useNewProtocol)
        {
            _botAgent.AddAccount(account, password, useNewProtocol);
        }

        public void RemoveAccount(AccountKey account)
        {
            _botAgent.RemoveAccount(account);
        }

        public void ChangeAccount(AccountKey account, string password, bool useNewProtocol)
        {
            _botAgent.ChangeAccount(account, password, useNewProtocol);
        }

        public ConnectionErrorInfo TestAccount(AccountKey account)
        {
            return _botAgent.TestAccount(account);
        }

        public ConnectionErrorInfo TestAccountCreds(AccountKey account, string password, bool useNewProtocol)
        {
            return _botAgent.TestCreds(account, password, useNewProtocol);
        }

        public void UploadPackage(string fileName, byte[] packageBinary)
        {
            _botAgent.UpdatePackage(packageBinary, fileName);
        }

        public void RemovePackage(PackageKey package)
        {
            _botAgent.RemovePackage(package);
        }

        public byte[] DownloadPackage(PackageKey package)
        {
            return _botAgent.DownloadPackage(package);
        }

        public string GetBotStatus(string botId)
        {
            return _botAgent.GetBotLog(botId).Status;
        }

        public LogRecordInfo[] GetBotLogs(string botId, DateTime lastLogTimeUtc, int maxCount)
        {
            return _botAgent.GetBotLog(botId).Messages.Where(e => e.TimeUtc > lastLogTimeUtc)
                .Select(e => new LogRecordInfo
                {
                    TimeUtc = e.TimeUtc,
                    Severity = Convert(e.Type),
                    Message = e.Message,
                }).ToArray();
        }

        public BotFolderInfo GetBotFolderInfo(string botId, BotFolderId folderId)
        {
            var botFolder = GetBotFolder(botId, folderId);

            return new BotFolderInfo
            {
                BotId = botId,
                FolderId = folderId,
                Path = botFolder.Folder,
                Files = botFolder.Files.Select(f => new BotFileInfo { Name = f.Name, Size = f.Size }).ToList(),
            };
        }

        public void ClearBotFolder(string botId, BotFolderId folderId)
        {
            var botFolder = GetBotFolder(botId, folderId);

            botFolder.Clear();
        }

        public void DeleteBotFile(string botId, BotFolderId folderId, string fileName)
        {
            var botFolder = GetBotFolder(botId, folderId);

            botFolder.DeleteFile(fileName);
        }

        public Stream GetBotFile(string botId, BotFolderId folderId, string fileName)
        {
            var botFolder = GetBotFolder(botId, folderId);

            return botFolder.GetFile(fileName).OpenRead();
        }

        public void UploadBotFile(string botId, BotFolderId folderId, string fileName, byte[] fileBinary)
        {
            var botFolder = GetBotFolder(botId, folderId);

            botFolder.SaveFile(fileName, fileBinary);
        }


        private UpdateType Convert(ChangeAction action)
        {
            switch (action)
            {
                case ChangeAction.Added:
                    return UpdateType.Added;
                case ChangeAction.Modified:
                    return UpdateType.Replaced;
                case ChangeAction.Removed:
                    return UpdateType.Removed;
                default:
                    throw new ArgumentException();
            }
        }

        private LogSeverity Convert(LogEntryType entryType)
        {
            switch (entryType)
            {
                case LogEntryType.Info:
                    return LogSeverity.Info;
                case LogEntryType.Error:
                    return LogSeverity.Error;
                case LogEntryType.Trading:
                    return LogSeverity.Trade;
                case LogEntryType.TradingSuccess:
                    return LogSeverity.TradeSuccess;
                case LogEntryType.TradingFail:
                    return LogSeverity.TradeFail;
                case LogEntryType.Custom:
                    return LogSeverity.Custom;
                default:
                    throw new ArgumentException();
            }
        }

        private IBotFolder GetBotFolder(string botId, BotFolderId folderId)
        {
            switch (folderId)
            {
                case BotFolderId.AlgoData:
                    return _botAgent.GetAlgoData(botId);
                case BotFolderId.BotLogs:
                    return _botAgent.GetBotLog(botId);
                default:
                    throw new ArgumentException();
            }
        }

        #region Event handlers

        private void OnAccountChanged(AccountModelInfo account, ChangeAction action)
        {
            try
            {
                AccountUpdated(new UpdateInfo<AccountModelInfo>
                {
                    Type = Convert(action),
                    Value = account,
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to send update: {ex.Message}", ex);
            }
        }

        private void OnBotChanged(BotModelInfo bot, ChangeAction action)
        {
            try
            {
                BotUpdated(new UpdateInfo<BotModelInfo>
                {
                    Type = Convert(action),
                    Value = bot,
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to send update: {ex.Message}", ex);
            }
        }

        private void OnPackageChanged(PackageInfo package, ChangeAction action)
        {
            try
            {
                PackageUpdated(new UpdateInfo<PackageInfo>
                {
                    Type = Convert(action),
                    Value = package,
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to send update: {ex.Message}", ex);
            }
        }

        private void OnBotStateChanged(BotModelInfo bot)
        {
            try
            {
                BotStateUpdated(bot);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to send update: {ex.Message}", ex);
            }
        }

        private void OnAccountStateChanged(AccountModelInfo account)
        {
            try
            {
                AccountStateUpdated(account);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to send update: {ex.Message}", ex);
            }
        }

        private void OnPackageStateChanged(PackageInfo package)
        {
            try
            {
                PackageStateUpdated(package);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to send update: {ex.Message}", ex);
            }
        }

        #endregion Event handlers
    }
}
