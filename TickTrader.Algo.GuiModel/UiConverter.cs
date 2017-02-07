﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickTrader.Algo.GuiModel
{
    public static class UiConverter
    {
        static UiConverter()
        {
            Int = new IntConverter();
            Double = new DoubleConverter();
            String = new StringConverter();
        }

        public static UiConverter<int> Int { get; private set; }
        public static UiConverter<double> Double { get; private set; }
        public static UiConverter<string> String { get; private set; }

        internal class StringConverter : UiConverter<string>
        {
            public override string Parse(string str, out GuiModelMsg error)
            {
                error = null;
                return str;
            }

            public override string ToString(string val)
            {
                return val;
            }

            public override bool FromObject(object objVal, out string result)
            {
                result = objVal?.ToString() ?? "";
                return true;
            }
        }

        internal class IntConverter : UiConverter<int>
        {
            public override int Parse(string str, out GuiModelMsg error)
            {
                error = null;
                try
                {
                    return int.Parse(str);
                }
                catch (FormatException)
                {
                    error = new GuiModelMsg(MsgCodes.NotInteger);
                }
                catch (OverflowException)
                {
                    error = new GuiModelMsg(MsgCodes.NumberOverflow);
                }
                return 0;
            }

            public override string ToString(int val)
            {
                return val.ToString();
            }

            public override bool FromObject(object objVal, out int result)
            {
                try
                {
                    result = System.Convert.ToInt32(objVal);
                    return true;
                }
                catch (Exception)
                {
                    result = 0;
                    return false;
                }
            }
        }

        internal class DoubleConverter : UiConverter<double>
        {
            public override double Parse(string str, out GuiModelMsg error)
            {
                error = null;
                try
                {
                    return double.Parse(str);
                }
                catch (FormatException)
                {
                    error = new GuiModelMsg(MsgCodes.NotDouble);
                }
                catch (OverflowException)
                {
                    error = new GuiModelMsg(MsgCodes.NumberOverflow);
                }
                return 0;
            }

            public override string ToString(double val)
            {
                return val.ToString("R");
            }

            public override bool FromObject(object objVal, out double result)
            {
                try
                {
                    result = System.Convert.ToDouble(objVal);
                    return true;
                }
                catch (Exception)
                {
                    result = 0;
                    return false;
                }
            }
        }
    }

    public abstract class UiConverter<T>
    {
        public abstract T Parse(string str, out GuiModelMsg error);
        public abstract string ToString(T val);
        public abstract bool FromObject(object objVal, out T result);
    }
}
