﻿using ActorSharp;
using ActorSharp.Lib;
using Machinarium.Qnil;
using System;
using System.Linq;
using System.Threading.Tasks;
using TickTrader.Algo.Core.Lib;
using TickTrader.Algo.Domain;
using TickTrader.FeedStorage.Serializers;
using TickTrader.SeriesStorage;
using TickTrader.SeriesStorage.ProtoSerializer;

namespace TickTrader.FeedStorage
{
    internal sealed class CustomFeedStorage : FeedStorageBase
    {
        private const string CustomSymbolsCollectionName = "customSymbols";

        private readonly ActorEvent<DictionaryUpdateArgs<string, CustomInfo>> _symbolListeners = new ActorEvent<DictionaryUpdateArgs<string, CustomInfo>>();
        private readonly VarDictionary<string, CustomInfo> _customSymbols = new VarDictionary<string, CustomInfo>();

        private ICollectionStorage<Guid, CustomInfo> _customSymbolsCollection;


        public CustomFeedStorage() : base()
        {
            _customSymbols.Updated += SendSymbolsUpdates;
        }


        protected override void LoadStoredData(string skippedCollection = null)
        {
            base.LoadStoredData(CustomSymbolsCollectionName);

            _customSymbolsCollection = Database.GetCollection(CustomSymbolsCollectionName, new GuidKeySerializer(), new ProtoValueSerializer<CustomInfo>());
            _customSymbols.Clear();

            foreach (var entry in _customSymbolsCollection.Iterate(Guid.Empty))
            {
                var smb = entry.Value;
                smb.StorageId = entry.Key;

                _customSymbols.Add(smb.Name, smb);
            }
        }


        protected override void CloseDatabase()
        {
            _customSymbols.Updated -= SendSymbolsUpdates;
            _customSymbolsCollection?.Dispose();
            _customSymbolsCollection = null;

            base.CloseDatabase();
        }


        public bool AddSymbol(CustomInfo newSymbol)
        {
            if (_customSymbols.ContainsKey(newSymbol.Name))
                return false;

            WriteSymbolToCollection(newSymbol);

            _customSymbols.Add(newSymbol.Name, newSymbol);

            return true;
        }

        private bool UpdateSymbol(CustomInfo newSymbol)
        {
            if (!_customSymbols.TryGetValue(newSymbol.Name, out var oldSymbol) || oldSymbol.Name != newSymbol.Name)
                return false;

            WriteSymbolToCollection(newSymbol, oldSymbol.StorageId);

            _customSymbols[oldSymbol.Name] = newSymbol;

            return true;
        }

        private bool RemoveSymbol(string symbolName)
        {
            if (!_customSymbols.TryGetValue(symbolName, out var smb))
                return false;

            Keys.Snapshot.Where(k => k.Symbol == symbolName).ForEach(u => RemoveSeries(u)); // clear cache

            _customSymbolsCollection.Remove(smb.StorageId); // remove symbol
            _customSymbols.Remove(symbolName);

            return true;
        }

        private void SendSymbolsUpdates(DictionaryUpdateArgs<string, CustomInfo> update) => _symbolListeners.FireAndForget(update);

        private void WriteSymbolToCollection(CustomInfo smb, Guid? key = null)
        {
            smb.StorageId = key ?? Guid.NewGuid();
            _customSymbolsCollection.Write(smb.StorageId, smb);
        }


        internal sealed class Handler : FeedHandler
        {
            private readonly ActorCallback<DictionaryUpdateArgs<string, CustomInfo>> _smbChangedCallback;
            private readonly Ref<CustomFeedStorage> _ref;


            public Handler(Ref<CustomFeedStorage> actorRef) : base(actorRef.Cast<CustomFeedStorage, FeedStorageBase>())
            {
                _smbChangedCallback = ActorCallback.Create<DictionaryUpdateArgs<string, CustomInfo>>(UpdateCollectionHandler);
                _ref = actorRef;
            }

            protected override async Task SyncSymbolCollection()
            {
                var snapshot = await _ref.Call(a =>
                {
                    a._symbolListeners.Add(_smbChangedCallback);
                    return a._customSymbols.Values.ToList();
                });

                snapshot.ForEach(AddNewCustomSymbol);
            }


            public override Task<bool> TryAddSymbol(ISymbolInfo symbol)
            {
                return _ref.Call(a => a.AddSymbol(CustomInfo.ToData(symbol)));
            }

            public override Task<bool> TryUpdateSymbol(ISymbolInfo symbol)
            {
                return _ref.Call(a => a.UpdateSymbol(CustomInfo.ToData(symbol)));
            }

            public override Task<bool> TryRemoveSymbol(string name)
            {
                return _ref.Call(a => a.RemoveSymbol(name));
            }


            public override void Dispose()
            {
                _ref.Send(a => a._symbolListeners.Remove(_smbChangedCallback));

                base.Dispose();
            }

            private void UpdateCollectionHandler(DictionaryUpdateArgs<string, CustomInfo> update)
            {
                switch (update.Action)
                {
                    case DLinqAction.Remove:
                        _symbols.Remove(update.OldItem.Name);
                        break;
                    case DLinqAction.Insert:
                    case DLinqAction.Replace:
                        AddNewCustomSymbol(update.NewItem);
                        break;
                    default:
                        break;
                }
            }

            private void AddNewCustomSymbol(CustomInfo data) => _symbols[data.Name] = new CustomSymbol(data, this);
        }
    }
}
