﻿using System;
using System.Threading;

namespace TickTrader.Algo.Server
{
    public interface IBotWriter
    {
        void LogMesssage(Domain.PluginLogRecord record);

        void UpdateStatus(string status);

        void Trace(string status);

        void Close();
    }


    public class BotListenerProxy
    {
        private readonly Action _onStopped;
        private readonly IBotWriter _writer;
        private readonly IDisposable _logSub, _statusSub, _stoppedSub;

        private string _currentStatus;
        private Timer _timer;


        public BotListenerProxy(ExecutorModel executor, Action onStopped, IBotWriter writer)
        {
            _onStopped = onStopped;
            _writer = writer;

            _logSub = executor.LogUpdated.Subscribe(Executor_LogUpdated);
            _statusSub = executor.StatusUpdated.Subscribe(Executor_StatusUpdated);
            _stoppedSub = executor.Stopped.Subscribe(Executor_Stopped);
        }


        public void Start()
        {
            _writer.Trace("Bot started");
            _timer = new Timer(LogStatus, null, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMinutes(1));
        }

        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
                LogStatus(null);
                _writer.Trace("Bot stopped");
                _writer.Close();
            }
        }


        public void Dispose()
        {
            _timer?.Dispose();

            _logSub.Dispose();
            _statusSub.Dispose();
            _stoppedSub.Dispose();
        }


        private void Executor_LogUpdated(Domain.PluginLogRecord record)
        {
            _writer.LogMesssage(record);
        }

        private void Executor_StatusUpdated(Domain.PluginStatusUpdate update)
        {
            _currentStatus = update.Message;
            _writer.UpdateStatus(update.Message);
        }

        private void Executor_Stopped(bool val)
        {
            _onStopped();
        }

        private void LogStatus(object state)
        {
            if (!string.IsNullOrWhiteSpace(_currentStatus))
            {
                _writer.Trace(string.Join(Environment.NewLine, "Status snapshot", _currentStatus, ""));
            }
        }
    }
}
