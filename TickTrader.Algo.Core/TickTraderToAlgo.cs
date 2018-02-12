﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.Algo.Api;
using BL = TickTrader.BusinessLogic;
using BO = TickTrader.BusinessObjects;

namespace TickTrader.Algo.Core
{
    public static class TickTraderToAlgo
    {
        public static BusinessObjects.AccountingTypes Convert(AccountTypes type)
        {
            switch (type)
            {
                case AccountTypes.Cash: return BusinessObjects.AccountingTypes.Cash;
                case AccountTypes.Gross: return BusinessObjects.AccountingTypes.Gross;
                case AccountTypes.Net: return BusinessObjects.AccountingTypes.Net;
            }
            throw new NotImplementedException("Unsupported account type: " + type);
        }

        public static BusinessLogic.AssetChangeTypes Convert(AssetChangeType cType)
        {
            switch (cType)
            {
                case AssetChangeType.Added: return BusinessLogic.AssetChangeTypes.Added;
                case AssetChangeType.Updated: return BusinessLogic.AssetChangeTypes.Replaced;
                case AssetChangeType.Removed: return BusinessLogic.AssetChangeTypes.Removed;
            }
            throw new NotImplementedException("Unsupported change type: " + cType);
        }

        public static BO.OrderTypes GetBlOrderType(this OrderEntity order)
        {
            return Convert(order.Type);
        }

        public static BO.OrderTypes Convert(OrderType apiType)
        {
            switch (apiType)
            {
                case OrderType.Limit: return BO.OrderTypes.Limit;
                case OrderType.StopLimit: return BO.OrderTypes.StopLimit;
                case OrderType.Market: return BO.OrderTypes.Market;
                case OrderType.Position: return BO.OrderTypes.Position;
                case OrderType.Stop: return BO.OrderTypes.Stop;
                default: throw new NotImplementedException("Unknown order type: " + apiType);
            }
        }

        public static BO.OrderSides GetBlOrderSide(this OrderEntity order)
        {
            return Convert(order.Side);
        }

        public static BO.OrderSides Convert(OrderSide apiSide)
        {
            switch (apiSide)
            {
                case OrderSide.Buy: return BO.OrderSides.Buy;
                case OrderSide.Sell: return BO.OrderSides.Sell;
                default: throw new NotImplementedException("Unknown order side: " + apiSide);
            }
        }

        public static double? AsNullable(this double value)
        {
            return double.IsNaN(value) ? null : (double?)value;
        }
    }
}
