﻿using TickTrader.Algo.Core.Metadata;

namespace TickTrader.Algo.Common.Info
{
    public class PluginInfo
    {
        public PluginKey Key { get; set; }

        public PluginDescriptor Descriptor { get; set; }


        public PluginInfo() { }

        public PluginInfo(PluginKey key, PluginDescriptor descriptor)
        {
            Key = key;
            Descriptor = descriptor;
        }
    }
}
