﻿using System;
using TickTrader.Algo.Common.Model.Config;
using TickTrader.Algo.Core;
using TickTrader.Algo.Core.Metadata;

namespace TickTrader.Algo.Common.Model.Setup
{
    public abstract class InputSetupModel : PropertySetupModel
    {
        private string _defaultSymbol;


        protected IAlgoSetupMetadata SetupMetadata { get; }

        protected IAlgoSetupContext SetupContext { get; }


        public InputMetadata Metadata { get; }

        public string SelectedSymbol { get; protected set; }


        private InputSetupModel(InputMetadata metadata, string defaultSymbolCode)
        {
            Metadata = metadata;
            _defaultSymbol = defaultSymbolCode;

            SetMetadata(metadata);
        }

        public InputSetupModel(InputMetadata metadata, IAlgoSetupMetadata setupMetadata, IAlgoSetupContext setupContext)
            : this(metadata, setupContext.DefaultSymbolCode)
        {
            SetupMetadata = setupMetadata;
            SetupContext = setupContext;
        }


        public override void Reset()
        {
            SelectedSymbol = _defaultSymbol;
        }


        protected virtual void LoadConfig(Input input)
        {
            SelectedSymbol = input.SelectedSymbol ?? _defaultSymbol;
        }

        protected virtual void SaveConfig(Input input)
        {
            input.Id = Id;
            input.SelectedSymbol = SelectedSymbol;
        }


        public class Invalid : InputSetupModel
        {
            public Invalid(InputMetadata descriptor, object error = null)
                : base(descriptor, null)
            {
                if (error == null)
                    Error = new ErrorMsgModel(descriptor.Error);
                else
                    Error = new ErrorMsgModel(error);
            }

            public Invalid(InputMetadata descriptor, string symbol, ErrorMsgModel error)
                : base(descriptor, symbol)
            {
                Error = error;
            }


            public override void Apply(IPluginSetupTarget target)
            {
                throw new Exception("Cannot configure invalid input!");
            }

            public override void Load(Property srcProperty)
            {
            }

            public override Property Save()
            {
                throw new Exception("Cannot save invalid input!");
            }

            public override void Reset()
            {
            }
        }
    }
}
