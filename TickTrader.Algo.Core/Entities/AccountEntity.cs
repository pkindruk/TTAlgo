﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.Algo.Api;

namespace TickTrader.Algo.Core
{
    public class AccountEntity : AccountDataProvider
    {
        public AccountEntity()
        {
            Orders = new OrdersCollection();
            Assets = new AssetsCollection();
        }

        public OrdersCollection Orders { get; private set; }
        public AssetsCollection Assets { get; private set; }

        public string Id { get; set; }
        public double Balance { get; set; }
        public string BalanceCurrency { get; set; }
        public AccountTypes Type { get; set; }

        internal void FireBalanceUpdateEvent()
        {
            BalanceUpdated();
        }

        OrderList AccountDataProvider.Orders { get { return Orders.OrderListImpl; } }
        AssetList AccountDataProvider.Assets { get { return Assets.AssetListImpl; } }

        public double Equity
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public NetPositionList NetPositions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event Action BalanceUpdated = delegate { };
    }
}
