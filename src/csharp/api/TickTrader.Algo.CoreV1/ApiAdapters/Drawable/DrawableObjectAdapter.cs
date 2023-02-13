﻿using System;
using TickTrader.Algo.Api;
using TickTrader.Algo.Domain;

namespace TickTrader.Algo.CoreV1
{
    internal interface IDrawableChangedWatcher
    {
        void OnChanged();
    }


    internal class DrawableObjectAdapter : IDrawableObject, IDrawableChangedWatcher
    {
        private readonly DrawableObjectInfo _info;
        private readonly IDrawableUpdateSink _updateSink;


        public string Name => _info.Name;

        public DrawableObjectType Type { get; } // local cache to reduce conversion costs

        public DateTime CreatedTime => _info.CreatedTime.ToUtcDateTime();

        public string OutputId => _info.OutputId;

        public bool IsBackground
        {
            get => _info.IsBackground;
            set
            {
                _info.IsBackground = value;
                OnChanged();
            }
        }

        public bool IsHidden
        {
            get => _info.IsHidden;
            set
            {
                _info.IsHidden = value;
                OnChanged();
            }
        }

        public bool IsSelectable
        {
            get => _info.IsSelectable;
            set
            {
                _info.IsSelectable = value;
                OnChanged();
            }
        }

        public long ZIndex
        {
            get => _info.ZIndex;
            set
            {
                _info.ZIndex = value;
                OnChanged();
            }
        }

        public string Tooltip
        {
            get => _info.Tooltip;
            set
            {
                _info.Tooltip = value;
                OnChanged();
            }
        }

        public IDrawableLineProps Line { get; }

        public IDrawableShapeProps Shape { get; }

        public IDrawableSymbolProps Symbol { get; }

        public IDrawableTextProps Text => throw new NotImplementedException();

        public IDrawableObjectAnchors Anchors { get; }

        public IDrawableObjectLevels Levels => throw new NotImplementedException();

        public IDrawableControlProps Control => throw new NotImplementedException();

        public IDrawableBitmapProps Bitmap => throw new NotImplementedException();


        internal bool IsNew { get; private set; }

        internal bool IsChanged { get; private set; }


        public DrawableObjectAdapter(DrawableObjectInfo info, DrawableObjectType type, IDrawableUpdateSink updateSink)
        {
            _info = info;
            Type = type;
            _updateSink = updateSink;
            IsNew = true;

            Line = new DrawableLinePropsAdapter(info.LineProps, this);
            Shape = new DrawableShapePropsAdapter(info.ShapeProps, this);
            Symbol = new DrawableSymbolPropsAdapter(info.SymbolProps, this);
            Anchors = new DrawableObjectAnchorsAdapter(info.Anchors, this);
        }


        public void PushChanges() => PushChangesInternal();


        internal void PushChangesInternal()
        {
            if (!IsNew && !IsChanged)
                return;

            var infoCopy = _info.Clone();

            DrawableCollectionUpdate upd = default;
            if (IsNew)
            {
                IsNew = false;
                upd = DrawableCollectionUpdate.Added(infoCopy);
            }
            else
            {
                upd = DrawableCollectionUpdate.Updated(infoCopy);
            }

            IsNew = false;
            IsChanged = false;

            _updateSink.Send(upd);
        }


        private void OnChanged() => IsChanged = true;

        void IDrawableChangedWatcher.OnChanged() => OnChanged();
    }
}
