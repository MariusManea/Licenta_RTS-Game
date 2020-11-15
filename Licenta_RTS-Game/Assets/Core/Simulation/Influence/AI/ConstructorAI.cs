﻿using RTSLockstep.Abilities.Essential;
using RTSLockstep.Agents;
using RTSLockstep.Data;
using RTSLockstep.Player.Commands;
using RTSLockstep.Utility;
using System;
using RTSLockstep.LSResources;

namespace RTSLockstep.Simulation.Influence
{
    public class ConstructorAI : DeterminismAI
    {
        public override void OnInitialize()
        {
            base.OnInitialize();
        }

        public override bool ShouldMakeDecision()
        {
            if (CachedAgent.Tag != AgentTag.Builder)
            {
                // agent isn't a constructor....
                return false;
            }
            else if (searchCount <= 0)
            {
                searchCount = SearchRate;
                if (!CachedAgent.MyStats.CachedConstruct.IsFocused && !CachedAgent.MyStats.CachedConstruct.IsBuildMoving)
                {
                    // We're ready to go but have no target
                    return true;
                }
            }

            if (CachedAgent.MyStats.CachedConstruct.IsFocused || CachedAgent.MyStats.CachedConstruct.IsBuildMoving)
            {
                // busy building
                searchCount -= 1;
                return false;
            }

            return base.ShouldMakeDecision();
        }

        public override void DecideWhatToDo()
        {
            base.DecideWhatToDo();

            InfluenceConstruction();
        }

        protected override Func<LSAgent, bool> AgentConditional
        {
            get
            {
                bool agentConditional(LSAgent other)
                {
                    Structure structure = other.GetAbility<Structure>();
                    return other.GlobalID != CachedAgent.GlobalID
                            && CachedAgentValid(other)
                            && CachedAgent.GlobalID != other.GlobalID
                            && structure.IsNotNull()
                            && structure.NeedsConstruction;
                }

                return agentConditional;
            }
        }

        protected override Func<byte, bool> AllianceConditional
        {
            get
            {
                bool allianceConditional(byte bite)
                {
                    return ((CachedAgent.Controller.GetAllegiance(bite) & AllegianceType.Friendly) != 0);
                }
                return allianceConditional;
            }
        }

        private void InfluenceConstruction()
        {
            if (nearbyAgent)
            {
                Structure closestBuilding = nearbyAgent.GetComponent<Structure>();
                if (closestBuilding)
                {
                    // send construct command
                    Command constructCom = new Command(AbilityDataItem.FindInterfacer("Construct").ListenInputID);

                    // send a flag for agent to register to construction group
                    constructCom.Add(new DefaultData(DataType.Bool, true));

                    constructCom.Add(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));
                    constructCom.ControllerID = CachedAgent.Controller.ControllerID;

                    constructCom.Add(new Influence(CachedAgent));

                    CommandManager.SendCommand(constructCom);

                    base.ResetAwareness();
                }
            }
        }
    }
}