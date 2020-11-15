﻿using System.Collections.Generic;
using UnityEngine;

using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.Agents;
using RTSLockstep.Agents.AgentController;
using RTSLockstep.BuildSystem;
using RTSLockstep.BuildSystem.BuildGrid;
using RTSLockstep.Simulation.Grid;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player.Commands;
using RTSLockstep.Player.Utility;
using RTSLockstep.LSResources;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;
using RTSLockstep.Player;

namespace RTSLockstep.Grouping
{
    public class ConstructGroup
    {
        public Queue<LSAgent> GroupConstructionQueue = new Queue<LSAgent>();

        private LSAgent _currentGroupTarget;
        public FastList<GridNode> GroupTargetDestinations;

        public int IndexID { get; set; }

        private byte _controllerID;

        private FastList<Construct> _constructors;

        private bool _calculatedBehaviors;

        private const long gridSpacing = FixedMath.One;

        public void Initialize(Command com)
        {
            _calculatedBehaviors = false;
            _controllerID = com.ControllerID;
            Selection selection = GlobalAgentController.InstanceManagers[_controllerID].GetSelection(com);
            _constructors = new FastList<Construct>(selection.selectedAgentLocalIDs.Count);

            // check if we're queueing structures to construct
            if (com.ContainsData<QueueStructure>())
            {
                QueueStructure[] queuedStructures = com.GetDataArray<QueueStructure>();

                ProcessConstructionQueue(queuedStructures);
            }
            // otherwise were going to help construct
            else if (com.TryGetData(out DefaultData targetValue, 1) && targetValue.Is(DataType.UShort))
            {
                if (GlobalAgentController.TryGetAgentInstance((ushort)targetValue.Value, out LSAgent tempTarget))
                {
                    if (tempTarget && tempTarget.GetAbility<Structure>().NeedsConstruction)
                    {
                        _currentGroupTarget = tempTarget;
                        GroupTargetDestinations = new FastList<GridNode>();

                     //   tempTarget.Body.GetOutsideBoundNodes(gridSpacing, GroupTargetDestinations);
                    }
                }
            }
        }

        public void LocalSimulate()
        {

        }

        public void LateSimulate()
        {
            if (_constructors.IsNotNull())
            {
                if (_constructors.Count > 0)
                {
                    if (!_calculatedBehaviors)
                    {
                        _calculatedBehaviors = CalculateAndExecuteBehaviors();
                    }
                    else if ((_currentGroupTarget.IsNull() 
                        || !_currentGroupTarget.GetAbility<Structure>().NeedsConstruction) && GroupConstructionQueue.Count > 0)
                    {
                        _currentGroupTarget = GroupConstructionQueue.Dequeue();
                        _calculatedBehaviors = false;
                    }
                }

                if (_constructors.Count == 0)
                {
                    Deactivate();
                }
            }
        }

        public void Add(Construct constructor)
        {
            if (constructor.MyConstructGroup.IsNotNull())
            {
                constructor.MyConstructGroup._constructors.Remove(constructor);
            }

            if (_currentGroupTarget.IsNotNull())
            {
                constructor.MyConstructGroup = this;
                constructor.MyConstructGroupID = IndexID;

                _constructors.Add(constructor);
            }
        }

        public void Remove(Construct constructor)
        {
            if (constructor.MyConstructGroup.IsNotNull() && constructor.MyConstructGroupID == IndexID)
            {
                _constructors.Remove(constructor);
                constructor.MyConstructGroup = null;
                constructor.MyConstructGroupID = -1;
            }
        }

        private bool CalculateAndExecuteBehaviors()
        {
            ExecuteConstruction();
            return true;
        }

        private void ProcessConstructionQueue(QueueStructure[] _queueStructures)
        {
            for (int i = 0; i < _queueStructures.Length; i++)
            {
                QStructure qStructure = _queueStructures[i].Value;
                if (qStructure.IsNotNull())
                {
                    LSAgent newRTSAgent = GlobalAgentController.InstanceManagers[_controllerID].CreateAgent(qStructure.StructureName, qStructure.BuildPoint, qStructure.RotationPoint) as LSAgent;
                    Structure newStructure = newRTSAgent.GetAbility<Structure>();

                    // remove the bounds so we can get to the temp structure from any angle
                    if (newRTSAgent.GetAbility<DynamicBlocker>())
                    {
                        newRTSAgent.GetAbility<DynamicBlocker>().SetTransparent(true);
                    }

                    // wall segments can shrink and expand
                    if (newStructure.StructureType == StructureType.WallPillar || newStructure.StructureType == StructureType.WallSegment)
                    {
                        newRTSAgent.transform.localScale = qStructure.LocalScale.ToVector3();
                        newStructure.CanOverlay = true;
                    }

                    newRTSAgent.Body.HalfWidth = qStructure.HalfWidth;
                    newRTSAgent.Body.HalfLength = qStructure.HalfLength;

                    newStructure.BuildSizeLow = (newRTSAgent.Body.HalfWidth.CeilToInt() * 2);
                    newStructure.BuildSizeHigh = (newRTSAgent.Body.HalfLength.CeilToInt() * 2);

                    if (GridBuilder.Place(newRTSAgent.GetAbility<Structure>(), newRTSAgent.Body.Position))
                    {
                        LSPlayer controllingPlayer = GlobalAgentController.InstanceManagers[_controllerID].ControllingPlayer;
                        controllingPlayer.PlayerRawMaterialManager.RemovePlayersRawMaterials(newRTSAgent);

                        newRTSAgent.SetControllingPlayer(GlobalAgentController.InstanceManagers[_controllerID].ControllingPlayer);

                        newStructure.AwaitConstruction();
                        // Set to transparent material until constructor is in range to start
                        ConstructionHandler.SetTransparentMaterial(newStructure.gameObject, GameResourceManager.AllowedMaterial, true);

                        if (_currentGroupTarget.IsNull())
                        {
                            //Set the current project if we don't have one
                            _currentGroupTarget = newRTSAgent;
                        }
                        else
                        {
                            GroupConstructionQueue.Enqueue(newRTSAgent);
                        }
                    }
                    else
                    {
                        Debug.Log("Couldn't place building!");
                        newRTSAgent.Die();
                    }
                }
            }
        }

        private void Deactivate()
        {
            Construct constructor;
            for (int i = 0; i < _constructors.Count; i++)
            {
                constructor = _constructors[i];
                constructor.MyConstructGroup = null;
                constructor.MyConstructGroupID = -1;
            }
            _constructors.FastClear();
            GroupConstructionQueue.Clear();
            _currentGroupTarget = null;
            ConstructGroupHelper.Pool(this);
            _calculatedBehaviors = false;
            IndexID = -1;
        }

        private void ExecuteConstruction()
        {
            for (int i = 0; i < _constructors.Count; i++)
            {
                Construct constructor = _constructors[i];
                constructor.OnConstructGroupProcessed(_currentGroupTarget);
            }
        }
    }
}
