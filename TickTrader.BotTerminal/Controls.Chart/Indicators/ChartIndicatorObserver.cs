﻿using Caliburn.Micro;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView;
using Machinarium.Qnil;
using SkiaSharp;
using System;
using System.Collections.Generic;
using TickTrader.Algo.Domain;
using TickTrader.Algo.Server;
using static TickTrader.Algo.Domain.Metadata.Types;
using LiveChartsCore.SkiaSharpView.Painting.Effects;

namespace TickTrader.BotTerminal.Controls.Chart
{
    internal sealed class ChartIndicatorObserver : IDisposable
    {
        public static int FreeSubWindowId;

        private readonly object _syncObj = new();
        private readonly Dictionary<(string, OutputTarget), int> _subWindowIdLookup = new();
        private readonly VarDictionary<int, OutputWindowViewModel> _subWindows = new();
        private readonly IVarList<OutputWrapper> _outputWrappers;


        public OutputWindowViewModel Overlay { get; } = new();

        public IObservableList<OutputWindowViewModel> SubWindows { get; }


        public ChartIndicatorObserver(ChartHostProxy chartHost)
        {
            _outputWrappers = chartHost.Indicators.TransformToList().Chain().SelectMany(m => m.Outputs).Chain().Select(o => new OutputWrapper(this, o)).DisposeItems();

            SubWindows = _subWindows.TransformToList().UseSyncContext().Chain().AsObservable();
        }


        public void Dispose()
        {
            _outputWrappers.Dispose();
        }


        private void AddOutput(PluginOutputModel.OutputProxy output)
        {
            lock (_syncObj)
            {
                //if (output.Descriptor.Target == OutputTarget.Overlay)
                //    Overlay.AddOutput(output);

                var key = (output.PluginId, output.Descriptor.Target);
                if (!_subWindowIdLookup.TryGetValue(key, out var windowId))
                {
                    windowId = FreeSubWindowId++;
                    _subWindowIdLookup.Add(key, windowId);
                    _subWindows[windowId] = new OutputWindowViewModel();
                }

                var outputWnd = _subWindows[windowId];
                outputWnd.AddOutput(output);
            }
        }

        private void RemoveOutput(PluginOutputModel.OutputProxy output)
        {
            lock (_syncObj)
            {
                var key = (output.PluginId, output.Descriptor.Target);
                if (!_subWindowIdLookup.TryGetValue(key, out var windowId))
                    return;

                var outputWnd = _subWindows[windowId];
                outputWnd.RemoveOutput(output);

                if (outputWnd.IsEmpty)
                {
                    _subWindowIdLookup.Remove(key);
                    _subWindows.Remove(windowId);

                    outputWnd.Dispose();
                }
            }
        }


        private sealed class OutputWrapper : IDisposable
        {
            private readonly ChartIndicatorObserver _parent;
            private readonly PluginOutputModel.OutputProxy _output;


            public OutputWrapper(ChartIndicatorObserver parent, PluginOutputModel.OutputProxy output)
            {
                _parent = parent;
                _output = output;

                _parent.AddOutput(output);
            }


            public void Dispose()
            {
                _parent.RemoveOutput(_output);
            }
        }
    }

    internal sealed class OutputWindowViewModel : PropertyChangedBase
    {
        private readonly VarDictionary<string, OutputSeriesViewModel> _outputs = new();


        public IObservableList<ISeries> Series { get; }

        public bool IsEmpty => _outputs.Count == 0;


        public OutputWindowViewModel()
        {
            Series = _outputs.TransformToList().Chain().Select(o => o.Series).UseSyncContext().Chain().AsObservable();
        }


        public void Dispose()
        {
            Series.Dispose();
        }


        public void AddOutput(PluginOutputModel.OutputProxy output)
        {
            _outputs.Add(output.SeriesId, new OutputSeriesViewModel(output));
        }

        public void RemoveOutput(PluginOutputModel.OutputProxy output)
        {
            _outputs.Remove(output.SeriesId);
        }
    }

    internal sealed class OutputSeriesViewModel
    {
        private readonly PluginOutputModel.OutputProxy _output;


        public ISeries Series { get; }


        public OutputSeriesViewModel(PluginOutputModel.OutputProxy output)
        {
            _output = output;
            Series = CreateSeries(output.Descriptor, output.Config);
        }


        private static ISeries CreateSeries(OutputDescriptor descriptor, IOutputConfig config)
        {
            var name = descriptor.DisplayName;
            var target = descriptor.Target;
            var precision = descriptor.Precision;
            var type = descriptor.PlotType;

            var mainColor = new SKColor(config.LineColorArgb);
            var settings = new IndicatorChartSettings
            {
                Name = name,
                //Precision = precision == -1 ? digits : precision,
                //Period = config.Timeframe,
            };

            //if (_settings.TryGetValue(target, out var global))
            //    global.Precision = Math.Max(settings.Precision, global.Precision);

            //var values = model.Values.Select(IndicatorPointsFactory.GetDefaultPoint);

            var stroke = new SolidColorPaint
            {
                Color = mainColor,
                StrokeThickness = config.LineThickness,
                PathEffect = GetLineStyle(descriptor.DefaultLineStyle),
            };

            var color = new SolidColorPaint
            {
                Color = mainColor,
            };

            ISeries series;

            if (type is PlotType.Line or PlotType.DiscontinuousLine)
            {
                series = new LineSeries<IndicatorPoint>
                {
                    Fill = Customizer.EmptyPaint,
                    Stroke = stroke,
                    GeometryFill = color,
                    GeometrySize = 0,
                    LineSmoothness = 0,
                    TooltipLabelFormatter = p => p.Model.ToPointTooltipInfo(settings),
                    EnableNullSplitting = type == PlotType.DiscontinuousLine,
                    AnimationsSpeed = Customizer.DefaultAnimationSpeed,
                };
            }
            else if (type is PlotType.Histogram)
            {
                series = new ColumnSeries<IndicatorPoint>
                {
                    IgnoresBarPosition = true,
                    Fill = stroke,
                    TooltipLabelFormatter = p => p.Model.ToPointTooltipInfo(settings),
                    AnimationsSpeed = Customizer.DefaultAnimationSpeed,
                };
            }
            else
            {
                series = new ScatterSeries<IndicatorPoint>
                {
                    Fill = stroke,
                    TooltipLabelFormatter = p => p.Model.ToPointTooltipInfo(settings),
                    AnimationsSpeed = Customizer.DefaultAnimationSpeed,
                    GeometrySize = 3,
                };
            }

            series.Name = name;
            //series.Values = values;
            series.ZIndex = 5;

            return series;
        }

        private static DashEffect GetLineStyle(LineStyle style) =>
            style switch
            {
                LineStyle.Dots or LineStyle.DotsRare or LineStyle.DotsVeryRare => new DashEffect(new float[] { 1, 3 }),
                LineStyle.Lines or LineStyle.LinesDots => new DashEffect(new float[] { 5, 5 }),
                _ => null,
            };
    }
}
