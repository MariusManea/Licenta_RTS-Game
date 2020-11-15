﻿using System;

#if UNITY_EDITOR
using RTSLockstep.Rotorz.ReorderableList;
#endif

namespace RTSLockstep.Data
{
#if UNITY_EDITOR

    public class DataItemAttribute : Attribute
    {
        public DataItemAttribute()
        {
            _writableName = true;
            _listFlags = ReorderableListFlags.DisableReordering;
        }

        public DataItemAttribute(
            bool writableName,
            ReorderableListFlags listFlags,
            bool autoGenerate,
            bool useFilter,
            Type scriptBaseType)
        {
            _writableName = writableName;
            _listFlags = listFlags;
            _autoGenerate = autoGenerate;
            _scriptBaseType = scriptBaseType;
        }

        private bool _writableName;

        public bool WritableName { get { return _writableName; } }

        private bool _useFilter;

        public bool UseFilter { get { return _useFilter; } }

        private ReorderableListFlags _listFlags;

        public ReorderableListFlags ListFlags { get { return _listFlags; } }

        private bool _autoGenerate;

        public bool AutoGenerate { get { return _autoGenerate; } }

        private Type _scriptBaseType;

        public Type ScriptBaseType { get { return _scriptBaseType; } }
    }
#endif

}