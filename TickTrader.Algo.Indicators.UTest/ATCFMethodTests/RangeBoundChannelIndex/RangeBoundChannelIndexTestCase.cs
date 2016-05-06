﻿using System;
using TickTrader.Algo.Indicators.Utility;
using TickTrader.Algo.Indicators.UTest.TestCases;
using TickTrader.Algo.Indicators.UTest.Utility;

namespace TickTrader.Algo.Indicators.UTest.ATCFMethodTests.RangeBoundChannelIndex
{
    public class RangeBoundChannelIndexTestCase : SimpleTestCase
    {
        public int Std { get; protected set; }

        public RangeBoundChannelIndexTestCase(Type indicatorType, string symbol, string quotesPath, string answerPath,
            int std) : base(indicatorType, symbol, quotesPath, answerPath, 40)
        {
            Std = std;
        }

        protected override void SetupParameters() { }

        protected override void SetupInput()
        {
            BarInputHelper.MapPrice(Builder, Symbol, AppliedPrice.Target.Close);
        }

        protected override void GetOutput()
        {
            PutOutputToBuffer("Rbci", 0);
            PutOutputToBuffer("UpperBound", 1);
            PutOutputToBuffer("LowerBound", 2);
            PutOutputToBuffer("UpperBound2", 3);
            PutOutputToBuffer("LowerBound2", 4);
        }
    }
}
