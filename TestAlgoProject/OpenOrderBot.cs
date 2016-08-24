﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.Algo.Api;

namespace TestAlgoProject
{
    [TradeBot(DisplayName = "Just Open Order")]
    public class OpenOrderBot : TradeBot
    {
        [Parameter(DefaultValue = 1000D)]
        public double Volume { get; set; }

        protected override void Init()
        {
        }

        protected override void OnStart()
        {
            OpenMarketOrder(Symbols.Current.Code, OrderSides.Buy, OrderVolume.Absolute(Volume));
            Exit();
        }
    }
}
