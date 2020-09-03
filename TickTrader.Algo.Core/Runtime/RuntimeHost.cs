﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TickTrader.Algo.Core.Lib;

namespace TickTrader.Algo.Core
{
    public interface IRuntimeHostProxy
    {
        Task Start(string address, int port, string proxyId);

        Task Stop();
    }


    public static class RuntimeHost
    {
        public static IRuntimeHostProxy Create(bool isolated)
        {
            if (isolated)
                return new CrossDomainRuntimeHost();
                //return new ChildProcessRuntimeHost();
            else
                return new TransparentRuntimeHost();
        }
    }


    public class TransparentRuntimeHost : IRuntimeHostProxy
    {
        public Task Start(string address, int port, string proxyId)
        {
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }
    }


    public class CrossDomainRuntimeHost : IRuntimeHostProxy, IDisposable
    {
        private Isolated<RuntimeV1Loader> _subDomain;


        public async Task Start(string address, int port, string proxyId)
        {
            _subDomain = new Isolated<RuntimeV1Loader>();

            await Task.Factory.StartNew(() => _subDomain.Value.Init(address, port, proxyId));
        }

        public async Task Stop()
        {
            await Task.Factory.StartNew(() => _subDomain.Value.Deinit());

            _subDomain?.Dispose();
            _subDomain = null;
        }


        public void Dispose()
        {
            _subDomain?.Dispose();
        }
    }


    public class ChildProcessRuntimeHost : IRuntimeHostProxy
    {
        private const int AbortTimeout = 5000;

        private readonly string _filePath = "/AlgoRuntime/TickTrader.Algo.RuntimeV1.exe";

        private Process _process;
        private TaskCompletionSource<bool> _stopTaskSrc;


        public Task Start(string address, int port, string proxyId)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "AlgoRuntime", "TickTrader.Algo.RuntimeV1.exe");
            var startInfo = new ProcessStartInfo(path)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                Arguments = string.Join(" ", address, port.ToString(), proxyId),
            };
            _process = Process.Start(startInfo);
            _process.EnableRaisingEvents = true;
            _process.Exited += OnExited;
            return Task.CompletedTask;
        }

        public async Task Stop()
        {
            _stopTaskSrc = new TaskCompletionSource<bool>();
            StopInternal();
            var hasStopped = await _stopTaskSrc.Task.ConfigureAwait(false);
            _process.Exited -= OnExited;
            if (!hasStopped)
                _process.Kill();
        }


        private void OnExited(object sender, EventArgs args)
        {
            _stopTaskSrc?.TrySetResult(true);
        }
        
        private async void StopInternal()
        {
            await Task.Delay(AbortTimeout).ConfigureAwait(false);
            _stopTaskSrc.TrySetResult(_process.HasExited);
        }
    }
}
