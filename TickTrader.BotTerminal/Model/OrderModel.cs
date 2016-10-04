﻿using Caliburn.Micro;
using SoftFX.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.Algo.Core;
using TickTrader.BusinessLogic;
using TickTrader.BusinessObjects;

namespace TickTrader.BotTerminal
{
    internal class OrderModel : PropertyChangedBase, TickTrader.BusinessLogic.IOrderModel
    {
        private string clientOrderId;
        private TradeRecordType orderType;
        private decimal amount;
        private decimal amountRemaining;
        public TradeRecordSide side;
        private decimal? price;
        private decimal? swap;
        private decimal? commission;
        private DateTime? created;
        private DateTime? expiration;
        private string comment;
        private double? stopLoss;
        private double? takeProfit;
        private decimal? profit;
        private decimal? margin;
        private decimal? currentPrice;

        public OrderModel(TradeRecord record)
        {
            this.Id = record.OrderId;
            this.clientOrderId = record.ClientOrderId;
            this.OrderId = long.Parse(Id);
            this.Symbol = record.Symbol;
            Update(record);
        }

        public OrderModel(ExecutionReport report)
        {
            this.Id = report.OrderId;
            this.clientOrderId = report.ClientOrderId;
            this.OrderId = long.Parse(Id);
            this.Symbol = report.Symbol;

            Update(report);
        }

        event Action<IOrderModel> IOrderModel.EssentialParametersChanged { add { } remove { } }

        #region Order Properties

        public string Id { get; private set; }
        public long OrderId { get; private set; }
        public string Symbol { get; private set; }
        public decimal Amount
        {
            get { return amount; }
            private set
            {
                if (this.amount != value)
                {
                    this.amount = value;
                    NotifyOfPropertyChange(nameof(Amount));
                }
            }
        }
        public decimal RemainingAmount
        {
            get { return amountRemaining; }
            private set
            {
                if (this.amountRemaining != value)
                {
                    this.amountRemaining = value;
                    NotifyOfPropertyChange(nameof(RemainingAmount));
                }
            }
        }
        public TradeRecordType OrderType
        {
            get { return orderType; }
            private set
            {
                if (orderType != value)
                {
                    this.orderType = value;
                    NotifyOfPropertyChange(nameof(OrderType));
                }
            }
        }
        public TradeRecordSide Side
        {
            get { return side; }
            private set
            {
                if (side != value)
                {
                    side = value;
                    NotifyOfPropertyChange(nameof(Side));
                }
            }
        }
        public decimal? Price
        {
            get { return price; }
            private set
            {
                if (price != value)
                {
                    price = value;
                    NotifyOfPropertyChange(nameof(Price));
                }
            }
        }
        public decimal? Swap
        {
            get { return swap; }
            private set
            {
                if (swap != value)
                {
                    swap = value;
                    NotifyOfPropertyChange(nameof(Swap));
                }
            }
        }
        public decimal? Commission
        {
            get { return commission; }
            private set
            {
                if (commission != value)
                {
                    commission = value;
                    NotifyOfPropertyChange(nameof(Commission));
                }
            }
        }
        public DateTime? Created
        {
            get { return created; }
            private set
            {
                if (created != value)
                {
                    created = value;
                    NotifyOfPropertyChange(nameof(Created));
                }
            }
        }
        public DateTime? Expiration
        {
            get { return expiration; }
            private set
            {
                if (expiration != value)
                {
                    expiration = value;
                    NotifyOfPropertyChange(nameof(Expiration));
                }
            }
        }
        public string Comment
        {
            get { return comment; }
            private set
            {
                if (comment != value)
                {
                    comment = value;
                    NotifyOfPropertyChange(nameof(Comment));
                }
            }
        }
        public double? TakeProfit
        {
            get { return takeProfit; }
            private set
            {
                if (takeProfit != value)
                {
                    takeProfit = value;
                    NotifyOfPropertyChange(nameof(TakeProfit));
                }
            }
        }
        public double? StopLoss
        {
            get { return stopLoss; }
            private set
            {
                if (stopLoss != value)
                {
                    stopLoss = value;
                    NotifyOfPropertyChange(nameof(StopLoss));
                }
            }
        }
        public decimal? Profit
        {
            get { return profit; }
            set
            {
                if (profit != value)
                {
                    profit = value;
                    NotifyOfPropertyChange(nameof(Profit));
                    NetProfit = profit + commission + swap;
                    NotifyOfPropertyChange(nameof(NetProfit));
                }
            }
        }
        public decimal? Margin
        {
            get { return margin; }
            set
            {
                if (margin != value)
                {
                    margin = value;
                    NotifyOfPropertyChange(nameof(Margin));
                }
            }
        }
        public decimal? NetProfit { get; private set; }
        public decimal? CurrentPrice
        {
            get { return currentPrice; }
            set
            {
                if (currentPrice != value)
                {
                    currentPrice = value;
                    NotifyOfPropertyChange(nameof(CurrentPrice));
                }
            }
        }

        #endregion

        #region IOrderModel

        public decimal? AgentCommision { get { return 0; } }
        public OrderError CalculationError { get; set; }
        public OrderCalculator Calculator { get; set; }
        bool IOrderModel.IsCalculated { get { return CalculationError == null; } }
        decimal? IOrderModel.MarginRateCurrent { get; set; }

        OrderTypes ICommonOrder.Type
        {
            get
            {
                switch (orderType)
                {
                    case TradeRecordType.Limit: return OrderTypes.Limit;
                    case TradeRecordType.Market: return OrderTypes.Market;
                    case TradeRecordType.Stop: return OrderTypes.Stop;
                    case TradeRecordType.Position: return OrderTypes.Position;
                    default: throw new NotImplementedException();
                }
            }
        }

        OrderSides ICommonOrder.Side
        {
            get
            {
                switch (side)
                {
                    case TradeRecordSide.Buy: return OrderSides.Buy;
                    case TradeRecordSide.Sell: return OrderSides.Sell;
                    default: throw new NotImplementedException();
                }
            }
        }

        #endregion

        public OrderEntity ToAlgoOrder()
        {
            return new OrderEntity(Id)
            {
                ClientOrderId = this.clientOrderId,
                RemainingAmount = (double)RemainingAmount,
                RequestedAmount = (double)Amount,
                Symbol = Symbol,
                Type = FdkToAlgo.Convert(orderType),
                Side = FdkToAlgo.Convert(Side),
                Price = (double)Price,
                Comment = this.Comment
            };
        }

        private void Update(TradeRecord record)
        {
            this.Amount = (decimal)record.InitialVolume;
            this.RemainingAmount = (decimal)record.Volume;
            this.OrderType = record.Type;
            this.Side = record.Side;
            this.Price = (decimal)record.Price;
            this.Created = record.Created;
            this.Expiration = record.Expiration;
            this.Comment = record.Comment;
            this.StopLoss = record.StopLoss;
            this.TakeProfit = record.TakeProfit;
            this.Swap = (decimal)record.Swap;
            this.Commission = (decimal)record.Commission;
        }

        private void Update(ExecutionReport report)
        {
            this.Amount = (decimal?)report.InitialVolume ?? 0M;
            this.RemainingAmount = (decimal)report.LeavesVolume;
            this.OrderType = report.OrderType;
            this.Side = report.OrderSide;
            this.Price = (decimal?)(report.Price ?? report.StopPrice) ?? 0;
            this.Created = report.Created;
            this.Expiration = report.Expiration;
            this.Comment = report.Comment;
            this.StopLoss = report.StopLoss;
            this.TakeProfit = report.TakeProfit;
            this.Swap = (decimal)report.Swap;
            this.Commission = (decimal)report.Commission;
        }
    }
}
