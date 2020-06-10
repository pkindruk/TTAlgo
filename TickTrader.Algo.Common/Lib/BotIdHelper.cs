﻿using System;
using System.Text;
using System.Text.RegularExpressions;

namespace TickTrader.Algo.Common.Lib
{
    public class BotIdHelper
    {
        public int MaxLength { get; private set; } = 35;
        public string Pattern { get; private set; } = "[^A-Za-z0-9 ]";

        public void UseMaxLength(int idLength)
        {
            MaxLength = idLength;
        }

        public void ExcludeCharacters(string pattern)
        {
            Pattern = pattern;
        }

        public string BuildId(string botName, string suffix)
        {
            var botIdBulder = new StringBuilder(Regex.Replace(botName, Pattern, ""));
            botIdBulder.Length -= Math.Max(0, botIdBulder.Length + suffix.Length + 1 - MaxLength);
            botIdBulder.Append(" ").Append(suffix);
            
            return botIdBulder.ToString();
        }

        public bool Validate(string botId)
        {
            return botId.Length <= MaxLength && !Regex.IsMatch(botId, Pattern);
        }
    }
}
