﻿using System;
using System.Collections.Generic;
using TickTrader.Algo.Core.Lib;

namespace TickTrader.Algo.Core
{
    public class CommonCdProxy : CrossDomainObject, IAccountInfoProvider, IPluginMetadata, ITradeHistoryProvider
    {
        private IAccountInfoProvider _acc;
        private IPluginMetadata _meta;
        private ITradeHistoryProvider _tHistory;

        public CommonCdProxy(IAccountInfoProvider acc, IPluginMetadata meta, ITradeHistoryProvider tHistory)
        {
            _acc = acc;
            _meta = meta;
            _tHistory = tHistory;

            _acc.OrderUpdated += Acc_OrderUpdated;
            _acc.BalanceUpdated += Acc_BalanceUpdated;
            _acc.PositionUpdated += Acc_PositionUpdated;
        }

        public override void Dispose()
        {
            base.Dispose();

            _acc.OrderUpdated -= Acc_OrderUpdated;
            _acc.BalanceUpdated -= Acc_BalanceUpdated;
            _acc.PositionUpdated -= Acc_PositionUpdated;
        }

        #region ITradeHistoryProvider

        public IAsyncPagedEnumerator<Domain.TradeReportInfo> GetTradeHistory(DateTime? from, DateTime? to, Domain.TradeHistoryRequestOptions options)
        {
            return _tHistory.GetTradeHistory(from, to, options);
        }

        #endregion

        #region IPluginMetadata

        public IEnumerable<Domain.CurrencyInfo> GetCurrencyMetadata()
        {
            return _meta.GetCurrencyMetadata();
        }

        public IEnumerable<Domain.SymbolInfo> GetSymbolMetadata()
        {
            return _meta.GetSymbolMetadata();
        }

        public IEnumerable<Domain.FullQuoteInfo> GetLastQuoteMetadata()
        {
            return _meta.GetLastQuoteMetadata();
        }

        #endregion

        #region IAccountInfoProvider

        public event Action<Domain.OrderExecReport> OrderUpdated;
        public event Action<Domain.BalanceOperation> BalanceUpdated;
        public event Action<Domain.PositionExecReport> PositionUpdated;

        public Domain.AccountInfo GetAccountInfo()
        {
            return _acc.GetAccountInfo();
        }

        public List<Domain.OrderInfo> GetOrders()
        {
            return _acc.GetOrders();
        }

        public List<Domain.PositionInfo> GetPositions()
        {
            return _acc.GetPositions();
        }

        public void SyncInvoke(Action action)
        {
            _acc.SyncInvoke(action);
        }

        private void Acc_BalanceUpdated(Domain.BalanceOperation rep)
        {
            BalanceUpdated?.Invoke(rep);
        }

        private void Acc_OrderUpdated(Domain.OrderExecReport rep)
        {
            OrderUpdated?.Invoke(rep);
        }

        private void Acc_PositionUpdated(Domain.PositionExecReport rep)
        {
            PositionUpdated?.Invoke(rep);
        }

        #endregion
    }
}
