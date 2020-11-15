﻿using RTSLockstep.Agents;
using RTSLockstep.LSResources;
using System;
using UnityEngine;

namespace RTSLockstep.Data
{
    [Serializable]
    public class AgentDataItem : ObjectDataItem, IAgentData
    {
        public AgentDataItem(string name, string description) : this()
        {
            base._name = name;
            base._description = description;
        }

        public AgentDataItem()
        {

        }

        public LSAgent GetAgent()
        {
            if (Prefab != null)
            {
                LSAgent agent = Prefab.GetComponent<LSAgent>();
                if (agent)
                {
                    return agent;
                }
            }
            return null;
        }

        public String GetAgentDescription()
        {
            return _description;
        }

        public Texture2D GetAgentIcon()
        {
            if (Icon != null)
            {
                return Icon.texture;
            }
            return null;
        }

        public int SortDegreeFromAgentType(AgentType agentType)
        {
            LSAgent agent = GetAgent();
            if (agent == null) return -1;
            if (agentType == agent.MyAgentType) return 1;
            return 0;
        }

#if UNITY_EDITOR

        GameObject lastPrefab;
        protected override void OnManage()
        {

            if (lastPrefab != Prefab)
            {
                if (string.IsNullOrEmpty(Name))
                {
                    _name = Prefab.name;
                }

                lastPrefab = Prefab;
            }
        }

#endif

    }
}
