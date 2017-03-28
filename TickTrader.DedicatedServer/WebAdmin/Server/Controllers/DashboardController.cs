﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using TickTrader.Algo.Api;
using TickTrader.Algo.Common.Model.Config;
using TickTrader.DedicatedServer.DS;
using TickTrader.DedicatedServer.WebAdmin.Server.Dto;
using TickTrader.DedicatedServer.WebAdmin.Server.Extensions;

namespace TickTrader.DedicatedServer.WebAdmin.Server.Controllers
{
    [Route("api/[controller]")]
    public class DashboardController: Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly IDedicatedServer _dedicatedServer;

        public DashboardController(IDedicatedServer ddServer, ILogger<DashboardController> logger)
        {
            _dedicatedServer = ddServer;
            _logger = logger;
        }

        [HttpGet]
        public TradeBotDto[] Get()
        {
            var bots = _dedicatedServer.TradeBots.ToArray();
            return bots.Select(b => b.ToDto()).ToArray();
        }

        [HttpPost]
        public TradeBotDto Post([FromBody]PluginSetupDto setup)
        {
            var tradeBot = _dedicatedServer.AddBot(setup.InstanceId,
                new AccountKey(setup.Account.Login, setup.Account.Server),
                new PluginKey(setup.PackageName, setup.PluginId),
                CreateConfig(setup));

            return tradeBot.ToDto();
        }

        private PluginConfig CreateConfig(PluginSetupDto setup)
        {
            var barConfig = new BarBasedConfig();
            barConfig.MainSymbol = setup.Symbol;
            barConfig.PriceType = BarPriceType.Ask;
            foreach(var param in setup.Parameters)
            {
                switch (param.DataType)
                {
                    case "Int":
                        barConfig.Properties.Add(new IntParameter() { Id = param.Id, Value = (int)param.Value });
                        break;
                    case "Double":
                        barConfig.Properties.Add(new DoubleParameter() { Id = param.Id, Value = (double)param.Value });
                        break;
                    case "String":
                        barConfig.Properties.Add(new StringParameter() { Id = param.Id, Value = (string)param.Value });
                        break;
                    case "Enum":
                        barConfig.Properties.Add(new EnumParameter() { Id = param.Id, Value = (string)param.Value });
                        break;
                }
            }

            return barConfig;
        }
    }
}
