﻿using ActorSharp;
using NLog;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using TickTrader.Algo.Core.Lib;
using TickTrader.Algo.Domain;
using TickTrader.Algo.Package;
using TickTrader.Algo.Server;

namespace TickTrader.BotAgent.BA.Models
{
    [DataContract(Name = "tradeBot", Namespace = "")]
    public class TradeBotModel
    {
        private static readonly ILogger _log = LogManager.GetLogger(nameof(ServerModel));

        private PluginConfig _config;
        [DataMember(Name = "configuration")]
        private Algo.Core.Config.PluginConfig _configEntry;

        private AlgoServer _server;
        private ClientModel _client;
        private Task _stopTask;
        private ExecutorModel executor;
        private BotLog.ControlHandler _botLog;
        private AlgoData.ControlHandler _algoData;
        private PluginInfo _info;
        private IDisposable _logSub, _statusSub;
        private TaskCompletionSource<object> _startedEvent;
        private bool _closed;

        public TradeBotModel(PluginConfig config)
        {
            Config = config;
        }


        public PluginConfig Config
        {
            get => _config;
            private set
            {
                _config = value;
                _configEntry = Algo.Core.Config.PluginConfig.FromDomain(value);
            }
        }
        [DataMember(Name = "running")]
        public bool IsRunning { get; private set; }


        public string Id => Config.InstanceId;
        public PluginPermissions Permissions => Config.Permissions;
        public string PackageId { get; private set; }
        public PluginModelInfo.Types.PluginState State { get; private set; }
        public AlgoPackageRef Package { get; private set; }
        public Exception Fault { get; private set; }
        public string FaultMessage { get; private set; }
        public string AccountId => _client.AccountId;
        public Ref<BotLog> LogRef => _botLog.Ref;
        public PluginInfo Info => _info;

        public Ref<AlgoData> AlgoDataRef => _algoData.Ref;

        public event Action<TradeBotModel> StateChanged;
        public event Action<TradeBotModel> IsRunningChanged;
        public event Action<TradeBotModel> ConfigurationChanged;

        public bool OnDeserialized()
        {
            try
            {
                _config = _configEntry.ToDomain();

                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to deserialize bot config {_configEntry.InstanceId}");
            }

            return false;
        }
        
        public bool Init(AlgoServer server, ClientModel client, string workingFolder, AlertStorage storage)
        {
            try
            {
                _server = server;
                _client = client;

                UpdatePackage();

                _client.StateChanged += Client_StateChanged;

                _botLog = new BotLog.ControlHandler(Id, storage);

                _algoData = new AlgoData.ControlHandler(Id);

                if (IsRunning && State != PluginModelInfo.Types.PluginState.Broken)
                    Start();

                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to init bot {Id}");
            }
            return false;
        }

        public void ChangeBotConfig(PluginConfig config)
        {
            CheckShutdownFlag();

            if (State == PluginModelInfo.Types.PluginState.Broken)
                return;

            if (IsStopped())
            {
                if (config.Key == null)
                    config.Key = Config.Key;

                Config = config;
                ConfigurationChanged?.Invoke(this);
            }
            else
                throw new AlgoException("Make sure that the bot is stopped before installing a new configuration");
        }

        private void Client_StateChanged(ClientModel client)
        {
            if (client.ConnectionState == AccountModelInfo.Types.ConnectionState.Online)
            {
                if (State == PluginModelInfo.Types.PluginState.Starting)
                    StartExecutor();
                else if (State == PluginModelInfo.Types.PluginState.Reconnecting)
                {
                    //executor.NotifyReconnectNotification();
                    ChangeState(PluginModelInfo.Types.PluginState.Running);
                }
            }
            else
            {
                if (State == PluginModelInfo.Types.PluginState.Running)
                {
                    //executor.NotifyDisconnectNotification();
                    ChangeState(PluginModelInfo.Types.PluginState.Reconnecting, client.LastError != null && !client.LastError.IsOk ? client.ErrorText : null);
                }
            }
        }

        public Task ClearLog()
        {
            return _botLog.Clear();
        }

        public Task ClearWorkingFolder()
        {
            return _algoData.Clear();
        }

        public void Start()
        {
            CheckShutdownFlag();

            if (!IsStopped())
                throw new AlgoException("Trade bot has been already started!");

            UpdatePackage();

            if (State == PluginModelInfo.Types.PluginState.Broken)
                throw new AlgoException("Trade bot is broken!");

            //Package.IncrementRef();

            SetRunning(true);

            ChangeState(PluginModelInfo.Types.PluginState.Starting);

            if (_client.ConnectionState == AccountModelInfo.Types.ConnectionState.Online)
                StartExecutor();
        }

        public Task StopAsync()
        {
            CheckShutdownFlag();

            return DoStop(null);
        }

        public Task Shutdown()
        {
            _closed = true;
            return DoStop(null);
        }

        public void Remove(bool cleanLog = false, bool cleanAlgoData = false)
        {
            _client.RemoveBot(Id, cleanLog, cleanAlgoData);
        }

        private void OnBotExited()
        {
            DoStop(null).Forget();
        }

        private Task DoStop(string error)
        {
            SetRunning(false);

            if (State == PluginModelInfo.Types.PluginState.Stopped || State == PluginModelInfo.Types.PluginState.Faulted)
                return Task.CompletedTask;

            if (State == PluginModelInfo.Types.PluginState.Starting && (_startedEvent == null || _startedEvent.Task.IsCompleted))
            {
                ChangeState(PluginModelInfo.Types.PluginState.Stopped);
                return Task.CompletedTask; // acc can't connect at bot start, also bot might be launched before
            }

            if (State == PluginModelInfo.Types.PluginState.Running || State == PluginModelInfo.Types.PluginState.Reconnecting || State == PluginModelInfo.Types.PluginState.Starting)
            {
                ChangeState(PluginModelInfo.Types.PluginState.Stopping);
                _stopTask = StopExecutor();
            }

            return _stopTask;
        }

        public void Dispose()
        {
            _client.StateChanged -= Client_StateChanged;
        }

        private bool IsStopped()
        {
            return State == PluginModelInfo.Types.PluginState.Stopped || State == PluginModelInfo.Types.PluginState.Faulted || State == PluginModelInfo.Types.PluginState.Broken;
        }

        private bool TaskIsNullOrStopped(Task task)
        {
            return task == null || task.IsCompleted || task.IsFaulted || task.IsCanceled;
        }

        private async void StartExecutor()
        {
            _startedEvent = new TaskCompletionSource<object>();

            try
            {
                await _algoData.EnsureDirectoryCreated();

                if (executor != null)
                    throw new InvalidOperationException("Cannot start executor: old executor instance is not disposed!");

                var executorConfig = new ExecutorConfig { AccountId = _client.Id, IsLoggingEnabled = true };

                executorConfig.SetPluginConfig(Config);
                executorConfig.InitPriorityInvokeStrategy();
                executorConfig.InitSlidingBuffering(4000);
                executorConfig.InitBarStrategy(Feed.Types.MarketSide.Bid);
                executorConfig.WorkingDirectory = await _algoData.GetFolder();
                executorConfig.LogDirectory = await _botLog.GetFolder();

                executor = await _server.CreateExecutor(Config.Key.PackageId, Config.InstanceId, executorConfig);

                _logSub = executor.LogUpdated.Subscribe(_botLog.AddLog);
                _statusSub = executor.StatusUpdated.Subscribe(_botLog.UpdateStatus);

                await executor.Start();

                ChangeState(PluginModelInfo.Types.PluginState.Running);
            }
            catch (Exception ex)
            {
                Fault = ex;
                _log.Error(ex, "StartExecutor() failed!");
                _startedEvent.SetResult(null);
                SetRunning(false);
                if (State != PluginModelInfo.Types.PluginState.Stopping)
                    await StopExecutor(true);
                return;
            }

            _startedEvent.SetResult(null);
        }

        private async Task StopExecutor(bool hasError = false)
        {
            if (_startedEvent != null)
                await _startedEvent.Task;

            try
            {
                if (executor != null)
                {
                    await Task.Run(() => executor.Stop());
                }
            }
            catch (Exception ex)
            {
                Fault = ex;
                _log.Error(ex, "StopExecutor() failed!");
            }

            DisposeExecutor();
            ChangeState(hasError ? PluginModelInfo.Types.PluginState.Faulted : PluginModelInfo.Types.PluginState.Stopped);
            OnStop();
        }

        private void DisposeExecutor()
        {
            _logSub?.Dispose();
            _statusSub?.Dispose();
            executor?.Dispose();
            executor = null;
        }

        private void OnStop()
        {
            try
            {
                //Package.DecrementRef();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "TradeBotModel.OnStopped() failed! {0}");
            }
        }

        private void ChangeState(PluginModelInfo.Types.PluginState newState, string errorMessage = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                _log.Info("TradeBot '{0}' State: {1}", Id, newState);
            else
                _log.Error("TradeBot '{0}' State: {1} Error: {2}", Id, newState, errorMessage);
            State = newState;
            FaultMessage = errorMessage;
            StateChanged?.Invoke(this);
        }

        private void SetRunning(bool val)
        {
            if (IsRunning != val)
            {
                IsRunning = val;
                IsRunningChanged?.Invoke(this);
            }
        }

        private void UpdatePackage()
        {
            var pluginKey = Config.Key;
            PackageId = pluginKey.PackageId;
            Package = _server.PkgStorage.GetPackageRef(PackageId).Result;

            if (Package == null)
            {
                BreakBot($"Algo package {PackageId} is not found!");
                return;
            }

            _info = Package.PackageInfo.GetPlugin(pluginKey);
            if (_info == null || !_info.Descriptor_.IsTradeBot)
            {
                BreakBot($"Trade bot {pluginKey.DescriptorId} is missing in Algo package {PackageId}!");
                return;
            }

            _server.PkgStorage.ReleasePackageRef(Package);

            if (State == PluginModelInfo.Types.PluginState.Broken)
                ChangeState(PluginModelInfo.Types.PluginState.Stopped, null);
        }

        public void Abort()
        {
            CheckShutdownFlag();

            //if (State == PluginModelInfo.Types.PluginState.Stopping)
            //    executor?.Abort();
        }

        private void BreakBot(string message)
        {
            ChangeState(PluginModelInfo.Types.PluginState.Broken, message);
            SetRunning(false);
        }

        private void CheckShutdownFlag()
        {
            if (_closed)
                throw new InvalidOperationException("Server is shutting down!");
        }

        private string GetConnectionInfo()
        {
            return $"account {_client.Username} on {_client.Address}";
        }
    }
}
