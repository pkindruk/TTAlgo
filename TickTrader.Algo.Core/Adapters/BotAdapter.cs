﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.Algo.Api;

namespace TickTrader.Algo.Core
{
    internal class BotAdapter : PluginAdapter
    {
        internal BotAdapter(Func<AlgoPlugin> pluginFactory, IPluginDataProvider provider)
            : base(pluginFactory, provider, new BuffersCoordinator())
        {
            InitParameters();
            BindUpInputs();
            BindUpOutputs();
        }

        public override void InvokeCalculate(bool isUpdate)
        {

        }

        public override void InvokeOnStart()
        {
            ((TradeBot)PluginInstance).InvokeStart();
        }

        public override void InvokeOnStop()
        {
            ((TradeBot)PluginInstance).InvokeStop();
        }
    }
}
