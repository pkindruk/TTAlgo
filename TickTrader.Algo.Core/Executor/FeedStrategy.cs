﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickTrader.Algo.Core
{
    [Serializable]
    public abstract class FeedStrategy : IFeedFixtureContext
    {
        private SubscriptionManager dispenser;
        private Dictionary<string, SubscriptionFixture> userSubscriptions = new Dictionary<string, SubscriptionFixture>();

        public FeedStrategy(IPluginFeedProvider feed)
        {
            if (feed == null)
                throw new ArgumentNullException("feed");

            this.dispenser = new SubscriptionManager(feed);
            this.Feed = feed;

            feed.FeedUpdated += Feed_FeedUpdated;
        }

        internal IFixtureContext ExecContext { get; private set; }
        internal IPluginFeedProvider Feed { get; private set; }

        public abstract int BufferSize { get; }
        public abstract ITimeRef TimeRef { get; }

        internal abstract void OnInit();
        protected abstract BufferUpdateResults UpdateBuffers(FeedUpdate update);
        internal abstract void MapInput<TSrc, TVal>(string inputName, string symbolCode, Func<TSrc, TVal> selector);
        internal abstract void Stop();

        public void OnUserSubscribe(string symbolCode, int depth)
        {
            SubscriptionFixture fixture;
            if (userSubscriptions.TryGetValue(symbolCode, out fixture))
                dispenser.Remove(fixture);
            fixture = new SubscriptionFixture(this, symbolCode, depth);
            userSubscriptions[symbolCode] = fixture;
            dispenser.Add(fixture);
        }

        internal void Init(IFixtureContext executor)
        {
            ExecContext = executor;
            OnInit();
        }

        internal virtual void Start()
        {
            ExecContext.PluginInvoke(b => BatchBuild(BufferSize));
        }

        private void BatchBuild(int x)
        {
            var builder = ExecContext.Builder;

            builder.StartBatch();

            for (int i = 0; i < x; i++)
            {
                builder.IncreaseVirtualPosition();
                builder.InvokeCalculate(false);
            }

            builder.StopBatch();
        }

        private void Feed_FeedUpdated(FeedUpdate[] updates)
        {
            ExecContext.PluginInvoke(b =>
            {
                foreach (var update in updates)
                    UpdateBuild(update);
            });
        }

        private void UpdateBuild(FeedUpdate update)
        {
            var result = UpdateBuffers(update);
            if (result == BufferUpdateResults.Extended)
            {
                ExecContext.Builder.IncreaseVirtualPosition();
                ExecContext.Builder.InvokeCalculate(false);
                dispenser.OnBufferUpdated(update.Quote);
                dispenser.OnUpdateEvent(update.Quote);
            }
            else if (result == BufferUpdateResults.LastItemUpdated)
            {
                ExecContext.Builder.InvokeCalculate(true);
                dispenser.OnBufferUpdated(update.Quote);
                dispenser.OnUpdateEvent(update.Quote);
            }

            ExecContext.Builder.InvokeOnQuote(update.Quote);
        }

        #region IFeedStrategyContext

        IFixtureContext IFeedFixtureContext.ExecContext { get { return ExecContext; } }
        IPluginFeedProvider IFeedFixtureContext.Feed { get { return Feed; } }

        void IFeedFixtureContext.Add(IFeedFixture subscriber)
        {
            dispenser.Add(subscriber);
        }

        void IFeedFixtureContext.Remove(IFeedFixture subscriber)
        {
            dispenser.Remove(subscriber);
        }

        #endregion IFeedStrategyContext
    }
}
