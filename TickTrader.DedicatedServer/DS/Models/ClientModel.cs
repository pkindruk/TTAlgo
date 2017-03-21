﻿using Machinarium.ActorModel;
using Machinarium.State;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using TickTrader.Algo.Common.Model;
using TickTrader.Algo.Common.Model.Setup;
using TickTrader.Algo.Core.Metadata;
using TickTrader.DedicatedServer.DS.Repository;

namespace TickTrader.DedicatedServer.DS.Models
{
    [DataContract(Name = "account", Namespace = "")]
    public class ClientModel : IAccount
    {
        private object _sync;
        private CancellationTokenSource connectCancellation;
        private TaskCompletionSource<ConnectionErrorCodes> testRequest;

        [DataMember(Name = "bots")]
        private List<TradeBotModel> _bots = new List<TradeBotModel>();
        private Func<string, PackageModel> _packageProvider;

        private bool stopRequested;
        private bool lostConnection;
        private int _startedBotsCount;

        public void Init(object syncObj, ILoggerFactory loggerFactory, Func<string, PackageModel> packageProvider)
        {
            _sync = syncObj;
            _packageProvider = packageProvider;
            var loggerAdapter = new LoggerAdapter(loggerFactory.CreateLogger<ConnectionModel>());

            if (_bots == null)
                _bots = new List<TradeBotModel>();

            foreach (var bot in _bots)
            {
                if (bot.IsRunning)
                    _startedBotsCount++;
                bot.IsRunningChanged += Bot_StateChanged;
                bot.Init(this, syncObj, _packageProvider, null);
            }

            Connection = new ConnectionModel(new ConnectionOptions() { EnableFixLogs = false });
            Connection.Disconnected += () =>
            {
                lock (_sync)
                {
                    lostConnection = true;
                    ManageConnection();
                }
            };
        }

        public ConnectionStates ConnectionState { get; private set; }
        public object SyncObj => _sync;
        public ConnectionModel Connection { get; private set; }
        public IEnumerable<ITradeBot> TradeBots => _bots;

        public event Action<ClientModel> StateChanged;
        public event Action<ClientModel> Changed;

        [DataMember(Name = "server")]
        public string Address { get; private set; }
        [DataMember(Name = "login")]
        public string Username { get; private set; }
        [DataMember(Name = "password")]
        public string Password { get; private set; }

        #region Connection Management

        public async Task<ConnectionErrorCodes> TestConnection()
        {
            Task<ConnectionErrorCodes> resultTask = null;

            lock (_sync)
            {
                if (ConnectionState == ConnectionStates.Online)
                    return ConnectionErrorCodes.None;
                else
                {
                    if (testRequest == null)
                    {
                        testRequest = new TaskCompletionSource<ConnectionErrorCodes>();
                        ManageConnection();
                    }

                    resultTask = testRequest.Task;
                }
            }

            return await resultTask;
        }

        private void ManageConnection()
        {
            if (ConnectionState == ConnectionStates.Offline)
            {
                if (testRequest != null || _startedBotsCount > 0)
                    Connect();
            }
            else if (ConnectionState == ConnectionStates.Online)
            {
                if (stopRequested || lostConnection || _startedBotsCount == 0)
                    Disconnect();
            }
        }

        private async void Disconnect()
        {
            ChangeState(ConnectionStates.Disconnecting);

            await Connection.DisconnectAsync();

            lock (_sync)
            {
                ChangeState(ConnectionStates.Offline);
                stopRequested = false;
                lostConnection = false;
                ManageConnection();
            }
        }

        private void ChangeState(ConnectionStates newState)
        {
            ConnectionState = newState;
            StateChanged?.Invoke(this);
        }

        private async void Connect()
        {
            ChangeState(ConnectionStates.Connecting);
            connectCancellation = new CancellationTokenSource();
            var result = await Connection.Connect(Username, Password, Address, connectCancellation.Token);
            lock (_sync)
            {
                if (result == ConnectionErrorCodes.None)
                    ChangeState(ConnectionStates.Online);
                else
                    ChangeState(ConnectionStates.Offline);
                testRequest?.TrySetResult(result);
                testRequest = null;
                ManageConnection();
            }
        }

        public void Change(string address, string username, string password)
        {
            lock (_sync)
            {
                Address = address;
                Username = username;
                Password = password;
                testRequest?.TrySetCanceled();
                testRequest = null;
                stopRequested = true;
                connectCancellation?.Cancel();
                ManageConnection();
            }
        }

        #endregion Connection Management

        public ITradeBot AddBot(string botId, string packageName, PluginSetup setup)
        {
            lock (_sync)
            {
                var package = _packageProvider(packageName);

                if (package == null)
                    throw new ArgumentException("Package '" + packageName + "' cannot be found!");

                var newBot = new TradeBotModel(botId, package, setup);
                newBot.IsRunningChanged += Bot_StateChanged;
                _bots.Add(newBot);
                ManageConnection();
                Changed?.Invoke(this);
                return newBot;
            }
        }

        private void Bot_StateChanged(TradeBotModel bot)
        {
            if (bot.IsRunning)
                _startedBotsCount++;
            else
                _startedBotsCount--;
            ManageConnection();
        }

        public void RemoveBot(string botId)
        {
            lock (_sync)
            {
                var bot = _bots.FirstOrDefault(b => b.Id == botId);
                if (bot != null)
                {
                    if (bot.IsRunning)
                        throw new InvalidStateException("Cannot remove running bot!");
                    _bots.Remove(bot);
                    bot.IsRunningChanged -= Bot_StateChanged;
                    Changed?.Invoke(this);
                }
            }
        }

        internal void RemoveBotsFromPackage(PackageModel package)
        {
            var toRemove = _bots.Where(b => b.Package == package).ToList();
            toRemove.ForEach(b => _bots.Remove(b));
        }
    }
}
