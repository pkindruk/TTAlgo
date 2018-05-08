﻿using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TickTrader.Algo.Api;
using TickTrader.Algo.Common.Info;
using TickTrader.Algo.Common.Model.Config;
using TickTrader.Algo.Common.Model.Setup;
using TickTrader.Algo.Core;
using TickTrader.Algo.Core.Metadata;

namespace TickTrader.BotTerminal
{
    public enum PluginSetupMode
    {
        New,
        Edit,
    }


    public abstract class PluginSetupViewModel : PropertyChangedBase
    {
        private List<PropertySetupViewModel> _allProperties;
        private List<ParameterSetupViewModel> _parameters;
        private List<InputSetupViewModel> _barBasedInputs;
        private List<InputSetupViewModel> _tickBasedInputs;
        private List<OutputSetupViewModel> _outputs;
        private TimeFrames _selectedTimeFrame;
        private SymbolInfo _mainSymbol;
        private MappingInfo _selectedMapping;
        private string _instanceId;
        private PluginPermissions _permissions;
        private IPluginIdProvider _idProvider;


        public IEnumerable<TimeFrames> AvailableTimeFrames { get; private set; }

        public abstract bool AllowChangeTimeFrame { get; }

        public TimeFrames SelectedTimeFrame
        {
            get { return _selectedTimeFrame; }
            set
            {
                if (_selectedTimeFrame == value)
                    return;

                var changeInputs = _selectedTimeFrame == TimeFrames.Ticks || value == TimeFrames.Ticks;
                _selectedTimeFrame = value;
                NotifyOfPropertyChange(nameof(SelectedTimeFrame));
                if (changeInputs)
                {
                    NotifyOfPropertyChange(nameof(Inputs));
                    NotifyOfPropertyChange(nameof(HasInputs));
                }
            }
        }

        public IReadOnlyList<SymbolInfo> AvailableSymbols { get; private set; }

        public abstract bool AllowChangeMainSymbol { get; }

        public SymbolInfo MainSymbol
        {
            get { return _mainSymbol; }
            set
            {
                if (_mainSymbol == value)
                    return;

                _mainSymbol = value;
                NotifyOfPropertyChange(nameof(MainSymbol));
            }
        }

        public IReadOnlyList<MappingInfo> AvailableMappings { get; private set; }

        public abstract bool AllowChangeMapping { get; }

        public MappingInfo SelectedMapping
        {
            get { return _selectedMapping; }
            set
            {
                if (_selectedMapping == value)
                    return;

                _selectedMapping = value;
                NotifyOfPropertyChange(nameof(SelectedMapping));
            }
        }


        public IEnumerable<ParameterSetupViewModel> Parameters => _parameters;

        public IEnumerable<InputSetupViewModel> Inputs => ActiveInputs;

        public IEnumerable<OutputSetupViewModel> Outputs => _outputs;

        public bool HasInputsOrParams => HasParams || HasInputs;

        public bool HasParams => _parameters.Count > 0;

        public bool HasInputs => ActiveInputs.Count > 0;

        public bool HasOutputs => _outputs.Count > 0;

        public bool HasDescription => !string.IsNullOrWhiteSpace(Descriptor?.Description);

        public PluginDescriptor Descriptor { get; }

        public PluginInfo Plugin { get; }

        public bool IsValid { get; private set; }

        public bool IsEmpty { get; private set; }

        public SetupMetadata SetupMetadata { get; }

        public PluginSetupMode Mode { get; }

        public bool IsEditMode => Mode == PluginSetupMode.Edit;

        public bool CanBeSkipped => IsEmpty && Descriptor.IsValid && Descriptor.Type != AlgoTypes.Robot;

        public string InstanceId
        {
            get { return _instanceId; }
            set
            {
                if (_instanceId == value)
                    return;

                _instanceId = value;
                NotifyOfPropertyChange(nameof(InstanceId));
                NotifyOfPropertyChange(nameof(IsInstanceIdValid));
                Validate();
            }
        }

        public bool IsInstanceIdValid => Mode == PluginSetupMode.Edit ? true : _idProvider.IsValidPluginId(Descriptor, InstanceId);

        public PluginPermissions Permissions
        {
            get { return _permissions; }
            set
            {
                if (_permissions == value)
                    return;

                _permissions = value;
                NotifyOfPropertyChange(nameof(Permissions));
                NotifyOfPropertyChange(nameof(AllowTrade));
                NotifyOfPropertyChange(nameof(Isolated));
            }
        }

        public bool AllowTrade
        {
            get { return Permissions.TradeAllowed; }
            set
            {
                if (Permissions.TradeAllowed == value)
                    return;

                Permissions.TradeAllowed = value;
                NotifyOfPropertyChange(nameof(AllowTrade));
            }
        }

        public bool Isolated
        {
            get { return Permissions.Isolated; }
            set
            {
                if (Permissions.Isolated == value)
                    return;

                Permissions.Isolated = value;
                NotifyOfPropertyChange(nameof(Isolated));
            }
        }


        private List<InputSetupViewModel> ActiveInputs => _selectedTimeFrame == TimeFrames.Ticks ? _tickBasedInputs : _barBasedInputs;


        public event System.Action ValidityChanged = delegate { };


        public PluginSetupViewModel(PluginInfo plugin, SetupMetadata setupMetadata, IPluginIdProvider idProvider, PluginSetupMode mode)
        {
            Plugin = plugin;
            Descriptor = plugin.Descriptor;
            SetupMetadata = setupMetadata;
            _idProvider = idProvider;
            Mode = mode;
        }


        public virtual void Load(PluginConfig cfg)
        {
            SelectedTimeFrame = cfg.TimeFrame;
            MainSymbol = AvailableSymbols.GetSymbolOrAny(cfg.MainSymbol);
            SelectedMapping = SetupMetadata.Mappings.GetBarToBarMappingOrDefault(cfg.SelectedMapping);
            InstanceId = cfg.InstanceId;
            Permissions = cfg.Permissions.Clone();
            foreach (var scrProperty in cfg.Properties)
            {
                var thisProperty = _allProperties.FirstOrDefault(p => p.Id == scrProperty.Id);
                if (thisProperty != null)
                    thisProperty.Load(scrProperty);
            }
        }

        public virtual PluginConfig Save()
        {
            var cfg = SaveToConfig();
            cfg.TimeFrame = SelectedTimeFrame;
            cfg.MainSymbol = MainSymbol.Name;
            cfg.SelectedMapping = SelectedMapping.Key;
            cfg.InstanceId = InstanceId;
            cfg.Permissions = Permissions.Clone();
            foreach (var property in _allProperties)
                cfg.Properties.Add(property.Save());
            return cfg;
        }

        public virtual void Reset()
        {
            SelectedTimeFrame = SetupMetadata.Context.DefaultTimeFrame;
            MainSymbol = AvailableSymbols.GetSymbolOrAny(SetupMetadata.Context.DefaultSymbolCode);
            SelectedMapping = SetupMetadata.Mappings.GetBarToBarMappingOrDefault(SetupMetadata.Context.DefaultMapping);
            InstanceId = _idProvider.GeneratePluginId(Descriptor);

            _parameters.ForEach(p => p.Reset());
            foreach (var p in _allProperties)
                p.Reset();
        }

        public void Validate()
        {
            IsValid = CheckValidity();
            ValidityChanged();
        }


        protected abstract PluginConfig SaveToConfig();


        protected virtual bool CheckValidity()
        {
            return Descriptor.Error == AlgoMetadataErrors.None && _allProperties.All(p => !p.HasError) && IsInstanceIdValid;
        }


        protected void Init()
        {
            AvailableTimeFrames = SetupMetadata.Api.TimeFrames;
            AvailableSymbols = SetupMetadata.Account.GetAvaliableSymbols(SetupMetadata.Context.DefaultSymbolCode);
            AvailableMappings = SetupMetadata.Mappings.BarToBarMappings;

            _parameters = Descriptor.Parameters.Select(CreateParameter).ToList();
            _barBasedInputs = Descriptor.Inputs.Select(CreateBarBasedInput).ToList();
            _tickBasedInputs = Descriptor.Inputs.Select(CreateTickBasedInput).ToList();
            _outputs = Descriptor.Outputs.Select(CreateOutput).ToList();

            _allProperties = _parameters.Concat<PropertySetupViewModel>(_barBasedInputs).Concat(_tickBasedInputs).Concat(_outputs).ToList();
            _allProperties.ForEach(p => p.ErrorChanged += s => Validate());

            IsEmpty = _allProperties.Count == 0 && !Descriptor.SetupMainSymbol;

            Reset();
            Validate();
        }


        private ParameterSetupViewModel CreateParameter(ParameterDescriptor descriptor)
        {
            if (!descriptor.IsValid)
                return new ParameterSetupViewModel.Invalid(descriptor);

            if (descriptor.IsEnum)
                return new EnumParamSetupViewModel(descriptor);
            if (descriptor.DataType == ParameterSetupViewModel.NullableIntTypeName)
                return new NullableIntParamSetupModel(descriptor);
            if (descriptor.DataType == ParameterSetupViewModel.NullableDoubleTypeName)
                return new NullableDoubleParamSetupModel(descriptor);

            switch (descriptor.DataType)
            {
                case "System.Boolean": return new BoolParamSetupViewModel(descriptor);
                case "System.Int32": return new IntParamSetupViewModel(descriptor);
                case "System.Double": return new DoubleParamSetupViewModel(descriptor);
                case "System.String": return new StringParamSetupViewModel(descriptor);
                case "TickTrader.Algo.Api.File": return new FileParamSetupViewModel(descriptor);
                default: return new ParameterSetupViewModel.Invalid(descriptor, ErrorMsgCodes.UnsupportedParameterType);
            }
        }

        private InputSetupViewModel CreateBarBasedInput(InputDescriptor descriptor)
        {
            if (!descriptor.IsValid)
                return new InputSetupViewModel.Invalid(descriptor);

            switch (descriptor.DataSeriesBaseTypeFullName)
            {
                case "System.Double": return new BarToDoubleInputSetupViewModel(descriptor, SetupMetadata);
                case "TickTrader.Algo.Api.Bar": return new BarToBarInputSetupViewModel(descriptor, SetupMetadata);
                //case "TickTrader.Algo.Api.Quote": return new QuoteInputSetupModel(descriptor, Metadata, DefaultSymbolCode, false);
                //case "TickTrader.Algo.Api.QuoteL2": return new QuoteInputSetupModel(descriptor, Metadata, DefaultSymbolCode, true);
                default: return new InputSetupViewModel.Invalid(descriptor, ErrorMsgCodes.UnsupportedInputType);
            }
        }

        private InputSetupViewModel CreateTickBasedInput(InputDescriptor descriptor)
        {
            if (!descriptor.IsValid)
                return new InputSetupViewModel.Invalid(descriptor);

            switch (descriptor.DataSeriesBaseTypeFullName)
            {
                case "System.Double": return new QuoteToDoubleInputSetupViewModel(descriptor, SetupMetadata);
                case "TickTrader.Algo.Api.Bar": return new QuoteToBarInputSetupViewModel(descriptor, SetupMetadata);
                case "TickTrader.Algo.Api.Quote": return new QuoteInputSetupViewModel(descriptor, SetupMetadata, false);
                case "TickTrader.Algo.Api.QuoteL2": return new QuoteInputSetupViewModel(descriptor, SetupMetadata, true);
                default: return new InputSetupViewModel.Invalid(descriptor, ErrorMsgCodes.UnsupportedInputType);
            }
        }

        private OutputSetupViewModel CreateOutput(OutputDescriptor descriptor)
        {
            if (!descriptor.IsValid)
                return new OutputSetupViewModel.Invalid(descriptor);

            switch (descriptor.DataSeriesBaseTypeFullName)
            {
                case "System.Double": return new ColoredLineOutputSetupViewModel(descriptor);
                case "TickTrader.Algo.Api.Marker": return new MarkerSeriesOutputSetupViewModel(descriptor);
                default: return new OutputSetupViewModel.Invalid(descriptor, ErrorMsgCodes.UnsupportedOutputType);
            }
        }
    }
}
