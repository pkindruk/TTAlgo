﻿using ActorSharp;
using ActorSharp.Lib;
using Machinarium.Qnil;
using System;
using System.Linq;
using System.Threading.Tasks;
using TickTrader.Algo.Domain;
using TickTrader.SeriesStorage;
using TickTrader.SeriesStorage.ProtoSerializer;

namespace TickTrader.FeedStorage
{
    internal sealed class CustomFeedStorage : FeedStorageBase
    {
        private const string CustomSymbolsCollectionName = "customSymbols";

        private readonly ActorEvent<DictionaryUpdateArgs<string, CustomInfo>> _symbolChangeListeners = new ActorEvent<DictionaryUpdateArgs<string, CustomInfo>>();

        private VarDictionary<string, CustomInfo> _commonSymbols = new VarDictionary<string, CustomInfo>();
        private ICollectionStorage<Guid, CustomInfo> _customSymbolsCollection;


        public CustomFeedStorage() : base()
        {
            _commonSymbols.Updated += SendUpdatesToListeners;
        }


        protected override void Start(string dbFolder)
        {
            base.Start(dbFolder);

            _customSymbolsCollection = Database.GetCollection(CustomSymbolsCollectionName, new GuidKeySerializer(), new ProtoValueSerializer<CustomInfo>());
            _commonSymbols.Clear();


            foreach (var entry in _customSymbolsCollection.Iterate(Guid.Empty))
            {
                var smb = entry.Value;
                smb.StorageId = entry.Key;

                _commonSymbols.Add(smb.Name, smb);
            }
        }

        protected override void Stop()
        {
            _commonSymbols.Updated -= SendUpdatesToListeners;
            _customSymbolsCollection.Dispose();
            _customSymbolsCollection = null;

            base.Stop();
        }

        protected override bool IsSpecialCollection(string name) => name == CustomSymbolsCollectionName;

        public bool Add(CustomInfo newSymbol)
        {
            if (_commonSymbols.ContainsKey(newSymbol.Name))
                return false;

            newSymbol.StorageId = Guid.NewGuid();
            _customSymbolsCollection.Write(newSymbol.StorageId, newSymbol);
            _commonSymbols.Add(newSymbol.Name, newSymbol);

            return true;
        }

        private void Update(CustomInfo symbolCfg)
        {
            CheckState();

            var oldEntity = _commonSymbols[symbolCfg.Name];

            if (oldEntity.Name != symbolCfg.Name)
                throw new ArgumentException("Changing name is not supported!");

            symbolCfg.StorageId = oldEntity.StorageId;
            _customSymbolsCollection.Write(oldEntity.StorageId, symbolCfg);
            _commonSymbols[symbolCfg.Name] = symbolCfg;
        }

        private Task RemoveAsync(string symbol)
        {
            return Task.Factory.StartNew(() => Remove(symbol));
        }

        private void Remove(string symbol)
        {
            CheckState();

            if (_commonSymbols.TryGetValue(symbol, out CustomInfo smbEntity))
            {
                var toRemove = Keys.Snapshot.Where(k => k.Symbol == symbol).ToList();

                // clear cache 
                toRemove.ForEach(u => RemoveSeries(u));

                // remove symbol
                _customSymbolsCollection.Remove(smbEntity.StorageId);
                _commonSymbols.Remove(symbol);
            }
        }

        private void SendUpdatesToListeners(DictionaryUpdateArgs<string, CustomInfo> update) => _symbolChangeListeners.FireAndForget(update);


        internal sealed class Handler : FeedHandler
        {
            private readonly ActorCallback<DictionaryUpdateArgs<string, CustomInfo>> _smbChangedCallback;
            private readonly Ref<CustomFeedStorage> _ref;


            public Handler(Ref<CustomFeedStorage> actorRef) : base(actorRef.Cast<CustomFeedStorage, FeedStorageBase>())
            {
                _smbChangedCallback = ActorCallback.Create<DictionaryUpdateArgs<string, CustomInfo>>(UpdateCollectionHandler);
                _ref = actorRef;
            }

            public override async Task SyncData()
            {
                await base.SyncData();

                var snapshot = await _ref.Call(a =>
                {
                    a._symbolChangeListeners.Add(_smbChangedCallback);
                    return a._commonSymbols.Values.ToList();
                });

                snapshot.ForEach(AddNewCustomSymbol);
            }

            public override Task<bool> TryAddSymbol(ISymbolInfo symbol)
            {
                return _ref.Call(a => a.Add(CustomInfo.ToData(symbol)));
            }

            public override Task<bool> TryUpdateSymbol(ISymbolInfo symbol)
            {
                throw new NotImplementedException();
            }

            public override Task<bool> TryRemoveSymbol(string name)
            {
                throw new NotImplementedException();
            }


            public override void Dispose()
            {
                _ref.Send(a => a._symbolChangeListeners.Remove(_smbChangedCallback));

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
