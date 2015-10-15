﻿using Abt.Controls.SciChart;
using Abt.Controls.SciChart.Model.DataSeries;
using Abt.Controls.SciChart.Visuals.RenderableSeries;
using Caliburn.Micro;
using SoftFX.Extended;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TickTrader.Algo.Core.Repository;
using TickTrader.BotTerminal.Lib;

namespace TickTrader.BotTerminal
{
    class ChartViewModel : Screen
    {
        private static readonly BarPeriod[] PredefinedPeriods = new BarPeriod[]
        {
            BarPeriod.MN1,
            BarPeriod.W1,
            BarPeriod.D1,
            BarPeriod.H4,
            BarPeriod.H1,
            BarPeriod.M30,
            BarPeriod.M15,
            BarPeriod.M5,
            BarPeriod.M1,
            BarPeriod.S10,
            BarPeriod.S1
        };

        public enum SelectableChartTypes { Candle, OHLC, Line, Mountain }

        private readonly ConnectionModel connection;
        private readonly TriggeredActivity updateActivity;
        private AlgoRepositoryModel repository;
        private Bar[] rawData;

        public ChartViewModel(string symbol, ConnectionModel connection, AlgoRepositoryModel repository)
        {
            this.Symbol = symbol;
            this.DisplayName = symbol;
            this.connection = connection;
            this.repository = repository;

            this.updateActivity = new TriggeredActivity(Update);

            this.Indicators = new BindableCollection<IndicatorBuilderModel>();

            UpdateSeriesStyle();

            connection.Connected += connection_Connected;
            connection.Disconnected += connection_Disconnected;

            repository.Removed += repository_Removed;
            repository.Replaced += repository_Replaced;

            if (connection.State.Current == ConnectionModel.States.Online)
                updateActivity.Trigger();
        }

        #region Bindable Properties

        private bool isBusy;
        private IndexRange visibleRange = new IndexRange(0, 10);
        private OhlcDataSeries<DateTime, double> data;
        private BarPeriod selectedPeriod = BarPeriod.M30;
        private SelectableChartTypes chartType = SelectableChartTypes.Candle;
        private ObservableCollection<IRenderableSeries> series = new ObservableCollection<IRenderableSeries>();

        public BarPeriod[] AvailablePeriods { get { return PredefinedPeriods; } }

        public IndexRange VisibleRange
        {
            get { return visibleRange; }
            set
            {
                visibleRange = value;
                NotifyOfPropertyChange("VisibleRange");
            }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                if (this.isBusy != value)
                {
                    this.isBusy = value;
                    NotifyOfPropertyChange("IsBusy");
                }
            }
        }

        public OhlcDataSeries<DateTime, double> Data
        {
            get { return data; }
            set
            {
                data = value;
                series[0].DataSeries = value;
                NotifyOfPropertyChange("Data");
            }
        }

        public IRenderableSeries MainSeries
        {
            get { return series[0]; }
            set
            {
                if (series.Count > 0)
                    series[0] = value;
                else
                    series.Add(value);
            }
        }

        public BarPeriod SelectedPeriod
        {
            get { return selectedPeriod; }
            set
            {
                selectedPeriod = value;
                NotifyOfPropertyChange("SelectedPeriod");
                updateActivity.Trigger(true);
            }
        }

        public ObservableCollection<IRenderableSeries> Series { get { return series; } }

        public Array ChartTypes { get { return Enum.GetValues(typeof(SelectableChartTypes)); } }

        public SelectableChartTypes SelectedChartType
        {
            get { return chartType; }
            set
            {
                chartType = value;
                NotifyOfPropertyChange("SelectedChartType");
                UpdateSeriesStyle();
            }
        }

        public BindableCollection<AlgoRepositoryItem> RepositoryIndicators { get { return repository.Indicators; } }

        public BindableCollection<IndicatorBuilderModel> Indicators { get; private set; }

        #endregion

        public void OpenIndicator(object descriptorObj)
        {
            try
            {
                AlgoRepositoryItem metadata = (AlgoRepositoryItem)descriptorObj;
                var model = new IndicatorBuilderModel(metadata);
                model.SetData(rawData);
                AddIndicator(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void AddIndicator(IndicatorBuilderModel indicator)
        {
            AddOutputs(indicator);
            Indicators.Add(indicator);
        }

        private void RemoveIndicator(IndicatorBuilderModel indicator)
        {
            RemoveOutputs(indicator);
            Indicators.Remove(indicator);
        }

        private void ReplaceIndicator(int index, IndicatorBuilderModel newIndicator)
        {
            RemoveOutputs(Indicators[index]);
            Indicators[index] = newIndicator;
            AddOutputs(newIndicator);
        }

        private void AddOutputs(IndicatorBuilderModel indicator)
        {
            foreach (var output in indicator.Series)
            {
                FastLineRenderableSeries chartSeries = new FastLineRenderableSeries();
                chartSeries.DataSeries = output;
                Series.Add(chartSeries);
            }
        }

        private void RemoveOutputs(IndicatorBuilderModel indicator)
        {
            foreach (var output in indicator.Series)
            {
                var seriesIndex = Series.IndexOf(s => s.DataSeries == output);
                if (seriesIndex > 0)
                    Series.RemoveAt(seriesIndex);
            }
        }

        private Task connection_Disconnected(object sender)
        {
            return updateActivity.Stop();
        }

        private void connection_Connected()
        {
            updateActivity.Trigger(true);
        }

        void repository_Removed(AlgoRepositoryItem item)
        {
            foreach (var indicator in Indicators)
            {
                if (indicator.AlgoId == item.Id)
                    indicator.Dispose();
            }
        }

        void repository_Replaced(AlgoRepositoryItem item)
        {
            for (int i = 0; i < Indicators.Count; i++)
            {
                if (Indicators[i].AlgoId == item.Id)
                {
                    var newModel = new IndicatorBuilderModel(item);
                    if (rawData != null)
                        newModel.SetData(rawData);
                    ReplaceIndicator(i, newModel);
                }
            }
        }

        private async Task Update(CancellationToken cToken)
        {
            this.Data = null;
            this.IsBusy = true;

            try
            {
                var response = await Task.Factory.StartNew(
                    () => connection.FeedProxy.Server.GetHistoryBars(
                        Symbol, DateTime.Now + TimeSpan.FromDays(1),
                        -4000, SoftFX.Extended.PriceType.Ask, SelectedPeriod));

                cToken.ThrowIfCancellationRequested();

                var newData = new OhlcDataSeries<DateTime, double>();

                this.rawData = response.Bars.Reverse().ToArray();

                foreach (var bar in rawData)
                    newData.Append(bar.From, bar.Open, bar.High, bar.Low, bar.Close);

                foreach (var indicator in this.Indicators)
                    indicator.SetData(rawData);

                this.Data = newData;
                
                if (newData.Count > 0)
                {
                    this.VisibleRange.Max = newData.Count - 1;
                    this.VisibleRange.Min = Math.Max(0, newData.Count - 101);
                }
            }
            catch (Exception ex)
            {
            }

            this.IsBusy = false;
        }

        public string Symbol { get; private set; }

        private void UpdateSeriesStyle()
        {
            switch (SelectedChartType)
            {
                case SelectableChartTypes.Candle:
                    MainSeries = new FastCandlestickRenderableSeries();
                    break;
                case SelectableChartTypes.Line:
                    MainSeries = new FastLineRenderableSeries();
                    break;
                case SelectableChartTypes.OHLC:
                    MainSeries = new FastOhlcRenderableSeries();
                    break;
                case SelectableChartTypes.Mountain:
                    MainSeries = new FastMountainRenderableSeries();
                    break;
            }

            MainSeries.DataSeries = Data;
        }
    }
}