﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.Algo.Api;
using TickTrader.Algo.Common.Model;
using TickTrader.Algo.Core;

namespace TickTrader.BotTerminal
{
    abstract class TransactionReport
    {
        public enum AggregatedTransactionType { Unknown, Buy, BuyLimit, BuyStop, Deposit, Sell, SellLimit, SellStop, Withdrawal, BuyStopLimit, SellStopLimit }
        public enum TransactionSide { None = -1, Buy, Sell }

        public TransactionReport() { }
        public TransactionReport(TradeReportEntity transaction, SymbolModel symbol)
        {
            PriceDigits = symbol?.PriceDigits ?? 5;
            ProfitDigits = symbol?.QuoteCurrencyDigits ?? 2;
            LotSize = symbol?.LotSize ?? 1;

            IsPosition = transaction.TradeRecordType == OrderType.Position;
            IsMarket = transaction.TradeRecordType == OrderType.Market;
            IsPending = transaction.TradeRecordType == OrderType.Limit
                || transaction.TradeRecordType == OrderType.Stop
                || transaction.TradeRecordType == OrderType.StopLimit;
            IsBalanceTransaction = transaction.TradeTransactionReportType == TradeExecActions.BalanceTransaction;

            OrderId = GetId(transaction);
            OpenTime = GetOpenTime(transaction);
            Type = GetTransactionType(transaction);
            Side = GetTransactionSide(transaction);
            ActionType = transaction.TradeTransactionReportType;
            Symbol = GetSymbolOrCurrency(transaction);
            OpenQuantity = GetOpenQuntity(transaction);
            OpenPrice = GetOpenPrice(transaction);
            CloseTime = GetCloseTime(transaction);
            CloseQuantity = GetCloseQuantity(transaction);
            ClosePrice = GetClosePrice(transaction);
            RemainingQuantity = GetRemainingQuntity(transaction);
            Swap = GetSwap(transaction);
            Commission = GetCommission(transaction);
            CommissionCurrency = GetCommissionCurrency(transaction);
            Comment = transaction.Comment;
            Balance = transaction.AccountBalance;
            NetProfitLoss = GetNetProfiLoss(transaction);
            GrossProfitLoss = GetGrossProfitLoss(transaction);
            StopLoss = GetStopLoss(transaction);
            TakeProfit = GetTakeProfit(transaction);
            MaxVisibleVolume = GetMaxVisibleVolume(transaction);

            // should be last (it's based on other fields)
            UniqueId = GetUniqueId(transaction);
        }

        public static TransactionReport Create(AccountTypes accountType, TradeReportEntity tTransaction, SymbolModel symbol = null)
        {
            switch (accountType)
            {
                case AccountTypes.Gross: return new GrossTransactionModel(tTransaction, symbol);
                case AccountTypes.Net: return new NetTransactionModel(tTransaction, symbol);
                case AccountTypes.Cash: return new CashTransactionModel(tTransaction, symbol);
                default: throw new NotSupportedException(accountType.ToString());
            }
        }

        private double? GetMaxVisibleVolume(TradeReportEntity transaction)
        {
            return transaction.MaxVisibleQuantity;
        }

        public TradeReportKey UniqueId { get; protected set; }
        public string OrderId { get; protected set; }
        public DateTime OpenTime { get; protected set; }
        public AggregatedTransactionType Type { get; protected set; }
        public TransactionSide Side { get; protected set; }
        public TradeExecActions ActionType { get; protected set; }
        public string Symbol { get; protected set; }
        public double? OpenQuantity { get; protected set; }
        public double? OpenPrice { get; protected set; }
        public double? StopLoss { get; protected set; }
        public double? TakeProfit { get; protected set; }
        public DateTime CloseTime { get; protected set; }
        public double? CloseQuantity { get; protected set; }
        public double? ClosePrice { get; protected set; }
        public double? RemainingQuantity { get; protected set; }
        public double Commission { get; protected set; }
        public string CommissionCurrency { get; protected set; }
        public double Swap { get; protected set; }
        public double Balance { get; protected set; }
        public string Comment { get; protected set; }
        public int PriceDigits { get; protected set; }
        public int ProfitDigits { get; protected set; }
        public double GrossProfitLoss { get; protected set; }
        public double NetProfitLoss { get; protected set; }
        public bool IsPosition { get; protected set; }
        public bool IsMarket { get; protected set; }
        public bool IsPending { get; protected set; }
        public bool IsBalanceTransaction { get; protected set; }
        public double? MaxVisibleVolume { get; protected set; }
        public double LotSize { get; }

        protected virtual AggregatedTransactionType GetTransactionType(TradeReportEntity transaction)
        {
            if (transaction.TradeTransactionReportType == TradeExecActions.BalanceTransaction)
                return transaction.TransactionAmount > 0 ? AggregatedTransactionType.Deposit : AggregatedTransactionType.Withdrawal;

            switch (transaction.TradeRecordType)
            {
                case OrderType.Market:
                case OrderType.Position:
                    return transaction.TradeRecordSide == OrderSide.Buy ? AggregatedTransactionType.Buy : AggregatedTransactionType.Sell;
                case OrderType.Limit:
                    return transaction.TradeRecordSide == OrderSide.Buy ? AggregatedTransactionType.BuyLimit : AggregatedTransactionType.SellLimit;
                case OrderType.StopLimit:
                    return transaction.TradeRecordSide == OrderSide.Buy ? AggregatedTransactionType.BuyStopLimit : AggregatedTransactionType.SellStopLimit;
                case OrderType.Stop:
                    return transaction.TradeRecordSide == OrderSide.Buy ? AggregatedTransactionType.BuyStop : AggregatedTransactionType.SellStop;
                default: return AggregatedTransactionType.Unknown;
            }
        }

        protected virtual string GetSymbolOrCurrency(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ? transaction.TransactionCurrency : transaction.Symbol;
        }

        protected virtual DateTime? CheckIsNull(DateTime dateTime)
        {
            if (dateTime.Year == 1970 && dateTime.Month == 1 && dateTime.Day == 1)
                return null;

            return dateTime;
        }

        protected virtual TransactionSide GetTransactionSide(TradeReportEntity transaction)
        {
            switch (transaction.TradeRecordSide)
            {
                case OrderSide.Buy: return TransactionSide.Buy;
                case OrderSide.Sell: return TransactionSide.Sell;
                default: return TransactionSide.None;
            }
        }

        protected virtual TradeReportKey GetUniqueId(TradeReportEntity transaction)
        {
            bool hasMultipleRecords = transaction.ActionId > 1 || RemainingQuantity > 0;

            var numericOrderId = long.Parse(transaction.OrderId);

            if (hasMultipleRecords)
                return new TradeReportKey(numericOrderId, transaction.ActionId);
            else
                return new TradeReportKey(numericOrderId, null);

            //if (hasMultipleRecords)
            //    return $"{transaction.OrderId}-{transaction.ActionId}";

            //return transaction.OrderId;
        }

        protected virtual string GetId(TradeReportEntity transaction)
        {
            return transaction.Id;
        }

        protected virtual string GetCommissionCurrency(TradeReportEntity transaction)
        {
            return transaction.TransactionCurrency;
        }

        protected virtual double? GetRemainingQuntity(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ? (double?)null : (transaction.LeavesQuantity / LotSize);
        }

        protected virtual double? GetClosePrice(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ? (double?)null : transaction.PositionClosePrice;
        }

        protected virtual double? GetCloseQuantity(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ? (double?)null : (transaction.PositionLastQuantity / LotSize);
        }

        protected virtual DateTime GetCloseTime(TradeReportEntity transaction)
        {
            return transaction.TransactionTime;
        }

        protected virtual double? GetOpenPrice(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ? (double?)null : transaction.Price;
        }

        protected virtual double? GetOpenQuntity(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ? (double?)null : (transaction.Quantity / LotSize);
        }

        protected virtual DateTime GetOpenTime(TradeReportEntity transaction)
        {
            return transaction.OrderCreated;
        }

        protected virtual double GetNetProfiLoss(TradeReportEntity transaction)
        {
            return transaction.TransactionAmount;
        }

        protected virtual double GetGrossProfitLoss(TradeReportEntity transaction)
        {
            return transaction.TransactionAmount - transaction.Swap - transaction.Commission;
        }

        protected virtual double GetSwap(TradeReportEntity transaction)
        {
            return transaction.Swap;
        }

        protected virtual double GetCommission(TradeReportEntity transaction)
        {
            return transaction.Commission;
        }

        protected virtual double? GetStopLoss(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ? (double?)null : transaction.StopLoss;
        }

        protected virtual double? GetTakeProfit(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ? (double?)null : transaction.TakeProfit;
        }
    }

    class NetTransactionModel : TransactionReport
    {
        public NetTransactionModel(TradeReportEntity transaction, SymbolModel model) : base(transaction, model) { }
        protected override double? GetOpenPrice(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ?
                (double?)null : transaction.TradeRecordType == OrderType.Stop || transaction.TradeRecordType == OrderType.StopLimit ?
                transaction.StopPrice : transaction.PosOpenPrice == 0 ? transaction.Price : transaction.PosOpenPrice;
        }
    }

    class GrossTransactionModel : TransactionReport
    {
        public GrossTransactionModel(TradeReportEntity transaction, SymbolModel symbol) : base(transaction, symbol)
        {
        }

        protected override DateTime GetOpenTime(TradeReportEntity transaction)
        {
            return IsPosition ? transaction.PositionOpened : transaction.OrderCreated;

        }

        protected override double? GetOpenQuntity(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ? (double?)null : IsPosition ? (transaction.PositionQuantity / LotSize) : (transaction.Quantity / LotSize);
        }

        protected override double? GetOpenPrice(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ? (double?)null :
                transaction.TradeRecordType == OrderType.Stop || transaction.TradeRecordType == OrderType.StopLimit ? transaction.StopPrice :
                IsPosition ? transaction.PosOpenPrice : transaction.Price;
        }

        protected override double? GetRemainingQuntity(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ? (double?)null : IsPosition ? (transaction.PositionLeavesQuantity / LotSize) : (transaction.LeavesQuantity / LotSize);
        }
    }

    class CashTransactionModel : TransactionReport
    {
        public CashTransactionModel(TradeReportEntity transaction, SymbolModel symbol) : base(transaction, symbol)
        {
            if (GetTransactionSide(transaction) == TransactionSide.Buy)
            {
                AssetI = transaction.DstAssetMovement ?? 0;
                AssetICurrency = transaction.DstAssetCurrency;

                AssetII = transaction.SrcAssetMovement ?? 0;
                AssetIICurrency = transaction.SrcAssetCurrency;
            }
            else if (GetTransactionSide(transaction) == TransactionSide.Sell)
            {
                AssetII = transaction.DstAssetMovement ?? 0;
                AssetIICurrency = transaction.DstAssetCurrency;

                AssetI = transaction.SrcAssetMovement ?? 0;
                AssetICurrency = transaction.SrcAssetCurrency;
            }
            else if (IsBalanceTransaction)
            {
                AssetI = transaction.TransactionAmount;
                AssetICurrency = transaction.TransactionCurrency;

                AssetII = 0;
                AssetIICurrency = "";
            }
        }

        public double AssetI { get; private set; }
        public double AssetII { get; private set; }
        public string AssetICurrency { get; set; }
        public string AssetIICurrency { get; set; }

        protected override string GetCommissionCurrency(TradeReportEntity transaction)
        {
            switch (GetTransactionSide(transaction))
            {
                case TransactionSide.Sell:
                case TransactionSide.Buy:
                    return transaction.DstAssetCurrency;
                case TransactionSide.None: return "";
                default: throw new NotSupportedException(GetTransactionSide(transaction).ToString());
            }
        }

        protected override double? GetClosePrice(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ? (double?)null : transaction.OrderFillPrice;
        }

        protected override double? GetCloseQuantity(TradeReportEntity transaction)
        {
            return IsBalanceTransaction ? (double?) null : transaction.OrderLastFillAmount;
        }
    }
}
