﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.Algo.Api;
using TickTrader.Algo.Api.Math;

namespace TickTrader.Algo.TestCollection.Auto.Tests
{
    public class OrderTemplate : TestParamsSet
    {
        private string _id;


        public Order RealOrder => Orders[Id];

        public bool IsCloseOrder => RealOrder.Type == OrderType.Position || RealOrder.Type == OrderType.Market;

        public bool IsStopOrder => Type == OrderType.Stop || Type == OrderType.StopLimit;

        public bool IsImmediateFill => (Type == OrderType.Market) || (Type == OrderType.Limit && Options.HasFlag(OrderExecOptions.ImmediateOrCancel));



        public double SlippagePrecision { get; }

        public bool IsIoc { get; }

        public OrderType InitType { get; protected set; }

        public string RelatedId { get; private set; }

        public string Id
        {
            get => _id;
            set
            {
                RelatedId = _id;
                _id = value;
            }
        }

        public OrderExecutionMode Mode { get; set; }


        public double? Price { get; set; }

        public double? StopPrice { get; set; }

        public double? MaxVisibleVolume { get; set; }

        public double? SL { get; set; }

        public double? TP { get; set; }

        public double? Volume { get; set; }

        public double? Slippage { get; set; }

        public DateTime? Expiration { get; set; }

        public string Comment { get; set; }


        public OrderTemplate() { }

        public OrderTemplate(TestParamsSet test, OrderExecutionMode mode) : base(test.Type, test.Side, test.Async)
        {
            Mode = mode;
            Options = test.Options;
            IsIoc = Type == OrderType.Limit && Options.HasFlag(OrderExecOptions.ImmediateOrCancel);

            InitType = Type;
            SlippagePrecision = Math.Pow(10, Math.Max(Symbol.Digits, 4));
        }

        public void UpdateTemplate(Order order, bool activate = false, bool position = false)
        {
            Id = order.Id;

            if (activate && Type == OrderType.StopLimit)
                Type = OrderType.Limit;
            else
                Type = position ? OrderType.Position : Type;

            Verification();
        }

        public void Verification(bool close = false)
        {
            if (close || IsImmediateFill || Type == OrderType.Position) //to do: create verification for Position
                return;

            if (RealOrder.IsNull != close)
                throw new VerificationException(Id, close);

            if (RealOrder.Type != Type)
                ThrowVerificationException(nameof(RealOrder.Type), Type, RealOrder.Type);

            if (RealOrder.Side != Side)
                ThrowVerificationException(nameof(RealOrder.Side), Side, RealOrder.Side);

            if (!RealOrder.RemainingVolume.EI(Volume))
                ThrowVerificationException(nameof(RealOrder.RemainingVolume), Volume, RealOrder.RemainingVolume);

            if (Type != OrderType.Stop && !CheckPrice(RealOrder.Price))
                ThrowVerificationException(nameof(RealOrder.Price), Price, RealOrder.Price);

            if (IsStopOrder && !RealOrder.StopPrice.EI(StopPrice))
                ThrowVerificationException(nameof(RealOrder.StopPrice), StopPrice, RealOrder.StopPrice);

            if (!RealOrder.MaxVisibleVolume.EI(MaxVisibleVolume) && !(-1.0).EI(MaxVisibleVolume) && !double.IsNaN(RealOrder.MaxVisibleVolume))
                ThrowVerificationException(nameof(RealOrder.MaxVisibleVolume), MaxVisibleVolume, RealOrder.MaxVisibleVolume);

            if (!RealOrder.TakeProfit.EI(TP) && !0.0.EI(TP) && !double.IsNaN(RealOrder.TakeProfit))
                ThrowVerificationException(nameof(RealOrder.TakeProfit), TP, RealOrder.TakeProfit);

            if (!RealOrder.StopLoss.EI(SL) && !0.0.EI(SL) && !double.IsNaN(RealOrder.StopLoss))
                ThrowVerificationException(nameof(RealOrder.StopLoss), SL, RealOrder.StopLoss);

            if (IsSlippageSupported)
                CheckSlippage(RealOrder.Slippage, (realSlippage, expectedSlippage) =>
                {
                    if (!realSlippage.E(expectedSlippage))
                        ThrowVerificationException(nameof(RealOrder.Slippage), expectedSlippage, realSlippage);
                });

            if (Comment != null && RealOrder.Comment != Comment)
                ThrowVerificationException(nameof(RealOrder.Comment), Comment, RealOrder.Comment);

            if (Expiration != null && RealOrder.Expiration != Expiration)
                ThrowVerificationException(nameof(RealOrder.Expiration), Expiration, RealOrder.Expiration);


            if (RealOrder.Tag != Tag)
                ThrowVerificationException(nameof(RealOrder.Tag), Tag, RealOrder.Tag);
        }

        public double? GetSlippageInPercent()
        {
            if (Slippage != null)
                Slippage = SlippageConverters.SlippagePipsToFractions(Slippage.Value, (IsStopOrder ? StopPrice : Price).Value, Symbol);

            return Slippage;
        }

        public string GetInfo(TestPropertyAction action, string property) => $"{action} {property}{GetAllProperties()}";

        public string GetInfo(TestOrderAction action) => action != TestOrderAction.Open ? $"{action}{GetAllProperties()}" : base.GetInfo();

        public string GetAction(TestPropertyAction action, string property) => $"{(Async ? "Async " : "")}{action} {property} {Type} {(Options == OrderExecOptions.ImmediateOrCancel ? "IoC" : "")} {Side} to order {Id}";

        private void ThrowVerificationException(string property, object expected, object current) => throw new VerificationException(RealOrder.Id, property, expected, current);

        private bool CheckPrice(double cur)
        {
            if (IsImmediateFill || Type == OrderType.Position)
                return Side == OrderSide.Buy ? cur.LteI(Price) : cur.GteI(Price);

            return cur.EI(Price);
        }

        protected double CeilSlippage(double slippage) => Math.Round(Math.Ceiling(slippage * SlippagePrecision) / SlippagePrecision, Symbol.Digits);

        protected double GetMaxSlippage() => SlippageConverters.SlippagePipsToFractions(Symbol.Slippage, (IsStopOrder ? StopPrice : Price).Value, Symbol);

        protected void CheckSlippage(double serverSlippage, Action<double, double> comparer)
        {
            var calcSlippage = GetMaxSlippage();
            var expectedSlippage = CeilSlippage(Slippage == null || Slippage.Value > calcSlippage ? calcSlippage : Slippage.Value);

            comparer(expectedSlippage, CeilSlippage(serverSlippage));
        }

        private string GetAllProperties()
        {
            var str = new StringBuilder(1 << 10);

            if (Id != null)
                str.Append($" order {Id}");

            SetProperty(str, Price, nameof(Price));
            SetProperty(str, Volume, nameof(Volume));
            SetProperty(str, Side, nameof(Side));
            SetProperty(str, InitType, nameof(Type)); //InitType as Type
            SetProperty(str, StopPrice, nameof(StopPrice));
            SetProperty(str, MaxVisibleVolume, nameof(MaxVisibleVolume));
            SetProperty(str, SL, nameof(SL));
            SetProperty(str, TP, nameof(TP));
            SetProperty(str, Slippage, nameof(Slippage));

            SetProperty(str, Expiration, nameof(Expiration));
            SetProperty(str, Comment, nameof(Comment));

            str.Append($", options={Options}");
            str.Append($", async={Async}.");

            return str.ToString();
        }

        private static void SetProperty(StringBuilder str, double? value, string name)
        {
            if (value != null && !double.IsNaN(value.Value) && !value.Value.LteI(0.0))
                str.Append($", {name}={value.Value}");
        }

        private static void SetProperty(StringBuilder str, object value, string name)
        {
            if (value != null && !string.IsNullOrEmpty(value as string))
                str.Append($", {name}={value}");
        }

        public OrderTemplate Copy() => (OrderTemplate)MemberwiseClone();

        public OrderTemplate InversedCopy(double? volume = null)
        {
            var copy = Copy();

            copy.Side = Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
            copy.Volume = volume ?? Volume;

            return copy;
        }
    }
}
