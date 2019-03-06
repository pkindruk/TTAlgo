﻿using System;
using System.Linq;
using System.Text;
using TickTrader.Algo.Common.Model.Config;
using TickTrader.Algo.Common.Model.Setup;
using TickTrader.Algo.Core.Metadata;

namespace TickTrader.BotTerminal
{
    public class FileParamSetupViewModel : ParameterSetupViewModel
    {
        private string _filePath;
        private string _fileName;

        public string DefaultFile { get; private set; }

        public string Filter { get; private set; }

        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                CheckFileName();
                NotifyOfPropertyChange(nameof(FileName));

                var fileName = string.Empty;

                try
                {
                    if (FilePath != null)
                    {
                        _filePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(FilePath), _fileName);
                        NotifyOfPropertyChange(nameof(FilePath));
                    }
                }
                catch (ArgumentException) { }
            }
        }

        public string FilePath
        {
            get { return _filePath; }
            set
            {
                _filePath = value;
                NotifyOfPropertyChange(nameof(FilePath));
                try
                {
                    if (FilePath != null)
                    {
                        _fileName = System.IO.Path.GetFileName(FilePath);
                        CheckFileName();
                        NotifyOfPropertyChange(nameof(FileName));
                    }
                }
                catch (ArgumentException) { }
            }
        }


        public FileParamSetupViewModel(ParameterDescriptor descriptor)
            : base(descriptor)
        {
            DefaultFile = descriptor.DefaultValue as string ?? string.Empty;

            var filterEntries = descriptor.FileFilters
               .Where(s => !string.IsNullOrWhiteSpace(s.FileMask) && !string.IsNullOrWhiteSpace(s.FileTypeName));

            var filterStrBuilder = new StringBuilder();
            foreach (var entry in filterEntries)
            {
                if (filterStrBuilder.Length > 0)
                    filterStrBuilder.Append('|');
                filterStrBuilder.Append(entry.FileTypeName).Append('|').Append(entry.FileMask);
            }
            Filter = filterStrBuilder.ToString();
        }


        public override string ToString()
        {
            return $"{DisplayName}: {FilePath}";
        }

        public override void Reset()
        {
            FilePath = DefaultFile;
        }

        public override void Load(Property srcProperty)
        {
            var typedSrcProperty = srcProperty as FileParameter;
            if (typedSrcProperty != null)
            {
                FilePath = typedSrcProperty.FileName;
            }
        }

        public override Property Save()
        {
            return new FileParameter() { Id = Id, FileName = FilePath };
        }

        private void CheckFileName()
        {
            var incorrectSymbols = System.IO.Path.GetInvalidFileNameChars();

            bool ok = FileName.All(s => !incorrectSymbols.Contains(s));

            if (string.IsNullOrWhiteSpace(FileName) || !ok)
                Error = new ErrorMsgModel(ErrorMsgCodes.RequiredButNotSet);
            else
                Error = null;
        }
    }
}
