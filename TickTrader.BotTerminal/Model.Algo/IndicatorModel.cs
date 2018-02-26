﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using TickTrader.Algo.Core;
using SciChart.Charting.Model.DataSeries;
using TickTrader.Algo.Common.Model.Setup;
using TickTrader.Algo.Api;
using System.Linq;

namespace TickTrader.BotTerminal
{
    internal class IndicatorModel : PluginModel
    {
        private Dictionary<string, IXyDataSeries> _series = new Dictionary<string, IXyDataSeries>();


        public bool HasOverlayOutputs { get { return Setup.Outputs.Any(o => o.Descriptor.Target == OutputTargets.Overlay); } }
        public bool HasPaneOutputs { get { return Setup.Outputs.Any(o => o.Descriptor.Target != OutputTargets.Overlay); } }


        private bool IsRunning { get; set; }
        private bool IsStopping { get; set; }


        public IndicatorModel(PluginSetupViewModel pSetup, IAlgoPluginHost host)
            : base(pSetup, host)
        {
            host.StartEvent += Host_StartEvent;
            host.StopEvent += Host_StopEvent;

            if (host.IsStarted)
                StartIndicator();
        }


        public IXyDataSeries GetOutputSeries(string id)
        {
            return _series[id];
        }

        public override void Dispose()
        {
            base.Dispose();

            Host.StartEvent -= Host_StartEvent;
            Host.StopEvent -= Host_StopEvent;
            if (IsRunning)
                StopIndicator().ContinueWith(t => { /* TO DO: log errors */ });
        }


        protected override PluginExecutor CreateExecutor()
        {
            var executor = base.CreateExecutor();

            foreach (var outputSetup in Setup.Outputs)
            {
                if (outputSetup is ColoredLineOutputSetupModel)
                {
                    var buffer = executor.GetOutput<double>(outputSetup.Id);
                    var adapter = new DoubleSeriesAdapter(buffer, (ColoredLineOutputSetupModel)outputSetup);
                    _series.Add(outputSetup.Id, adapter.SeriesData);
                }
                else if (outputSetup is MarkerSeriesOutputSetupModel)
                {
                    var buffer = executor.GetOutput<Marker>(outputSetup.Id);
                    var adapter = new MarkerSeriesAdapter(buffer, (MarkerSeriesOutputSetupModel)outputSetup);
                    _series.Add(outputSetup.Id, adapter.SeriesData);
                }
            }

            return executor;
        }


        private void StartIndicator()
        {
            if (!IsRunning)
            {
                IsRunning = StartExcecutor();
            }
        }

        private async Task StopIndicator()
        {
            if (IsRunning && !IsStopping)
            {
                IsStopping = true;
                await StopExecutor();
                IsRunning = false;
                IsStopping = false;
                foreach (var dataLine in this._series.Values)
                    dataLine.Clear();
            }
        }

        private void Host_StartEvent()
        {
            StartIndicator();
        }

        private Task Host_StopEvent(object sender, CancellationToken cToken)
        {
            return StopIndicator();
        }
    }
}
