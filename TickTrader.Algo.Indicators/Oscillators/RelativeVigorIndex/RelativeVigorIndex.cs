﻿using System;
using TickTrader.Algo.Api;
using TickTrader.Algo.Indicators.Trend.MovingAverage;

namespace TickTrader.Algo.Indicators.Oscillators.RelativeVigorIndex
{
    [Indicator(Category = "Oscillators", DisplayName = "Relative Vigor Index", Version = "1.0")]
    public class RelativeVigorIndex : Indicator
    {
        private MovingAverage _moveTriMa, _rangeTriMa;
        private IMA _rviMa, _moveMa, _rangeMa;

        [Parameter(DefaultValue = 10, DisplayName = "Period")]
        public int Period { get; set; }

        [Input]
        public new BarSeries Bars { get; set; }

        [Output(DisplayName = "RVI Average", DefaultColor = Colors.Green)]
        public DataSeries RviAverage { get; set; }

        [Output(DisplayName = "Signal", DefaultColor = Colors.Red)]
        public DataSeries Signal { get; set; }

        public int LastPositionChanged
        {
            get { return 0; }
        }

        public RelativeVigorIndex()
        {
        }

        public RelativeVigorIndex(BarSeries bars, int period)
        {
            Bars = bars;
            Period = period;

            InitializeIndicator();
        }

        protected void InitializeIndicator()
        {
            _moveTriMa = new MovingAverage(Bars.Move, 4, 0, Method.Triangular);
            _rangeTriMa = new MovingAverage(Bars.Range, 4, 0, Method.Triangular);
            _moveMa = MABase.CreateMaInstance(Period, Method.Simple);
            _moveMa.Init();
            _rangeMa = MABase.CreateMaInstance(Period, Method.Simple);
            _rangeMa.Init();
            _rviMa = MABase.CreateMaInstance(4, Method.Triangular);
            _rviMa.Init();
        }

        protected override void Init()
        {
            InitializeIndicator();
        }

        protected override void Calculate()
        {
            var i = LastPositionChanged;
            if (!double.IsNaN(_moveTriMa.Average[i]))
            {
                if (IsUpdate)
                {
                    _moveMa.UpdateLast(_moveTriMa.Average[i]);
                    _rangeMa.UpdateLast(_rangeTriMa.Average[i]);
                }
                else
                {
                    _moveMa.Add(_moveTriMa.Average[i]);
                    _rangeMa.Add(_rangeTriMa.Average[i]);
                }
            }
            if (!double.IsNaN(_rangeMa.Average) && Math.Abs(_rangeMa.Average) < 1e-12)
            {
                RviAverage[i] = _moveMa.Average*_moveMa.Period;
            }
            else
            {
                RviAverage[i] = _moveMa.Average/_rangeMa.Average;
            }
            if (!double.IsNaN(RviAverage[i]))
            {
                if (IsUpdate)
                {
                    _rviMa.UpdateLast(RviAverage[i]);
                }
                else
                {
                    _rviMa.Add(RviAverage[i]);
                }
            }
            Signal[i] = _rviMa.Average;
        }
    }
}
