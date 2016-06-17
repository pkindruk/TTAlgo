﻿using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickTrader.BotTerminal
{
    internal enum RateChangeDirections
    {
        Unknown,
        Up,
        Down,
    }

    internal class RateDirectionTracker : PropertyChangedBase
    {
        private double? rate;
        private int precision;
        private string rateFormat;

        public double? Rate
        {
            get { return rate; }
            set
            {
                if (rate == null || value == null)
                    Direction = RateChangeDirections.Unknown;
                else if (rate.Value < value)
                    Direction = RateChangeDirections.Up;
                else if (rate.Value > value)
                    Direction = RateChangeDirections.Down;

                this.rate = value;

                NotifyOfPropertyChange(nameof(Rate));
                NotifyOfPropertyChange(nameof(Direction));
                NotifyOfPropertyChange(nameof(RateString));
            }
        }

        public int Precision
        {
            get { return precision; }
            set
            {
                precision = value;
                rateFormat = "{0:F" + value + "}";

                NotifyOfPropertyChange(nameof(Precision));
                NotifyOfPropertyChange(nameof(RateString));
            }
        }

        public string RateString
        {
            get { return Rate.HasValue ? string.Format(rateFormat, Rate) : ""; }
        }

        public RateChangeDirections Direction { get; private set; }
    }
}
