﻿using ActorSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TickTrader.Algo.Domain;

namespace TickTrader.FeedStorage.Api
{
    public interface ISymbolCatalog
    {
        ISymbolData this[ISymbolKey name] { get; }

        IReadOnlyList<ISymbolData> AllSymbols { get; }


        ISymbolCollection<ISymbolInfo> OnlineCollection { get; }

        ISymbolCollection<ICustomInfo> CustomCollection { get; }


        Task<ISymbolCatalog> ConnectClient(IOnlineStorageSettings settings);

        Task DisconnectClient();


        Task<ISymbolCatalog> OpenCustomStorage(ICustomStorageSettings settings);

        Task CloseCustomStorage();


        Task CloseCatalog();


        Task<ActorChannel<SliceInfo>> DownloadBarSeriesToStorage(string symbol, Feed.Types.Timeframe timeframe, Feed.Types.MarketSide marketSide, DateTime from, DateTime to);

        Task<ActorChannel<SliceInfo>> DownloadTickSeriesToStorage(string symbol, Feed.Types.Timeframe timeframe, DateTime from, DateTime to);
    }


    public interface ISymbolCollection<T>
    {
        string StorageFolder { get; }

        ISymbolData this[string name] { get; }


        List<ISymbolData> Symbols { get; }


        Task<bool> TryAddSymbol(T symbol);

        Task<bool> TryUpdateSymbol(T symbol);

        Task<bool> TryRemoveSymbol(ISymbolKey symbol);


        event Action<ISymbolData> SymbolAdded;

        event Action<ISymbolData> SymbolRemoved;

        event Action<ISymbolData, ISymbolData> SymbolUpdated;
    }
}
