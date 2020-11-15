﻿using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Data;
using RTSLockstep.Player.Commands;
using RTSLockstep.Simulation.LSMath;
using UnityEngine;

namespace RTSLockstep.Grouping
{
    public class MovementGroupHelper : BehaviourHelper
    {
        public override ushort ListenInput
        {
            get
            {
                return AbilityDataItem.FindInterfacer(typeof(Move)).ListenInputID;
            }
        }

        public static MovementGroup LastCreatedGroup { get; private set; }
        private static readonly FastBucket<MovementGroup> activeGroups = new FastBucket<MovementGroup>();
        private static readonly FastStack<MovementGroup> pooledGroups = new FastStack<MovementGroup>();

        public static MovementGroupHelper Instance { get; private set; }

        protected override void OnInitialize()
        {
            Instance = this;
            activeGroups.FastClear();
        }

        protected override void OnSimulate()
        {
            for (int i = 0; i < activeGroups.PeakCount; i++)
            {
                if (activeGroups.arrayAllocation[i])
                {
                    MovementGroup moveGroup = activeGroups[i];
                    moveGroup.LocalSimulate();
                }
            }
        }

        protected override void OnLateSimulate()
        {
            for (int i = 0; i < activeGroups.PeakCount; i++)
            {
                if (activeGroups.arrayAllocation[i])
                {
                    MovementGroup moveGroup = activeGroups[i];
                    moveGroup.LateSimulate();
                }
            }
        }

        public static bool CheckValid()
        {
            return Instance != null;
        }

        public static bool CheckValidAndAlert()
        {
            if (CheckValid())
            {
                return true;
            }

            Debug.LogError("No instance of MovementGroupHelper found. Please configure the scene to have a MovementGroupHelper for the script that requires it.");
            return false;
        }

        protected override void OnExecute(Command com)
        {
            if (com.ContainsData<Vector2d>())
            {
                StaticExecute(com);
            }
        }

        public static void StaticExecute(Command com)
        {
            CreateGroup(com);
        }

        public static MovementGroup CreateGroup(Command com)
        {
            MovementGroup moveGroup = pooledGroups.Count > 0 ? pooledGroups.Pop() : new MovementGroup();

            moveGroup.IndexID = activeGroups.Add(moveGroup);
            LastCreatedGroup = moveGroup;
            moveGroup.Initialize(com);
            return moveGroup;
        }

        public static void Pool(MovementGroup group)
        {
            int indexID = group.IndexID;
            activeGroups.RemoveAt(indexID);
            pooledGroups.Add(group);
        }

        protected override void OnDeactivate()
        {
            Instance = null;
            LastCreatedGroup = null;
        }
    }
}