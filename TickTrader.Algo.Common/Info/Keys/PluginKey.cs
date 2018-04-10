﻿namespace TickTrader.Algo.Common.Info
{
    public class PluginKey
    {
        public string PackagePath { get; set; }

        public string DescriptorId { get; set; }


        public PluginKey() { }

        public PluginKey(string packagePath, string descriptorId)
        {
            PackagePath = packagePath;
            DescriptorId = descriptorId;
        }


        public override string ToString()
        {
            return $"plugin {DescriptorId} from {PackagePath}";
        }
    }
}
