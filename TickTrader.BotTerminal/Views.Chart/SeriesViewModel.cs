﻿using SciChart.Charting.Model.ChartSeries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.PaletteProviders;
using SciChart.Data.Numerics;
using System.ComponentModel;
using System.Windows.Media;
using TickTrader.Algo.Core;
using Caliburn.Micro;
using TickTrader.Algo.GuiModel;
using SciChart.Charting.Visuals.RenderableSeries;
using TickTrader.Algo.Api;

namespace TickTrader.BotTerminal
{
    internal static class SeriesViewModel
    {
        public static IRenderableSeriesViewModel Create(ChartModelBase chart, IDataSeries data)
        {
            if (chart is BarChartModel && data is OhlcDataSeries<DateTime, double>)
            {
                switch (chart.SelectedChartType)
                {
                    case SelectableChartTypes.OHLC:
                        return new OhlcRenderableSeriesViewModel() { DataSeries = data, StyleKey = "BarChart_OhlcStyle" };
                    case SelectableChartTypes.Candle:
                        return new CandlestickRenderableSeriesViewModel() { DataSeries = data, StyleKey = "BarChart_CandlestickStyle" };
                    case SelectableChartTypes.Line:
                        return new LineRenderableSeriesViewModel() { DataSeries = data, StyleKey= "BarChart_LineStyle" };
                    case SelectableChartTypes.Mountain:
                        return new MountainRenderableSeriesViewModel() { DataSeries = data, StyleKey = "BarChart_MountainStyle" };
                }
            }

            return null;
        }

        public static IRenderableSeriesViewModel Create(IndicatorModel2 model, OutputSetup outputSetup)
        {
            var seriesData = model.GetOutputSeries(outputSetup.Id);

            if (outputSetup is ColoredLineOutputSetup)
                return Create(seriesData, (ColoredLineOutputSetup)outputSetup);
            else if (outputSetup is MarkerSeriesOutputSetup)
                return Create(seriesData, (MarkerSeriesOutputSetup)outputSetup);

            return null;
        }

        private static IRenderableSeriesViewModel Create(IXyDataSeries seriesData, ColoredLineOutputSetup outputSetup)
        {
            var viewModel = new LineRenderableSeriesViewModel();
            viewModel.DataSeries = seriesData;
            viewModel.DrawNaNAs = outputSetup.Descriptor.PlotType == Algo.Api.PlotType.DiscontinuousLine ?
                 LineDrawMode.Gaps : LineDrawMode.ClosedLines;
            viewModel.Stroke = outputSetup.LineColor;
            viewModel.StrokeThickness = outputSetup.LineThickness;
            viewModel.IsVisible = outputSetup.IsEnabled && outputSetup.IsValid;
            viewModel.StrokeDashArray = outputSetup.LineStyle.ToStrokeDashArray();
            viewModel.StyleKey = "DoubleSeries_Style";

            return viewModel;
        }

        private static IRenderableSeriesViewModel Create(IXyDataSeries seriesData, MarkerSeriesOutputSetup outputSetup)
        {
            var viewModel = new LineRenderableSeriesViewModel();
            viewModel.DataSeries = seriesData;
            viewModel.DrawNaNAs = LineDrawMode.Gaps;
            viewModel.StrokeThickness = 0;
            viewModel.IsVisible = outputSetup.IsEnabled && outputSetup.IsValid;
            viewModel.StyleKey = "MarkerSeries_Style";
            var markerTool = new AlgoPointMarker()
            {
                Stroke = outputSetup.LineColor,
                StrokeThickness = outputSetup.LineThickness
            };
            switch (outputSetup.MarkerSize)
            {
                case MarkerSizes.Large: markerTool.Width = 10; markerTool.Height = 20; break;
                case MarkerSizes.Small: markerTool.Width = 6; markerTool.Height = 12; break;
                default: markerTool.Width = 8; markerTool.Height = 16; break;
            }
            viewModel.PointMarker = markerTool;

            return viewModel;
        }
    }
}
