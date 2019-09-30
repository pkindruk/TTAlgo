﻿using Caliburn.Micro;
using Machinarium.Var;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.Algo.Common.Model;
using Machinarium.Qnil;
using BO = TickTrader.BusinessObjects;
using BL = TickTrader.BusinessLogic;
using TickTrader.Algo.Api;

namespace TickTrader.BotTerminal
{
    internal class SymbolCfgEditorViewModel : Screen, IWindowModel
    {
        private VarContext _varContext = new VarContext();

        public SymbolCfgEditorViewModel(CustomSymbol symbol, IObservableList<string> availableCurrencies, Predicate<string> symbolExistsFunc, bool createNew = false)
        {
            if (symbol == null)
            {
                IsEditMode = false;
                symbol = new CustomSymbol()
                {
                    Name = "Symbol1",
                    Description = "",
                    BaseCurr = "EUR",
                    ProfitCurr = "USD",
                    Digits = 5,
                    ContractSize = 1,
                    MinVolume = 0.01,
                    MaxVolume = 1000,
                    VolumeStep = 0.01,
                    Slippage = 0,

                    CommissionType = CommissionType.Percent,
                    Commission = 0.0018,
                    LimitsCommission = 0.0018,
                    MinCommission = 0.222,
                    MinCommissionCurr = "USD",

                    SwapEnabled = true,
                    SwapType = BO.SwapType.Points,
                    SwapSizeShort = 1.08,
                    SwapSizeLong = -4.38,
                    TripleSwap = false,
                    TripleSwapDay = 0,

                    ProfitMode = BO.ProfitCalculationModes.Forex,

                    MarginMode = BO.MarginCalculationModes.Forex,
                    MarginHedged = 0.5,
                    MarginCurrency = "EUR",
                    MarginFactor = 3,
                    StopOrderMarginReduction = 1,
                    HiddenLimitOrderMarginReduction = 1,
                };
            }
            else
            {
                IsEditMode = !createNew;
            }

            DisplayName = IsEditMode ? "Edit Symbol Settings" : "Add Custom Symbol";
            Error = _varContext.AddProperty<string>();
            AvailableCurrencies = availableCurrencies;

            Name = _varContext.AddValidable<string>(symbol.Name);
            Description = _varContext.AddValidable<string>(symbol.Description);
            BaseCurr = _varContext.AddValidable<string>(symbol.BaseCurr);
            ProfitCurr = _varContext.AddValidable<string>(symbol.ProfitCurr);
            Digits = _varContext.AddIntValidable(symbol.Digits);
            DigitsStr = _varContext.AddConverter(Digits, new StringToInt());
            ContractSize = _varContext.AddDoubleValidable(symbol.ContractSize);
            ContractSizeStr = _varContext.AddConverter(ContractSize, new StringToDouble());
            MinVolume = _varContext.AddDoubleValidable(symbol.MinVolume);
            MinVolumeStr = _varContext.AddConverter(MinVolume, new StringToDouble());
            MaxVolume = _varContext.AddDoubleValidable(symbol.MaxVolume);
            MaxVolumeStr = _varContext.AddConverter(MaxVolume, new StringToDouble());
            VolumeStep = _varContext.AddDoubleValidable(symbol.VolumeStep);
            VolumeStepStr = _varContext.AddConverter(VolumeStep, new StringToDouble());
            Slippage = _varContext.AddIntValidable(symbol.Slippage);
            SlippageStr = _varContext.AddConverter(Slippage, new StringToInt());

            SelectedCommissionType = _varContext.AddValidable(symbol.CommissionType);
            Commission = _varContext.AddDoubleValidable(symbol.Commission);
            CommissionStr = _varContext.AddConverter(Commission, new StringToDouble());
            LimitsCommission = _varContext.AddDoubleValidable(symbol.LimitsCommission);
            LimitsCommissionStr = _varContext.AddConverter(LimitsCommission, new StringToDouble());
            MinCommission = _varContext.AddDoubleValidable(symbol.MinCommission);
            MinCommissionStr = _varContext.AddConverter(MinCommission, new StringToDouble());
            SelectedMinCommissionCurr = _varContext.AddValidable(symbol.MinCommissionCurr);

            SwapEnabled = _varContext.AddBoolValidable(symbol.SwapEnabled);
            TripleSwap = _varContext.AddBoolValidable(symbol.TripleSwap);
            SelectedSwapType = _varContext.AddValidable(symbol.SwapType);
            SwapSizeSort = _varContext.AddDoubleValidable(symbol.SwapSizeShort);
            SwapSizeSortStr = _varContext.AddConverter(SwapSizeSort, new StringToDouble());
            SwapSizeLong = _varContext.AddDoubleValidable(symbol.SwapSizeLong);
            SwapSizeLongStr = _varContext.AddConverter(SwapSizeLong, new StringToDouble());
            SelectedTripleSwapDay = _varContext.AddIntValidable(symbol.TripleSwapDay);
            TripleSwapEnabled = _varContext.AddBoolProperty();

            SelectedProfitMode = _varContext.AddValidable(symbol.ProfitMode);

            SelectedMarginMode = _varContext.AddValidable(symbol.MarginMode);
            MarginHedged = _varContext.AddDoubleValidable(symbol.MarginHedged);
            MarginHedgedStr = _varContext.AddConverter(MarginHedged, new StringToDouble());
            SelectedMarginCurrency = _varContext.AddValidable(symbol.MarginCurrency);
            MarginFactor = _varContext.AddDoubleValidable(symbol.MarginFactor);
            MarginFactorStr = _varContext.AddConverter(MarginFactor, new StringToDouble());
            StopOrderMarginReduction = _varContext.AddDoubleValidable(symbol.StopOrderMarginReduction);
            StopOrderMarginReductionStr = _varContext.AddConverter(StopOrderMarginReduction, new StringToDouble());
            HiddenLimitOrderMarginReduction = _varContext.AddDoubleValidable(symbol.HiddenLimitOrderMarginReduction);
            HiddenLimitOrderMarginReductionStr = _varContext.AddConverter(HiddenLimitOrderMarginReduction, new StringToDouble());

            if (!IsEditMode)
            {
                Name.MustBeNotEmpy();
                Name.AddValidationRule(v => !symbolExistsFunc(v), "Symbol with such name already exists!");
            }

            BaseCurr.MustBeNotEmpy();
            ProfitCurr.MustBeNotEmpy();

            Digits.AddValidationRule(v => v >= 1 && v <= 11, "Digits can be from 1 to 11!");

            ContractSize.AddValidationRule(GetValidatePositiveRange(1e-6, 1e6), GetPositiveRangeErrorMessage(1e6, nameof(ContractSize), 1e-6));
            Slippage.AddValidationRule(GetValidatePositiveRange(0, (int)1e6), GetPositiveRangeErrorMessage(1e6, nameof(Slippage)));
            MinVolume.AddValidationRule(GetValidatePositiveRange(0.01, 1e6), GetPositiveRangeErrorMessage(1e6, nameof(MinVolume), 0.01));
            MaxVolume.AddValidationRule(GetValidatePositiveRange(0.01, 1e6), GetPositiveRangeErrorMessage(1e6, nameof(MaxVolume), 0.01));
            VolumeStep.AddValidationRule(GetValidatePositiveRange(0.01, 1e6), GetPositiveRangeErrorMessage(1e6, nameof(VolumeStep), 0.01));

            SelectedCommissionType.AddValidationRule(t => t == CommissionType.PerUnit || t == CommissionType.Absolute || t == CommissionType.Percent, "Selected СommissionType not supported");

            Commission.AddValidationRule(IsPositiveValue, GetPositiveRangeErrorMessage(nameof(Commission)));
            LimitsCommission.AddValidationRule(IsPositiveValue, GetPositiveRangeErrorMessage(nameof(LimitsCommission)));
            MinCommission.AddValidationRule(IsPositiveValue, GetPositiveRangeErrorMessage(nameof(MinCommission)));

            MarginHedged.AddValidationRule(GetValidatePositiveRange(0.01, 1), GetPositiveRangeErrorMessage(1, nameof(MarginHedged)));
            MarginFactor.AddValidationRule(GetValidatePositiveRange(1, 1e6), GetPositiveRangeErrorMessage(1e6, nameof(MarginFactor), 1));
            StopOrderMarginReduction.AddValidationRule(GetValidatePositiveRange(0.01, 1), GetPositiveRangeErrorMessage(1, nameof(StopOrderMarginReduction), 0.01));
            HiddenLimitOrderMarginReduction.AddValidationRule(GetValidatePositiveRange(0.01, 1), GetPositiveRangeErrorMessage(1, nameof(HiddenLimitOrderMarginReduction), 0.01));

            Action<VarChangeEventArgs<string>> validate = a =>
            {
                if (BaseCurr.Value == ProfitCurr.Value)
                    Error.Value = "Profit currency cannot be same as base currency!";
                else
                if (MinVolume.Value > MaxVolume.Value)
                    Error.Value = "The MaxVolume must be more or equal than MinVolume!";
                else
                    Error.Value = null;
            };

            _varContext.TriggerOnChange(BaseCurr.Var, validate);
            _varContext.TriggerOnChange(ProfitCurr.Var, validate);
            _varContext.TriggerOnChange(MinVolumeStr.Var, validate);
            _varContext.TriggerOnChange(MaxVolumeStr.Var, validate);

            _varContext.TriggerOnChange(SwapEnabled.Var, a =>
            {
                TripleSwapEnabled.Value = a.New && TripleSwap.Value;
            });

            _varContext.TriggerOnChange(TripleSwap.Var, a =>
            {
                TripleSwapEnabled.Value = a.New && SwapEnabled.Value;
            });

            IsValid = Error.Var.IsEmpty() & _varContext.GetValidationModelResult();
        }

        public CustomSymbol GetResultingSymbol()
        {
            return new CustomSymbol()
            {
                Name = Name.Value.Trim(),
                Description = Description.Value,
                BaseCurr = BaseCurr.Value.Trim(),
                ProfitCurr = ProfitCurr.Value.Trim(),
                Digits = Digits.Value,
                ContractSize = ContractSize.Value,
                MinVolume = MinVolume.Value,
                MaxVolume = MaxVolume.Value,
                VolumeStep = VolumeStep.Value,
                Slippage = Slippage.Value,

                CommissionType = SelectedCommissionType.Value,
                Commission = Commission.Value,
                LimitsCommission = LimitsCommission.Value,
                MinCommission = MinCommission.Value,
                MinCommissionCurr = SelectedMinCommissionCurr.Value,

                SwapEnabled = SwapEnabled.Value,
                SwapType = SelectedSwapType.Value,
                SwapSizeShort = SwapSizeSort.Value,
                SwapSizeLong = SwapSizeLong.Value,
                TripleSwap = TripleSwap.Value,
                TripleSwapDay = SelectedTripleSwapDay.Value,

                ProfitMode = SelectedProfitMode.Value,

                MarginMode = SelectedMarginMode.Value,
                MarginHedged = MarginHedged.Value,
                MarginFactor = MarginFactor.Value,
                MarginCurrency = SelectedMarginCurrency.Value,
                StopOrderMarginReduction = StopOrderMarginReduction.Value,
                HiddenLimitOrderMarginReduction = HiddenLimitOrderMarginReduction.Value,
            };
        }

        public Validable<string> Name { get; }
        public Validable<string> Description { get; }
        public Validable<string> BaseCurr { get; }
        public Validable<string> ProfitCurr { get; }
        public IntValidable Digits { get; }
        public IValidable<string> DigitsStr { get; }
        public DoubleValidable ContractSize { get; }
        public IValidable<string> ContractSizeStr { get; }
        public DoubleValidable MinVolume { get; }
        public IValidable<string> MinVolumeStr { get; }
        public DoubleValidable MaxVolume { get; }
        public IValidable<string> MaxVolumeStr { get; }
        public DoubleValidable VolumeStep { get; }
        public IValidable<string> VolumeStepStr { get; }
        public IntValidable Slippage { get; }
        public IValidable<string> SlippageStr { get; }
        public IObservableList<string> AvailableCurrencies { get; }

        public BoolValidable SwapEnabled { get; }
        public Validable<BO.SwapType> SelectedSwapType { get; }
        public IEnumerable<BO.SwapType> SwapTypes => EnumHelper.AllValues<BO.SwapType>();
        public DoubleValidable SwapSizeSort { get; }
        public IValidable<string> SwapSizeSortStr { get; }
        public DoubleValidable SwapSizeLong { get; }
        public IValidable<string> SwapSizeLongStr { get; }
        public BoolValidable TripleSwap { get; }
        public BoolProperty TripleSwapEnabled { get; }

        public IntValidable SelectedTripleSwapDay { get; }
        public IEnumerable<string> TripleSwapDays => Enumerable.Range(0, 7).Select(u => Enum.GetName(typeof(DayOfWeek), u));

        public Validable<BO.ProfitCalculationModes> SelectedProfitMode { get; }
        public IEnumerable<BO.ProfitCalculationModes> ProfitModes => EnumHelper.AllValues<BO.ProfitCalculationModes>();

        public Validable<BO.MarginCalculationModes> SelectedMarginMode { get; }
        public IEnumerable<BO.MarginCalculationModes> MarginModes => EnumHelper.AllValues<BO.MarginCalculationModes>();
        public Validable<string> SelectedMarginCurrency { get; }
        public DoubleValidable MarginHedged { get; }
        public IValidable<string> MarginHedgedStr { get; }
        public DoubleValidable MarginFactor { get; }
        public IValidable<string> MarginFactorStr { get; }
        public DoubleValidable StopOrderMarginReduction { get; }
        public IValidable<string> StopOrderMarginReductionStr { get; }
        public DoubleValidable HiddenLimitOrderMarginReduction { get; }
        public IValidable<string> HiddenLimitOrderMarginReductionStr { get; }

        public Validable<CommissionType> SelectedCommissionType { get; }
        public IEnumerable<CommissionType> CommissionTypes => EnumHelper.AllValues<CommissionType>();
        public DoubleValidable Commission { get; }
        public IValidable<string> CommissionStr { get; }
        public DoubleValidable LimitsCommission { get; }
        public IValidable<string> LimitsCommissionStr { get; }
        public DoubleValidable MinCommission { get; }
        public IValidable<string> MinCommissionStr { get; }
        public Validable<string> SelectedMinCommissionCurr { get; }

        public Property<string> Error { get; }
        public bool IsEditMode { get; }
        public bool IsAddMode => !IsEditMode;

        public BoolVar IsValid { get; }

        public void Ok()
        {
            TryClose(true);
        }

        public void Cancel()
        {
            TryClose(false);
        }

        private Predicate<int> GetValidatePositiveRange(int min, int max) => (int val) => val >= min && val <= max;
        private Predicate<double> GetValidatePositiveRange(double min, double max) => (double val) => val >= min && val <= max;
        private bool IsPositiveValue(double x) => x >= 0;

        private string GetPositiveRangeErrorMessage(double max, string prop, double min = 0) => $"{prop} must be between {min} and {max}";
        private string GetPositiveRangeErrorMessage(string prop) => $"{prop} must be positive";
    }
}
