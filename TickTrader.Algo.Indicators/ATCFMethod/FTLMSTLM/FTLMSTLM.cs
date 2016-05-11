﻿using TickTrader.Algo.Api;

namespace TickTrader.Algo.Indicators.ATCFMethod.FTLMSTLM
{
    [Indicator(Category = "AT&CF Method", DisplayName = "AT&CF Method/FTLM-STLM")]
    public class FtlmStlm : Indicator
    {
        private FastTrendLineMomentum.FastTrendLineMomentum _ftlm;
        private SlowTrendLineMomentum.SlowTrendLineMomentum _stlm;

        [Input]
        public DataSeries Price { get; set; }

        [Output(DisplayName = "FTLM", DefaultColor = Colors.DarkKhaki)]
        public DataSeries Ftlm { get; set; }

        [Output(DisplayName = "STLM", DefaultColor = Colors.DarkSalmon)]
        public DataSeries Stlm { get; set; }

        public int LastPositionChanged { get { return _ftlm.LastPositionChanged; } }

        public FtlmStlm() { }

        public FtlmStlm(DataSeries price)
        {
            Price = price;

            InitializeIndicator();
        }

        private void InitializeIndicator()
        {
            _ftlm = new FastTrendLineMomentum.FastTrendLineMomentum(Price);
            _stlm = new SlowTrendLineMomentum.SlowTrendLineMomentum(Price);
        }

        protected override void Init()
        {
            InitializeIndicator();
        }

        protected override void Calculate()
        {
            var pos = LastPositionChanged;
            Ftlm[pos] = _ftlm.Ftlm[pos];
            Stlm[pos] = _stlm.Stlm[pos];
        }
    }
}
