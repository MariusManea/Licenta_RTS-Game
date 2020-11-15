﻿using UnityEngine;
using Newtonsoft.Json;

using RTSLockstep.BuildSystem.BuildGrid;
using RTSLockstep.Managers.GameState;
using RTSLockstep.LSResources;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;
using RTSLockstep.RawMaterials;
using System.Collections.Generic;
using RTSLockstep.Utility.FastCollections;

namespace RTSLockstep.Abilities.Essential
{
    /*
     * Essential ability that attaches to any active structure 
     */
    public class Structure : Ability, IBuildable
    {
        [SerializeField]
        public bool CanProvision;
        [SerializeField, Range(0, int.MaxValue)]
        public int ProvisionAmount;
        [SerializeField]
        public bool CanStoreRawMaterial;
        [SerializeField, Tooltip("Enter object names for resources this structure can store.")]
        public RawMaterialSetLimit RawMaterialStorageDetails;
        public StructureType StructureType;
        /// <summary>
        /// Every wall pillar needs a corresponding section of wall to hold up.
        /// </summary>
        /// <value>The game object of the wall segement prefab</value>
        public GameObject WallSegmentGO;
        /// <summary>
        /// Describes the width and height of the buildable. This value does not change on the buildable.
        /// </summary>
        /// <value>The size of the build.</value>
        public int BuildSizeLow { get; set; }
        public int BuildSizeHigh { get; set; }

        public Coordinate GridPosition { get; set; }
        /// <summary>
        /// Function that relays to the buildable whether or not it's on a valid building spot.
        /// </summary>
        public bool IsValidOnGrid { get; set; }
        public bool IsMoving { get; set; }
        public bool CanOverlay { get; set; }

        public bool ValidPlacement { get; set; }
        public bool ConstructionStarted { get; set; }
        public bool NeedsConstruction { get; private set; }

        private const long gridSpacing = 2 * FixedMath.One;
        public Dictionary<Vector2d, bool> OccupiedNodes;

        private bool _needsRepair;
        private bool _provisioned;
        private int upgradeLevel;

        private Rally cachedRallyPoint;

        protected override void OnSetup()
        {
            cachedRallyPoint = Agent.GetAbility<Rally>();

            upgradeLevel = 1;
        }

        protected override void OnInitialize()
        {
            NeedsConstruction = false;
            _needsRepair = false;
            _provisioned = false;

            if (CanStoreRawMaterial)
            {
                OccupiedNodes = new Dictionary<Vector2d, bool>();

                Agent.Body.GetOutsideBoundsPositions(gridSpacing, out FastList<Vector2d> targetBoundaryPositions);

                foreach (var pos in targetBoundaryPositions)
                {
                    OccupiedNodes.Add(pos, false);
                }
            }
        }

        protected override void OnSimulate()
        {
            if (!NeedsConstruction && Agent.MyStats.CurrentHealth != Agent.MyStats.MaxHealth)
            {
                _needsRepair = true;
            }

            if (Agent.MyStats.CurrentHealth == Agent.MyStats.MaxHealth)
            {
                if (CanProvision && !_provisioned)
                {
                    _provisioned = true;
                    Agent.GetControllingPlayer().PlayerRawMaterialManager.IncreaseRawMaterialLimit(RawMaterialType.Provision, ProvisionAmount);
                }
            }
        }

        public void AwaitConstruction()
        {
            NeedsConstruction = true;
            IsCasting = true;
            Agent.MyStats.CachedHealth.CurrentHealth = FixedMath.Create(0);

            if (cachedRallyPoint)
            {
                cachedRallyPoint.SetSpawnPoint();
            }
        }

        public void BuildUp(long amount)
        {
            Agent.MyStats.CachedHealth.CurrentHealth += amount;
            if (Agent.MyStats.CurrentHealth >= Agent.MyStats.CachedHealth.BaseHealth)
            {
                Agent.MyStats.CachedHealth.CurrentHealth = Agent.MyStats.CachedHealth.BaseHealth;
                NeedsConstruction = false;
                IsCasting = false;
                Agent.SetTeamColor();
                if (CanProvision && !_provisioned)
                {
                    _provisioned = true;
                    Agent.GetControllingPlayer().PlayerRawMaterialManager.IncreaseRawMaterialLimit(RawMaterialType.Provision, ProvisionAmount);
                }
            }
        }

        public int GetUpgradeLevel()
        {
            return upgradeLevel;
        }

        public bool CanStoreResources(RawMaterialType resourceType)
        {
            return RawMaterialStorageDetails.IsNotNull() && RawMaterialStorageDetails.ContainsKey(resourceType);
        }

        public void SetGridPosition(Vector2d pos)
        {
            Coordinate coord = new Coordinate(pos.x.ToInt(), pos.y.ToInt());
            GridPosition = coord;
        }

        protected override void OnDeactivate()
        {
            if (CanProvision)
            {
                Agent.GetControllingPlayer().PlayerRawMaterialManager.DecrementRawMaterialLimit(RawMaterialType.Provision, ProvisionAmount);
            }

            GridBuilder.Unbuild(this);
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteBoolean(writer, "NeedsBuilding", NeedsConstruction);
            SaveManager.WriteBoolean(writer, "NeedsRepair", _needsRepair);
            if (NeedsConstruction)
            {
                SaveManager.WriteRect(writer, "PlayingArea", Agent.GetPlayerArea());
            }
        }

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "NeedsBuilding":
                    NeedsConstruction = (bool)readValue;
                    break;
                case "NeedsRepair":
                    _needsRepair = (bool)readValue;
                    break;
                case "PlayingArea":
                    Agent.SetPlayingArea(LoadManager.LoadRect(reader));
                    break;
                default: break;
            }
        }
    }
}