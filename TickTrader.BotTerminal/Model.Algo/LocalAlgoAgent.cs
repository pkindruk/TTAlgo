﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Machinarium.Qnil;
using TickTrader.Algo.Common.Info;
using TickTrader.Algo.Common.Model;
using TickTrader.Algo.Common.Model.Config;
using TickTrader.Algo.Common.Model.Library;
using TickTrader.Algo.Common.Model.Setup;

namespace TickTrader.BotTerminal
{
    internal class LocalAlgoAgent : IAlgoAgent, IAlgoSetupMetadata
    {
        private static readonly ApiMetadataInfo _apiMetadata = ApiMetadataInfo.CreateCurrentMetadata();

        private readonly MappingCollectionInfo _mappingsInfo;
        private ISyncContext _syncContext;
        private VarDictionary<PackageKey, PackageInfo> _packages;
        private VarDictionary<PluginKey, PluginInfo> _plugins;
        private VarDictionary<AccountKey, AccountModelInfo> _accounts;


        public string Name => "Bot Terminal";

        public IVarSet<PackageKey, PackageInfo> Packages => _packages;

        public IVarSet<PluginKey, PluginInfo> Plugins => _plugins;

        public IVarSet<AccountKey, AccountModelInfo> Accounts => _accounts;

        public IVarSet<string, BotModelInfo> Bots { get; }

        public PluginCatalog Catalog { get; }

        public IPluginIdProvider IdProvider => AlgoEnv.IdProvider;



        public AlgoEnvironment AlgoEnv { get; }

        public IAlgoLibrary Library => AlgoEnv.Library;

        public TraderClientModel ClientModel { get; }

        public BotManager BotManager { get; }


        public event Action<PackageInfo> PackageStateChanged;

        public event Action<AccountModelInfo> AccountStateChanged;

        public event Action<BotModelInfo> BotStateChanged;


        public LocalAlgoAgent(AlgoEnvironment algoEnv, TraderClientModel clientModel, BotManager botManager)
        {
            AlgoEnv = algoEnv;
            ClientModel = clientModel;
            BotManager = botManager;

            _syncContext = new DispatcherSync();
            _packages = new VarDictionary<PackageKey, PackageInfo>();
            _plugins = new VarDictionary<PluginKey, PluginInfo>();
            _accounts = new VarDictionary<AccountKey, AccountModelInfo>();
            Bots = botManager.Bots.Select((k, v) => v.ToInfo());
            Catalog = new PluginCatalog(this);
            _mappingsInfo = AlgoEnv.Mappings.ToInfo();

            Library.PackageUpdated += LibraryOnPackageUpdated;
            Library.PluginUpdated += LibraryOnPluginUpdated;
            Library.PackageStateChanged += OnPackageStateChanged;
            Library.Reset += LibraryOnReset;
            ClientModel.Connection.StateChanged += ClientConnectionOnStateChanged;
            BotManager.StateChanged += OnBotStateChanged;
        }


        public Task<SetupMetadata> GetSetupMetadata(AccountKey account, SetupContextInfo setupContext)
        {
            var accountMetadata = new AccountMetadataInfo(new AccountKey(ClientModel.Connection.CurrentServer,
                ClientModel.Connection.CurrentLogin), ClientModel.ObservableSymbolList.Select(s => new SymbolInfo(s.Name)).ToList());
            var res = new SetupMetadata(_apiMetadata, _mappingsInfo, accountMetadata, setupContext ?? BotManager.GetSetupContextInfo());
            return Task.FromResult(res);
        }

        public Task StartBot(string instanceId)
        {
            //BotManager.StartBot(instanceId);
            return Task.FromResult(this);
        }

        public Task StopBot(string instanceId)
        {
            //BotManager.StopBot(instanceId);
            return Task.FromResult(this);
        }

        public Task AddBot(AccountKey account, PluginConfig config)
        {
            //BotManager.AddBot(account, config);
            return Task.FromResult(this);
        }

        public Task RemoveBot(string instanceId, bool cleanLog = false, bool cleanAlgoData = false)
        {
            BotManager.RemoveBot(instanceId);
            return Task.FromResult(this);
        }

        public Task ChangeBotConfig(string instanceId, PluginConfig newConfig)
        {
            BotManager.ChangeBotConfig(instanceId, newConfig);
            return Task.FromResult(this);
        }

        public Task AddAccount(AccountKey account, string password, bool useNewProtocol)
        {
            throw new NotSupportedException();
        }

        public Task RemoveAccount(AccountKey account)
        {
            throw new NotSupportedException();
        }

        public Task ChangeAccount(AccountKey account, string password, bool useNewProtocol)
        {
            throw new NotSupportedException();
        }

        public Task<ConnectionErrorInfo> TestAccount(AccountKey account)
        {
            throw new NotSupportedException();
        }

        public Task<ConnectionErrorInfo> TestAccountCreds(AccountKey account, string password, bool useNewProtocol)
        {
            throw new NotSupportedException();
        }

        public Task UploadPackage(string fileName, byte[] packageBinary)
        {
            //_protocolClient.UploadPackage(fileName, packageBinary);
            return Task.FromResult(this);
        }

        public Task RemovePackage(PackageKey package)
        {
            // _protocolClient.RemovePackage(package);
            return Task.FromResult(this);
        }

        public Task<byte[]> DownloadPackage(PackageKey package)
        {
            // _protocolClient.DownloadPackage(package);
            return Task.FromResult(new byte[0]);
        }


        private void OnPackageStateChanged(PackageInfo package)
        {
            PackageStateChanged?.Invoke(package);
        }

        private void OnAccountStateChanged(AccountModelInfo account)
        {
            AccountStateChanged?.Invoke(account);
        }

        private void OnBotStateChanged(TradeBotModel bot)
        {
            if (Bots.Snapshot.TryGetValue(bot.InstanceId, out var botModel))
            {
                botModel.State = bot.State.ToInfo();
                botModel.Descriptor = bot.PluginRef?.Metadata.Descriptor;
                BotStateChanged?.Invoke(botModel);
            }
        }

        private void ClientConnectionOnStateChanged(ConnectionModel.States oldState, ConnectionModel.States newState)
        {
            var accountKey = new AccountKey(ClientModel.Connection.CurrentServer, ClientModel.Connection.CurrentLogin);
            if (_accounts.TryGetValue(accountKey, out var account))
            {
                account.ConnectionState = ClientModel.Connection.State.ToInfo();
                account.LastError = ClientModel.Connection.LastError;
                OnAccountStateChanged(account);
            }
            else
            {
                _accounts.Clear();
                account = new AccountModelInfo
                {
                    Key = accountKey,
                    ConnectionState = ClientModel.Connection.State.ToInfo(),
                    LastError = ClientModel.Connection.LastError,
                    UseNewProtocol = ClientModel.Connection.CurrentProtocol == "SFX",
                };
                _accounts.Add(accountKey, account);
            }
        }

        private void LibraryOnPackageUpdated(UpdateInfo<PackageInfo> update)
        {
            _syncContext.Invoke(() =>
            {
                var package = update.Value;
                switch (update.Type)
                {
                    case UpdateType.Added:
                        _packages.Add(package.Key, package);
                        break;
                    case UpdateType.Replaced:
                        _packages[package.Key] = package;
                        break;
                    case UpdateType.Removed:
                        _packages.Remove(package.Key);
                        break;
                }
            });
        }

        private void LibraryOnPluginUpdated(UpdateInfo<PluginInfo> update)
        {
            _syncContext.Invoke(() =>
            {
                var plugin = update.Value;
                switch (update.Type)
                {
                    case UpdateType.Added:
                        _plugins.Add(plugin.Key, plugin);
                        break;
                    case UpdateType.Replaced:
                        _plugins[plugin.Key] = plugin;
                        break;
                    case UpdateType.Removed:
                        _plugins.Remove(plugin.Key);
                        break;
                }
            });
        }

        private void LibraryOnReset()
        {
            _syncContext.Invoke(() =>
            {
                _packages.Clear();
                _plugins.Clear();
            });
        }


        #region IAlgoSetupMetadata implementation

        public MappingCollection Mappings => AlgoEnv.Mappings;

        public IReadOnlyList<ISymbolInfo> Symbols => ClientModel.ObservableSymbolList;

        #endregion IAlgoSetupMetadata implementation
    }
}
