﻿using System.Runtime.Serialization;

namespace TickTrader.Algo.Common.Model.Config
{
    [DataContract(Name = "property", Namespace = "TTAlgo.Setup.ver2")]
    [KnownType(typeof(BoolParameter))]
    [KnownType(typeof(IntParameter))]
    [KnownType(typeof(NullableIntParameter))]
    [KnownType(typeof(DoubleParameter))]
    [KnownType(typeof(NullableDoubleParameter))]
    [KnownType(typeof(StringParameter))]
    [KnownType(typeof(EnumParameter))]
    [KnownType(typeof(FileParameter))]
    [KnownType(typeof(ColoredLineOutput))]
    [KnownType(typeof(MarkerSeriesOutput))]
    [KnownType(typeof(QuoteInput))]
    [KnownType(typeof(QuoteToDoubleInput))]
    [KnownType(typeof(BarToBarInput))]
    [KnownType(typeof(BarToDoubleInput))]
    [KnownType(typeof(QuoteToBarInput))]
    public abstract class Property
    {
        [DataMember(Name = "key")]
        public string Id { get; set; }
    }

    [DataContract(Name = "Parameter", Namespace = "TTAlgo.Setup.ver2")]
    public abstract class Parameter : Property
    {
        public abstract object ValObj { get; }
    }

    [DataContract(Name = "bool", Namespace = "TTAlgo.Setup.ver2")]
    public class BoolParameter : Parameter<bool>
    {
    }

    [DataContract(Name = "int", Namespace = "TTAlgo.Setup.ver2")]
    public class IntParameter : Parameter<int>
    {
    }

    [DataContract(Name = "nint", Namespace = "TTAlgo.Setup.ver2")]
    public class NullableIntParameter : Parameter<int?>
    {
    }

    [DataContract(Name = "double", Namespace = "TTAlgo.Setup.ver2")]
    public class DoubleParameter : Parameter<double>
    {
    }

    [DataContract(Name = "ndouble", Namespace = "TTAlgo.Setup.ver2")]
    public class NullableDoubleParameter : Parameter<double?>
    {
    }

    [DataContract(Name = "string", Namespace = "TTAlgo.Setup.ver2")]
    public class StringParameter : Parameter<string>
    {
    }

    [DataContract(Name = "enum", Namespace = "TTAlgo.Setup.ver2")]
    public class EnumParameter : Parameter<string>
    {
    }

    [DataContract(Name = "file", Namespace = "TTAlgo.Setup.ver2")]
    public class FileParameter : Parameter
    {
        [DataMember(Name = "fileName")]
        public string FileName { get; set; }

        public override object ValObj => FileName;
    }

    [DataContract(Namespace = "TTAlgo.Setup.ver2")]
    public class Parameter<T> : Parameter
    {
        [DataMember(Name = "value")]
        public T Value { get; set; }

        public override object ValObj => Value;
    }
}
