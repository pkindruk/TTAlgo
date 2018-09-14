﻿using System.Runtime.Serialization;

namespace TickTrader.Algo.Common.Model.Config.Version1
{
    [DataContract(Name = "Input", Namespace = "")]
    public abstract class Input : Property
    {
        [DataMember]
        public string SelectedSymbol { get; set; }
    }


    [DataContract(Name = "QuoteToQuoteInput", Namespace = "")]
    public class QuoteToQuoteInput : Input
    {
        [DataMember]
        public bool UseL2 { get; set; }
    }


    public enum QuoteToDoubleMappings { Ask, Bid, Median }


    [DataContract(Name = "QuoteToDoubleInput", Namespace = "")]
    public class QuoteToDoubleInput : Input
    {
        [DataMember]
        public QuoteToDoubleMappings Mapping { get; set; }
    }


    [DataContract(Name = "BarInputBase", Namespace = "")]
    public abstract class BarInputBase : Input
    {
    }


    public enum BarPriceType
    {
        Bid = 1,
        Ask = 2,
    }


    [DataContract(Name = "SingleBarInputBase", Namespace = "")]
    public abstract class SingleBarInputBase : BarInputBase
    {
        [DataMember]
        public BarPriceType PriceType { get; set; }
    }


    [DataContract(Name = "BarToBarInput", Namespace = "")]
    public class BarToBarInput : BarInputBase
    {
        [DataMember]
        public string SelectedMapping { get; set; }
    }


    [DataContract(Name = "BarToDoubleInput", Namespace = "")]
    public class BarToDoubleInput : BarInputBase
    {
        [DataMember]
        public string SelectedMapping { get; set; }
    }


    [DataContract(Name = "QuoteToBarInput", Namespace = "")]
    public class QuoteToBarInput : SingleBarInputBase
    {
    }
}
