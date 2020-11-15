﻿using UnityEngine;
using System;
using System.Collections.Generic;
using RTSLockstep.Abilities;
using RTSLockstep.Managers.Input;
using RTSLockstep.LSResources;
using RTSLockstep.Integration;

namespace RTSLockstep.Data
{
#if UNITY_EDITOR
    [DataItem(
        false,
        Rotorz.ReorderableList.ReorderableListFlagsUtility.DefinedItems,
        true,
        true,
        typeof(ActiveAbility))]
#endif
    [Serializable]

    public sealed class AbilityDataItem : ScriptDataItem
    {

        private static Dictionary<string, AbilityDataItem> CodeInterfacerMap = new Dictionary<string, AbilityDataItem>();
        private static Dictionary<Type, AbilityDataItem> TypeInterfacerMap = new Dictionary<Type, AbilityDataItem>();

        public static void Setup()
        {
            if (LSDatabaseManager.TryGetDatabase(out IAbilityDataProvider database))
            {
                AbilityDataItem[] interfacers = database.AbilityData;
                for (int i = 0; i < interfacers.Length; i++)
                {
                    AbilityDataItem interfacer = interfacers[i];
                    if (interfacer.Script.Type == null)
                    {
                        //exception or ignore?
                        continue;
                    }
                    interfacer.LocalInitialize();
                    CodeInterfacerMap.Add(interfacer.Name, interfacer);
                    TypeInterfacerMap.Add(interfacer.Script.Type, interfacer);

                    //Debug.Log (interfacer.ListenInputCode + ", " + InputCodeManager.GetCodeID (interfacer.ListenInputCode) + ", " + InputCodeManager.GetCodeID ("Stop"));
                    //Debug.Log (interfacer.Name + ", " + interfacer.ListenInputCode + ", " + interfacer.ListenInputID);
                }
            }
        }

        public static AbilityDataItem FindInterfacer(string code)
        {
            if (!CodeInterfacerMap.TryGetValue(code, out AbilityDataItem output))
            {
                throw new Exception(string.Format("AbilityInterfacer for code '{0}' not found.", code));
            }
            return output;
        }

        public static AbilityDataItem FindInterfacer(Type type)
        {
            if (TypeInterfacerMap.TryGetValue(type, out AbilityDataItem interfacer))
            {
                return interfacer;
            }

            return null;
        }

        public static AbilityDataItem FindInterfacer<TAbility>() where TAbility : ActiveAbility
        {
            return FindInterfacer(typeof(TAbility));
        }

        private void LocalInitialize()
        {
            ListenInputInitialized = true;
            _listenInputID = InputCodeManager.GetCodeID(_listenInputCode);
        }

        public string GetAbilityCode()
        {
            return Name;
        }

        [SerializeField, DataCode("Input")]
        private string _listenInputCode;

        bool ListenInputInitialized { get; set; }

        private ushort _listenInputID;

        public string ListenInputCode { get { return _listenInputCode; } }

        public ushort ListenInputID
        {
            get
            {
                if (ListenInputInitialized)
                {
                    return _listenInputID;
                }
                else
                {
                    throw new System.Exception("This is a bug");
                }
            }
        }

        [SerializeField]
        private InformationGatherType _informationGather;

        public InformationGatherType InformationGather { get { return _informationGather; } }
    }

}