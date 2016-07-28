﻿using Caliburn.Micro;
using Machinarium.State;
using Machinarium.Qnil;
using NLog;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.RenderableSeries;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using TickTrader.Algo.Core.Metadata;
using TickTrader.Algo.Core.Repository;
using TickTrader.Algo.GuiModel;
using TickTrader.BotTerminal.Lib;
using SoftFX.Extended;
using Api = TickTrader.Algo.Api;
using TickTrader.Algo.Core;

namespace TickTrader.BotTerminal
{
    public enum SelectableChartTypes { Candle, OHLC, Line, Mountain, DigitalLine, DigitalMountain, Scatter }
    public enum TimelineTypes { Real, Uniform }

    internal abstract class ChartModelBase : PropertyChangedBase, IAlgoSetupFactory, IDisposable, IRateUpdatesListener, IAlgoPluginHost
    {
        private Logger logger;
        private enum States { Idle, LoadingData, Online, Stopping, Closed }
        private enum Events { Loaded, Stopped }

        private StateMachine<States> stateController = new StateMachine<States>(new DispatcherStateMachineSync());
        private List<IDataSeries> seriesCollection = new List<IDataSeries>();
        private DynamicList<IndicatorModel2> indicators = new DynamicList<IndicatorModel2>();
        private PluginCatalog catalog;
        private SelectableChartTypes chartType;
        private bool isIndicatorsOnline;
        private bool isLoading;
        private bool isUpdateRequired;
        private bool isCloseRequested;
        private bool isConnected;
        private readonly List<SelectableChartTypes> supportedChartTypes = new List<SelectableChartTypes>();
        private ChartNavigator navigator;
        private TimelineTypes timelineType;
        private long indicatorNextId = 1;
        private AxisBase timeAxis;
        private bool isCrosshairEnabled;
        private string dateAxisLabelFormat;

        public ChartModelBase(SymbolModel symbol, PluginCatalog catalog, FeedModel feed, BotJournal journal)
        {
            logger = NLog.LogManager.GetCurrentClassLogger();
            this.Feed = feed;
            this.Model = symbol;
            this.catalog = catalog;
            this.Journal = journal;

            TimelineType = TimelineTypes.Uniform;

            this.AvailableIndicators = catalog.Indicators.OrderBy((k, v) => v.DisplayName).Chain().AsObservable();

            this.isConnected = feed.Connection.State.Current == ConnectionModel.States.Online;
            feed.Connection.Connected += Connection_Connected;
            feed.Connection.Disconnected += Connection_Disconnected1;

            symbol.Subscribe(this);

            CurrentAsk = symbol.LastQuote?.Ask;
            CurrentBid = symbol.LastQuote?.Bid;

            stateController.AddTransition(States.Idle, () => isUpdateRequired && isConnected, States.LoadingData);
            stateController.AddTransition(States.LoadingData, Events.Loaded, States.Online);
            stateController.AddTransition(States.Online, () => isUpdateRequired && isConnected, States.Stopping);
            //stateController.AddTransition(States.Stopping, () => isUpdateRequired && isConnected, States.LoadingData);
            stateController.AddTransition(States.Stopping, Events.Stopped, States.Idle);

            stateController.OnEnter(States.LoadingData, ()=> Update(CancellationToken.None));
            stateController.OnEnter(States.Online, StartIndicators);
            stateController.OnEnter(States.Stopping, StopIndicators);

            stateController.StateChanged += (o, n) => logger.Debug("Chart [" + Model.Name + "] " + o + " => " + n);
        }

        protected SymbolModel Model { get; private set; }
        protected FeedModel Feed { get; private set; }
        protected ConnectionModel Connection { get { return Feed.Connection; } }

        public abstract Api.TimeFrames TimeFrame { get; }
        public IEnumerable<IDataSeries> DataSeriesCollection { get { return seriesCollection; } }
        public IObservableListSource<PluginCatalogItem> AvailableIndicators { get; private set; }
        public IDynamicListSource<IndicatorModel2> Indicators { get { return indicators; } }
        public IEnumerable<SelectableChartTypes> ChartTypes { get { return supportedChartTypes; } }
        public IEnumerable<TimelineTypes> AvailableTimelines { get { return new TimelineTypes[] { TimelineTypes.Uniform, TimelineTypes.Real }; } }
        public string SymbolCode { get { return Model.Name; } }
        public BotJournal Journal { get; private set; }
        public double? CurrentAsk { get; private set; }
        public double? CurrentBid { get; private set; }

        protected void Activate()
        {
            stateController.ModifyConditions(() => isUpdateRequired = true);
        }

        protected void AddSeries(IDataSeries series)
        {
            seriesCollection.Add(series);
        }

        public void Deactivate()
        {
        }

        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                if (this.isLoading != value)
                {
                    this.isLoading = value;
                    NotifyOfPropertyChange(nameof(IsLoading));
                    NotifyOfPropertyChange(nameof(IsReady));
                }
            }
        }

        public bool IsReady { get { return !IsLoading; } }

        public TimelineTypes TimelineType
        {
            get { return timelineType; }
            set
            {
                this.timelineType = value;
                NotifyOfPropertyChange("TimelineType");
                SwitchNavigator(value);
            }
        }

        public ChartNavigator Navigator
        {
            get { return navigator; }
            private set
            {
                navigator = value;
                TimeAxis = value.CreateAxis();

                Binding cursorTextFormatBinding = new Binding("DateAxisLabelFormat");
                cursorTextFormatBinding.Source = this;
                cursorTextFormatBinding.Mode = BindingMode.TwoWay;
                TimeAxis.SetBinding(AxisBase.CursorTextFormattingProperty, cursorTextFormatBinding);

                if (NavigatorChanged != null)
                    NavigatorChanged();
            }
        }

        public AxisBase TimeAxis
        {
            get { return timeAxis; }
            set
            {
                timeAxis = value;
                NotifyOfPropertyChange("TimeAxis");
            }
        }

        public SelectableChartTypes SelectedChartType
        {
            get { return chartType; }
            set
            {
                chartType = value;
                ChartTypeChanged();
                NotifyOfPropertyChange(nameof(SelectedChartType));
            }
        }

        public bool IsCrosshairEnabled
        {
            get { return isCrosshairEnabled; }
            set
            {
                this.isCrosshairEnabled = value;
                NotifyOfPropertyChange("IsCrosshairEnabled");
            }
        }

        public string DateAxisLabelFormat
        {
            get { return dateAxisLabelFormat; }
            set
            {
                this.dateAxisLabelFormat = value;
                NotifyOfPropertyChange("DateAxisLabelFormat");
            }
        }

        public int Depth { get { return 0; } }
        public DateTime TimelineStart { get; private set; }
        public DateTime TimelineEnd { get; private set; }

        public event System.Action NavigatorChanged = delegate { };
        public event System.Action ChartTypeChanged = delegate { };
        public event System.Action DepthChanged = delegate { };
        public event System.Action ParamsLocked = delegate { };
        public event System.Action ParamsUnlocked = delegate { };

        public void AddIndicator(PluginSetup setup)
        {
            var indicator = CreateIndicator(setup);
            indicators.Add(indicator);
        }

        public void RemoveIndicator(IndicatorModel2 i)
        {
            if (indicators.Remove(i))
                i.Dispose();
        }

        protected long GetNextIndicatorId()
        {
            return indicatorNextId++;
        }

        protected abstract PluginSetup CreateSetup(AlgoPluginRef catalogItem);
        protected abstract IndicatorBuilder CreateBuilder(PluginSetup setup);

        protected abstract void ClearData();
        protected abstract Task<DataMetrics> LoadData(CancellationToken cToken);
        protected abstract IndicatorModel2 CreateIndicator(PluginSetup setup);

        protected void Support(SelectableChartTypes chartType)
        {
            this.supportedChartTypes.Add(chartType);
        }

        private async void Update(CancellationToken cToken)
        {
            this.IsLoading = true;

            try
            {
                isUpdateRequired = false;
                ClearData();
                await StopEvent.InvokeAsync(this);
                var metrics = await LoadData(cToken);
                TimelineStart = metrics.StartDate;
                TimelineEnd = metrics.EndDate;
                Navigator.Update(metrics.Count, metrics.StartDate, metrics.EndDate);
                StartEvent();
            }
            catch (Exception ex)
            {
                logger.Error("Update ERROR " + ex);
            }

            stateController.PushEvent(Events.Loaded);

            this.IsLoading = false;
        }

        private void SwitchNavigator(TimelineTypes timelineType)
        {
            switch (timelineType)
            {
                case TimelineTypes.Real:
                    Navigator = new NonUniformChartNavigator();
                    break;
                case TimelineTypes.Uniform:
                    Navigator = new UniformChartNavigator();
                    break;
                default:
                    throw new Exception("Unsupported: " + timelineType);
            }
        }

        private void Connection_Disconnected1()
        {
            stateController.ModifyConditions(() => isConnected = false);
        }

        private void Connection_Connected()
        {
            stateController.ModifyConditions(() => isConnected = true);
        }

        public void Dispose()
        {
            AvailableIndicators.Dispose();

            Model.Unsubscribe(this);
        }

        public virtual void OnRateUpdate(Quote tick)
        {
            this.CurrentAsk = tick.Ask;
            this.CurrentBid = tick.Bid;
            NotifyOfPropertyChange(nameof(CurrentAsk));
            NotifyOfPropertyChange(nameof(CurrentBid));
        }

        private void StartIndicators()
        {
            isIndicatorsOnline = true;
            StartEvent();
        }

        private async void StopIndicators()
        {
            isIndicatorsOnline = false;
            await StopEvent.InvokeAsync(this);
            stateController.PushEvent(Events.Stopped);
        }

        IndicatorBuilder IAlgoSetupFactory.CreateBuilder(PluginSetup setup)
        {
            return CreateBuilder(setup);
        }

        PluginSetup IAlgoSetupFactory.CreateSetup(AlgoPluginRef catalogItem)
        {
            return CreateSetup(catalogItem);
        }

        #region IAlgoPluginHost

        void IAlgoPluginHost.Lock()
        {
            ParamsLocked();
        }

        void IAlgoPluginHost.Unlock()
        {
            ParamsUnlocked();
        }

        IPluginFeedProvider IAlgoPluginHost.GetProvider()
        {
            return new PluginFeedProvider(Feed.Symbols, Feed.History);
        }

        protected abstract FeedStrategy GetFeedStrategy();

        FeedStrategy IAlgoPluginHost.GetFeedStrategy()
        {
            return GetFeedStrategy();
        }

        bool IAlgoPluginHost.IsStarted { get { return isIndicatorsOnline; } }

        public event System.Action ParamsChanged = delegate { };
        public event System.Action StartEvent = delegate { };
        public event AsyncEventHandler StopEvent = delegate { return CompletedTask.Default; };

        #endregion

        protected struct DataMetrics
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int Count { get; set; }
        }
    }
}
