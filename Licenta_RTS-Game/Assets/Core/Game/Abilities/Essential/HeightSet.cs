﻿using RTSLockstep.Determinism;
using RTSLockstep.Integration;
using RTSLockstep.Environment;
using UnityEngine;

namespace RTSLockstep.Abilities.Essential
{
    [UnityEngine.DisallowMultipleComponent]
    public class HeightSet : Ability
    {
        [SerializeField]
        private int _mapIndex;

        public int MapIndex { get { return _mapIndex; } }

        [SerializeField, FixedNumber]
        private long _bonusHeight;
        public long BonusHeight { get { return _bonusHeight; } }

        private long _offset;

        [Lockstep(true)]
        public long Offset
        {
            get { return _offset; }
            set
            {
                if (_offset != value)
                {
                    _offset = value;
                    ForceUpdate = true;
                }
            }
        }

        public bool ForceUpdate { get; set; }

        protected override void OnSimulate()
        {
            if (Agent && (Agent.Body.PositionChanged || Agent.Body.PositionChangedBuffer || ForceUpdate))
            {
                UpdateHeight();
            }
        }
        public void UpdateHeight()
        {
            long height = HeightmapSaver.Instance.GetHeight(MapIndex, Agent.Body.Position) + _bonusHeight + Offset;
            Agent.Body.HeightPos = height;
        }
    }
}