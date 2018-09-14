﻿using Caliburn.Micro;
using Machinarium.Qnil;
using Machinarium.Var;
using SciChart.Charting.Model.DataSeries;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using TickTrader.Algo.Api;
using TickTrader.Algo.Common.Info;
using TickTrader.Algo.Common.Lib;
using TickTrader.Algo.Common.Model;
using TickTrader.Algo.Common.Model.Config;
using TickTrader.Algo.Common.Model.Library;
using TickTrader.Algo.Common.Model.Setup;
using TickTrader.Algo.Core;
using TickTrader.Algo.Core.Lib;
using TickTrader.Algo.Core.Metadata;
using TickTrader.Algo.Core.Repository;

namespace TickTrader.BotTerminal
{
    internal class BacktesterViewModel : Screen, IWindowModel, IAlgoSetupMetadata, IPluginIdProvider, IAlgoSetupContext
    {
        private AlgoEnvironment _env;
        private IShell _shell;
        private SymbolCatalog _catalog;
        private Property<List<BotLogRecord>> _journalContent = new Property<List<BotLogRecord>>();
        private SymbolToken _mainSymbolToken;
        private IVarList<ISymbolInfo> _symbolTokens;
        private IReadOnlyList<ISymbolInfo> _observableSymbolTokens;
        private VarContext _var = new VarContext();
        private TraderClientModel _client;
        private WindowManager _localWnd;
        private double _initialBalance = 10000;
        private string _balanceCurrency = "USD";
        private int _serverPingMs = 200;
        private int _leverage = 100;
        private AccountTypes _accType;
        private int _emulatedPing = 200;

        public BacktesterViewModel(AlgoEnvironment env, TraderClientModel client, SymbolCatalog catalog, IShell shell)
        {
            DisplayName = "Strategy/Indicator Tester";

            _env = env ?? throw new ArgumentNullException("env");
            _catalog = catalog ?? throw new ArgumentNullException("catalog");
            _shell = shell ?? throw new ArgumentNullException("shell");
            _client = client;

            _localWnd = new WindowManager(this);

            ProgressMonitor = new ActionViewModel();
            FeedSources = new ObservableCollection<BacktesterSymbolSetupViewModel>();

            DateRange = new DateRangeSelectionViewModel();
            IsUpdatingRange = new BoolProperty();
            MainTimeFrame = new Property<TimeFrames>();

            MainTimeFrame.Value = TimeFrames.M1;

            //_availableSymbols = env.Symbols;

            AddSymbol(SymbolSetupType.MainSymbol);
            AddSymbol(SymbolSetupType.MainFeed, FeedSources[0].SelectedSymbol.Var);

            SelectedPlugin = new Property<AlgoPluginViewModel>();
            IsPluginSelected = SelectedPlugin.Var.IsNotNull();
            IsTradeBotSelected = SelectedPlugin.Var.Check(p => p != null && p.Descriptor.Type == AlgoTypes.Robot);
            IsRunning = ProgressMonitor.IsRunning;
            IsStopping = ProgressMonitor.IsCancelling;
            CanStart = !IsRunning & client.IsConnected & !IsUpdatingRange.Var & IsPluginSelected;
            CanSetup = !IsRunning & client.IsConnected;
            CanStop = ProgressMonitor.CanCancel;

            Plugins = env.LocalAgentVM.PluginList;

            _mainSymbolToken = SpecialSymbols.MainSymbolPlaceholder;
            var predefinedSymbolTokens = new VarList<ISymbolInfo>(new ISymbolInfo[] { _mainSymbolToken });
            var existingSymbolTokens = _catalog.AllSymbols.Select(s => (ISymbolInfo)s.ToSymbolToken());
            _symbolTokens = VarCollection.Combine<ISymbolInfo>(predefinedSymbolTokens, existingSymbolTokens);
            _observableSymbolTokens = _symbolTokens.AsObservable();

            env.LocalAgentVM.Plugins.Updated += a =>
            {
                if (a.Action == DLinqAction.Remove && a.OldItem.Key == SelectedPlugin.Value?.Key)
                    SelectedPlugin.Value = null;
            };

            ChartPage = new BacktesterChartPageViewModel();
            ResultsPage = new BacktesterReportViewModel();

            _var.TriggerOnChange(SelectedPlugin.Var, a =>
            {
                if (a.New != null)
                {
                    PackageRef = _env.LocalAgent.Library.GetPackageRef(a.New.Info.Key.GetPackageKey());
                    PluginRef = _env.LocalAgent.Library.GetPluginRef(a.New.Info.Key);
                    PluginSetupModel = Algo.Common.Model.Setup.AlgoSetupFactory.CreateSetup(PluginRef, this, this);
                    PluginConfig = null;
                }
            });
        }

        public ActionViewModel ProgressMonitor { get; private set; }
        public IObservableList<AlgoPluginViewModel> Plugins { get; private set; }
        public Property<AlgoPluginViewModel> SelectedPlugin { get; private set; }
        public Property<TimeFrames> MainTimeFrame { get; private set; }
        public BoolVar IsPluginSelected { get; }
        public BoolVar IsTradeBotSelected { get; }
        public BoolVar IsRunning { get; }
        public BoolVar IsStopping { get; }
        public BoolVar CanSetup { get; }
        public BoolVar CanStart { get; }
        public BoolVar CanStop { get; }
        public BoolProperty IsUpdatingRange { get; private set; }
        public DateRangeSelectionViewModel DateRange { get; }
        public ObservableCollection<BacktesterSymbolSetupViewModel> FeedSources { get; private set; }
        //public IEnumerable<TimeFrames> AvailableTimeFrames => EnumHelper.AllValues<TimeFrames>();
        public Var<List<BotLogRecord>> JournalRecords => _journalContent.Var;
        public BacktesterReportViewModel ResultsPage { get; }
        public BacktesterChartPageViewModel ChartPage { get; }
        public PluginSetupModel PluginSetupModel { get; private set; }
        public PluginConfig PluginConfig { get; private set; }
        public AlgoPackageRef PackageRef { get; private set; }
        public AlgoPluginRef PluginRef { get; private set; }

        public void OpenPluginSetup()
        {
            var setup = PluginConfig == null
                ? new BacktesterPluginSetupViewModel(_env.LocalAgent, SelectedPlugin.Value.Info, this, this.GetSetupContextInfo())
                : new BacktesterPluginSetupViewModel(_env.LocalAgent, SelectedPlugin.Value.Info, this, this.GetSetupContextInfo(), PluginConfig);
            _localWnd.OpenMdiWindow("SetupAuxWnd", setup);
            setup.Closed += PluginSetupClosed;
            //_shell.ToolWndManager.OpenMdiWindow("AlgoSetupWindow", setup);
        }

        public async void OpenTradeSetup()
        {
            var setup = new BacktesterTradeSetupViewModel();
            setup.SelectedAccType.Value = _accType;
            setup.InitialBalance.Value = _initialBalance;
            setup.Leverage.Value = _leverage;
            setup.BalanceCurrency.Value = _balanceCurrency;
            setup.EmulatedServerPing.Value = _emulatedPing;

            _localWnd.OpenMdiWindow("SetupAuxWnd", setup);

            if (await setup.Result)
            {
                _emulatedPing = setup.EmulatedServerPing.Value;
                _accType = setup.SelectedAccType.Value;
                _serverPingMs = setup.EmulatedServerPing.Value;

                if (_accType == AccountTypes.Cash || _accType == AccountTypes.Gross)
                {
                    _initialBalance = setup.InitialBalance.Value;
                    _leverage = setup.Leverage.Value;
                    _balanceCurrency = setup.BalanceCurrency.Value;
                }
                else if (_accType == AccountTypes.Cash)
                {
                }
            }
        }

        public void Start()
        {
            ProgressMonitor.Start(DoEmulation);
        }

        public void Stop()
        {
            ProgressMonitor.Cancel();
        }

        private void PluginSetupClosed(BacktesterPluginSetupViewModel setup, bool dlgResult)
        {
            if (dlgResult)
                PluginConfig = setup.GetConfig();

            setup.Closed -= PluginSetupClosed;
        }

        private async Task DoEmulation(IActionObserver observer, CancellationToken cToken)
        {
            try
            {
                ChartPage.Clear();
                ResultsPage.Clear();
                _journalContent.Value = null;

                await PrecacheData(observer, cToken);

                cToken.ThrowIfCancellationRequested();

                await SetupAndRunBacktester(observer, cToken);
            }
            catch (OperationCanceledException)
            {
                observer.SetMessage("Canceled.");
            }
        }

        private async Task PrecacheData(IActionObserver observer, CancellationToken cToken)
        {
            foreach (var symbolSetup in FeedSources)
                await symbolSetup.PrecacheData(observer, cToken, DateRange.From, DateRange.To);
        }

        private async Task SetupAndRunBacktester(IActionObserver observer, CancellationToken cToken)
        {
            var chartSymbol = FeedSources[0].SelectedSymbol.Value;
            var chartTimeframe = FeedSources[0].SelectedTimeframe.Value;
            var chartPriceLayer = BarPriceType.Bid;

            _mainSymbolToken.Id = chartSymbol.Key;

            observer.StartProgress(DateRange.From.GetAbsoluteDay(), DateRange.To.GetAbsoluteDay());
            observer.SetMessage("Emulating...");

            PluginSetupModel.Load(PluginConfig);
            PluginSetupModel.MainSymbolPlaceholder.Id = chartSymbol.Key;

            // TODO: place correctly to avoid domain unload during backtester run
            //PackageRef.IncrementRef();
            //PackageRef.DecrementRef();

            using (var tester = new Backtester(PluginRef, DateRange.From, DateRange.To))
            {
                PluginSetupModel.Apply(tester);

                foreach (var outputSetup in PluginSetupModel.Outputs)
                {
                    if (outputSetup is ColoredLineOutputSetupModel)
                        tester.InitOutputCollection<double>(outputSetup.Id);
                    else if (outputSetup is MarkerSeriesOutputSetupModel)
                        tester.InitOutputCollection<Marker>(outputSetup.Id);
                }

                var updateTimer = new DispatcherTimer();
                updateTimer.Interval = TimeSpan.FromMilliseconds(50);
                updateTimer.Tick += (s, a) =>
                {
                    var point = tester.CurrentTimePoint;
                    if (point != null)
                        observer.SetProgress(tester.CurrentTimePoint.Value.GetAbsoluteDay());
                };
                updateTimer.Start();

                Exception execError = null;

                try
                {
                    foreach (var symbolSetup in FeedSources)
                        symbolSetup.Apply(tester, DateRange.From, DateRange.To);

                    tester.Feed.AddBarBuilder(chartSymbol.Name, chartTimeframe, chartPriceLayer);

                    foreach (var rec in _client.Currencies.Snapshot)
                        tester.Currencies.Add(rec.Key, rec.Value);

                    //foreach (var rec in _client.Symbols.Snapshot)
                    //    tester.Symbols.Add(rec.Key, rec.Value.Descriptor);

                    tester.AccountType = _accType;
                    tester.BalanceCurrency = _balanceCurrency;
                    tester.Leverage = _leverage;
                    tester.InitialBalance = _initialBalance;
                    tester.ServerPing = TimeSpan.FromMilliseconds(_serverPingMs);

                    await Task.Run(() => tester.Run(cToken));

                    observer.SetProgress(DateRange.To.GetAbsoluteDay());
                }
                catch (Exception ex)
                {
                    execError = ex;
                }
                finally
                {
                    updateTimer.Stop();
                }

                await CollectEvents(tester, observer);
                await LoadStats(observer, tester);
                await LoadChartData(tester, observer, tester);

                if (execError != null)
                    throw execError; //observer.SetMessage(execError.Message);
                else
                    observer.SetMessage("Done.");
            }
        }

        private async Task CollectEvents(Backtester tester, IActionObserver observer)
        {
            var totalCount = tester.EventsCount;

            observer.StartProgress(0, totalCount);
            observer.SetMessage("Updating journal...");

            _journalContent.Value = await Task.Run(() =>
            {
                var events = new List<BotLogRecord>(totalCount);

                using (var cde = tester.GetEvents())
                {
                    foreach (var record in cde.JoinPages(i => observer.SetProgress(i)))
                        events.Add(record);

                    return events;
                }
            });
        }

        private async Task LoadStats(IActionObserver observer, Backtester tester)
        {
            observer.SetMessage("Loading testing result data...");

            ResultsPage.Stats = await Task.Factory.StartNew(() => tester.GetStats());
        }

        private async Task LoadChartData(Backtester tester, IActionObserver observer, Backtester backtester)
        {
            var timeFrame = backtester.MainTimeframe;
            var count = backtester.BarHistoryCount;

            timeFrame = AdjustTimeframe(timeFrame, count, out count);

            observer.SetMessage("Loading feed chart data ...");
            var feedChartData = await LoadBarSeriesAsync(tester.GetMainSymbolHistory(timeFrame), observer, timeFrame, count);

            observer.SetMessage("Loading equity chart data...");
            var equityChartData = await LoadBarSeriesAsync(tester.GetEquityHistory(timeFrame), observer, timeFrame, count);

            observer.SetMessage("Loading margin chart data...");
            var marginChartData = await LoadBarSeriesAsync(tester.GetMarginHistory(timeFrame), observer, timeFrame, count);

            ChartPage.SetFeedSeries(feedChartData);
            ChartPage.SetEquitySeries(equityChartData);
            ChartPage.SetMarginSeries(marginChartData);
        }

        private Task<OhlcDataSeries<DateTime, double>> LoadBarSeriesAsync(IPagedEnumerator<BarEntity> src, IActionObserver observer, TimeFrames timeFrame, int totalCount)
        {
            observer.StartProgress(0, totalCount);

            return Task.Run(() =>
            {
                using (src)
                {
                    var chartData = new OhlcDataSeries<DateTime, double>();

                    foreach (var bar in src.JoinPages(i => observer.SetProgress(i)))
                        chartData.Append(bar.OpenTime, bar.Open, bar.High, bar.Low, bar.Close);

                    observer.SetProgress(totalCount);

                    return chartData;
                }
            });
        }

        private TimeFrames AdjustTimeframe(TimeFrames currentFrame, int currentSize, out int aproxNewSize)
        {
            const int maxGraphSize = 1000;

            for (var i = currentFrame; i > TimeFrames.MN; i--)
            {
                aproxNewSize = BarExtentions.GetApproximateTransformSize(currentFrame, currentSize, i);
                if (aproxNewSize <= maxGraphSize)
                    return i;
            }

            aproxNewSize = BarExtentions.GetApproximateTransformSize(currentFrame, currentSize, TimeFrames.MN);
            return TimeFrames.MN;
        }

        private void AddSymbol()
        {
            AddSymbol(SymbolSetupType.AdditionalFeed);
        }

        private void AddSymbol(SymbolSetupType type, Var<SymbolData> symbolSrc = null)
        {
            var smb = new BacktesterSymbolSetupViewModel(type, _catalog.ObservableSymbols, symbolSrc);
            smb.Removed += Smb_Removed;
            smb.OnAdd += AddSymbol;

            smb.IsUpdating.PropertyChanged += IsUpdating_PropertyChanged;

            FeedSources.Add(smb);
        }

        private void Smb_Removed(BacktesterSymbolSetupViewModel smb)
        {
            FeedSources.Remove(smb);
            smb.IsUpdating.PropertyChanged -= IsUpdating_PropertyChanged;
            smb.Removed -= Smb_Removed;

            UpdateRangeState();
        }

        private void UpdateRangeState()
        {
            IsUpdatingRange.Value = FeedSources.Any(s => s.IsUpdating.Value);
            if (!IsUpdatingRange.Value)
            {
                var max = FeedSources.Max(s => s.AvailableRange.Value?.Item2);
                var min = FeedSources.Min(s => s.AvailableRange.Value?.Item1);
                DateRange.UpdateBoundaries(min ?? DateTime.MinValue, max ?? DateTime.MaxValue);
            }
        }

        private void IsUpdating_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateRangeState();
        }

        #region IAlgoSetupMetadata

        IReadOnlyList<ISymbolInfo> IAlgoSetupMetadata.Symbols => _observableSymbolTokens;

        MappingCollection IAlgoSetupMetadata.Mappings => _env.LocalAgent.Mappings;

        IPluginIdProvider IAlgoSetupMetadata.IdProvider => this;

        #endregion


        #region IPluginIdProvider

        string IPluginIdProvider.GeneratePluginId(PluginDescriptor descriptor)
        {
            return descriptor.DisplayName;
        }

        bool IPluginIdProvider.IsValidPluginId(PluginDescriptor descriptor, string pluginId)
        {
            return true;
        }

        #endregion IPluginIdProvider

        #region IAlgoSetupContext

        TimeFrames IAlgoSetupContext.DefaultTimeFrame => MainTimeFrame.Value;

        ISymbolInfo IAlgoSetupContext.DefaultSymbol => _mainSymbolToken;

        MappingKey IAlgoSetupContext.DefaultMapping => new MappingKey(MappingCollection.DefaultFullBarToBarReduction);

        #endregion IAlgoSetupContext
    }
}
