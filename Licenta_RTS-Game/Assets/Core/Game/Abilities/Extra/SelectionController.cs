﻿using RTSLockstep.Abilities.Essential;
using RTSLockstep.LSResources;
using RTSLockstep.Managers;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player;
using RTSLockstep.Player.Utility;
using RTSLockstep.Utility;
using UnityEngine;

namespace RTSLockstep.Abilities.Extra
{
    public class SelectionController : Ability
    {
        private Health cachedHealth;
        private Harvest cachedHarvest;
        private Structure cachedStructure;

        protected Rect selectBox;
        protected GUIStyle healthStyle = new GUIStyle();
        protected float healthPercentage = 1.0f;

        protected override void OnSetup()
        {
            Agent.OnSelectedChange += HandleSelectedChange;
            Agent.OnHighlightedChange += HandleHighlightedChange;

            cachedHealth = Agent.GetAbility<Health>();
            cachedHarvest = Agent.GetAbility<Harvest>();
            cachedStructure = Agent.GetAbility<Structure>();
        }

        protected override void DoGUI()
        {
            if (Agent && !GameResourceManager.MenuOpen)
            {
                if (Agent.IsSelected && Agent.IsVisible)
                {
                    DrawSelection();
                    if (cachedStructure && cachedStructure.NeedsConstruction)
                    {
                        DrawBuildProgress();
                    }
                }
            }
        }

        public void HandleSelectedChange()
        {
            if (ReplayManager.IsPlayingBack)
            {
                return;
            }

            Agent.SetPlayingArea(Agent.Controller.ControllingPlayer.PlayerHUD.GetPlayingArea());
        }

        public void HandleHighlightedChange()
        {
            if (ReplayManager.IsPlayingBack)
            {
                return;
            }

            if (Agent && Agent.IsActive)
            {
                if (!Agent.IsSelected && Agent.IsHighlighted)
                {
                    if (Selector.MainSelectedAgent && Selector.MainSelectedAgent.IsOwnedBy(PlayerManager.CurrentPlayerController))
                    {
                        if (Selector.MainSelectedAgent.Controller.RefEquals(Agent.Controller))
                        {
                            //agent belongs to current player
                            if (Selector.MainSelectedAgent.GetAbility<Construct>())
                            {
                                if (Agent.MyAgentType == AgentType.Structure && Agent.GetAbility<Structure>() && Agent.GetAbility<Structure>().NeedsConstruction)
                                {
                                    PlayerManager.CurrentPlayer.PlayerHUD.SetCursorState(CursorState.Construct);
                                }
                            }
                            else
                            {
                                PlayerManager.CurrentPlayer.PlayerHUD.SetCursorState(CursorState.Select);
                            }
                        }
                        else if (Agent.Controller.GetAllegiance(Selector.MainSelectedAgent.Controller) != AllegianceType.Friendly && PlayerManager.CurrentPlayerController.SelectedAgents.Count > 0
                            && Agent.MyAgentType != AgentType.RawMaterial)
                        {
                            if ((Agent.MyAgentType == AgentType.Unit || Agent.MyAgentType == AgentType.Structure) && Selector.MainSelectedAgent.IsActive
                                && Selector.MainSelectedAgent.GetAbility<Attack>())
                            {
                                PlayerManager.CurrentPlayer.PlayerHUD.SetCursorState(CursorState.Attack);
                            }
                        }
                        else if (Selector.MainSelectedAgent.GetAbility<Harvest>())
                        {
                            if (Agent.MyAgentType == AgentType.RawMaterial && !Agent.GetAbility<ResourceDeposit>().IsEmpty())
                            {
                                PlayerManager.CurrentPlayer.PlayerHUD.SetCursorState(CursorState.Harvest);
                            }
                            else if (Agent.MyAgentType == AgentType.Structure 
                                && Agent.GetAbility<Structure>() && Agent.GetAbility<Structure>().CanStoreResources(Selector.MainSelectedAgent.GetAbility<Harvest>().HarvestType)
                                && Selector.MainSelectedAgent.GetAbility<Harvest>().GetCurrentLoad() > 0)
                            {
                                PlayerManager.CurrentPlayer.PlayerHUD.SetCursorState(CursorState.Deposit);
                            }
                        }
                    }
                    else
                    {
                        //agent doesn't belong to player
                        PlayerManager.CurrentPlayer.PlayerHUD.SetCursorState(CursorState.Select);
                    }
                }
            }
        }

        private void DrawSelection()
        {
            GUI.skin = GameResourceManager.SelectBoxSkin;

            Bounds selectionBounds = WorkManager.CalculateBounds(Agent.gameObject, Agent.Body.Position.ToVector3());
            Rect selectBox = WorkManager.CalculateSelectionBox(selectionBounds, Agent.GetPlayerArea());

            // Draw the selection box around the currently selected object, within the bounds of the playing area
            GUI.BeginGroup(Agent.GetPlayerArea());
            DrawSelectionBox(selectBox);
            GUI.EndGroup();
        }

        protected void DrawSelectionBox(Rect selectBox)
        {
            GUI.Box(selectBox, "");
            CalculateCurrentHealth(0.35f, 0.65f);
            DrawHealthBar(selectBox, "");
            if (cachedHarvest)
            {
                long currentLoad = cachedHarvest.GetCurrentLoad();
                if (currentLoad > 0)
                {
                    float percentFull = currentLoad / (float)cachedHarvest.Capacity;
                    float maxHeight = selectBox.height - 4;
                    float height = maxHeight * percentFull;
                    float leftPos = selectBox.x + selectBox.width - 7;
                    float topPos = selectBox.y + 2 + (maxHeight - height);
                    float width = 5;
                    Texture2D resourceBar = GameResourceManager.GetResourceHealthBar(cachedHarvest.HarvestType);
                    if (resourceBar)
                    {
                        GUI.DrawTexture(new Rect(leftPos, topPos, width, height), resourceBar);
                    }
                }
            }
        }

        public void CalculateCurrentHealth(float lowSplit, float highSplit)
        {
            if (Agent.MyAgentType == AgentType.Unit || Agent.MyAgentType == AgentType.Structure)
            {
                healthPercentage = (float) cachedHealth.CurrentHealth / (float)cachedHealth.MaxHealth;
                if (healthPercentage > highSplit)
                {
                    healthStyle.normal.background = GameResourceManager.HealthyTexture;
                }
                else if (healthPercentage > lowSplit)
                {
                    healthStyle.normal.background = GameResourceManager.DamagedTexture;
                }
                else
                {
                    healthStyle.normal.background = GameResourceManager.CriticalTexture;
                }
            }
            else if (Agent.MyAgentType == AgentType.RawMaterial)
            {
                healthPercentage = Agent.GetAbility<ResourceDeposit>().AmountLeft / (float)Agent.GetAbility<ResourceDeposit>().Capacity;
                healthStyle.normal.background = GameResourceManager.GetResourceHealthBar(Agent.GetAbility<ResourceDeposit>().ResourceType);
            }
        }

        public void DrawHealthBar(Rect selectBox, string label)
        {
            healthStyle.padding.top = -20;
            healthStyle.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(selectBox.x, selectBox.y - 7, selectBox.width * healthPercentage, 5), label, healthStyle);
        }

        private void DrawBuildProgress()
        {
            GUI.skin = GameResourceManager.SelectBoxSkin;
            Bounds selectionBounds = WorkManager.CalculateBounds(Agent.gameObject, Agent.Body.Position.ToVector3());
            Rect selectBox = WorkManager.CalculateSelectionBox(selectionBounds, Agent.GetPlayerArea());
            //Draw the selection box around the currently selected object, within the bounds of the main draw area
            GUI.BeginGroup(Agent.GetPlayerArea());
            CalculateCurrentHealth(0.5f, 0.99f);
            DrawHealthBar(selectBox, "Constructing...");
            GUI.EndGroup();
        }
    }
}