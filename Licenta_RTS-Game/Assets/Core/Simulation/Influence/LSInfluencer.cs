﻿using RTSLockstep.Abilities.Essential;
using RTSLockstep.Agents;
using RTSLockstep.Simulation.Grid;
using RTSLockstep.Managers;
using System;
using System.Collections.Generic;
using RTSLockstep.Simulation.LSPhysics;
using RTSLockstep.Utility;

namespace RTSLockstep.Simulation.Influence
{
    public class LSInfluencer
    {
        #region Static Helpers
        static LSAgent tempAgent;
        static GridNode tempNode;
        #endregion

        #region Collection Helper
        [NonSerialized]
        public int bucketIndex = -1;
        #endregion

        #region ScanNode Helper
        public int NodeTicket;
        #endregion

        public GridNode LocatedNode { get; private set; }
        public LSBody Body { get; private set; }
        public LSAgent Agent { get; private set; }

        // convert to fast array
        private List<DeterminismAI> AgentAI = new List<DeterminismAI>();

        public void Setup(LSAgent agent)
        {
            Agent = agent;
            Body = agent.Body;

            if (Agent.GetAbility<Attack>() && Agent.GetAbility<Attack>().IsOffensive)
            {
                AgentAI.Add(new OffensiveAI());
            }

            if (Agent.GetAbility<Harvest>())
            {
                AgentAI.Add(new HarvesterAI());
            }

            if (Agent.GetAbility<Construct>())
            {
                AgentAI.Add(new ConstructorAI());
            }

            foreach (var AI in AgentAI)
            {
                AI.OnSetup(agent);
            }
        }

        public void Initialize()
        {
            LocatedNode = GridManager.GetNodeByPos(Body.Position.x, Body.Position.y);

            LocatedNode.AddLinkedAgent(this);

            foreach (var AI in AgentAI)
            {
                AI.OnInitialize();
            }
        }

        public void Simulate()
        {
            if (Body.PositionChangedBuffer)
            {
                tempNode = GridManager.GetNodeByPos(Body.Position.x, Body.Position.y);

                if (tempNode.IsNull())
                {
                    return;
                }

                if (!ReferenceEquals(tempNode, LocatedNode))
                {
                    if (LocatedNode.IsNotNull())
                    {
                        LocatedNode.RemoveLinkedAgent(this);
                    }

                    tempNode.AddLinkedAgent(this);
                    LocatedNode = tempNode;
                }
            }

            // we don't need influence for simulations!
            if (!ReplayManager.IsPlayingBack)
            {
                foreach (var AI in AgentAI)
                {
                    AI.OnSimulate();
                }
            }
        }

        public void Deactivate()
        {
            if (LocatedNode.IsNotNull())
            {
                LocatedNode.RemoveLinkedAgent(this);
                LocatedNode = null;
            }
        }
    }
}