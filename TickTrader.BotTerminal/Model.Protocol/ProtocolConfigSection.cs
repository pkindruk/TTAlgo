﻿using System.Configuration;

namespace TickTrader.BotTerminal
{
    public class ProtocolConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("logs", IsRequired = true)]
        public LoggingConfigElement Logs => (LoggingConfigElement)this["logs"];

        [ConfigurationProperty("servers", IsRequired = false)]
        public ProtocolServerElementCollection Servers
        {
            get { return (ProtocolServerElementCollection)this["servers"]; }
        }


        public static ProtocolConfigSection GetCfgSection()
        {
            ProtocolConfigSection section = ConfigurationManager.GetSection("protocol.config") as ProtocolConfigSection;
            if (section == null)
                throw new ConfigurationErrorsException("Section protocol.config is not found in the application configuration file.");
            return section;
        }
    }


    public class LoggingConfigElement : ConfigurationElement
    {
        public string DirectoryName => (string)this["dir"];

        public bool LogEvents => (bool)this["logEvents"];

        public bool LogStates => (bool)this["logStates"];

        public bool LogMessages => (bool)this["logMessages"];
    }


    [ConfigurationCollection(typeof(ProtocolServerElement), AddItemName = "server")]
    public class ProtocolServerElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ProtocolServerElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ProtocolServerElement)element).Address;
        }
    }


    public class ProtocolServerElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = false)]
        public string Name => (string)this["name"];

        [ConfigurationProperty("short", IsRequired = false)]
        public string ShortName => (string)this["short"];

        [ConfigurationProperty("address", IsRequired = true)]
        public string Address => (string)this["address"];

        [ConfigurationProperty("port", IsRequired = true)]
        public int Port => (int)this["port"];

        [ConfigurationProperty("certName", IsRequired = true)]
        public string CertificateName => (string)this["certName"];

        [ConfigurationProperty("color", IsRequired = true)]
        public string Color => (string)this["color"];
    }
}
