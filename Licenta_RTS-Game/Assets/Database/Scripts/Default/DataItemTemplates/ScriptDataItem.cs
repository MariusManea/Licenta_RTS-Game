﻿using UnityEngine;
using System;
using TypeReferences;

namespace RTSLockstep.Data
{
    public class ScriptDataItem : MetaDataItem
    {
        [SerializeField, HideInInspector]
        TypeReferences.ClassTypeReference _script;
        public ClassTypeReference Script
        {
            get { return _script; }
            protected set { _script = value; }
        }
        protected sealed override void OnInject(object[] data)
        {
            Type type = (Type)data[0];
            _script = type;
            _name = type.Name;
        }
    }
}