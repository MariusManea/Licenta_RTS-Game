﻿//=======================================================================
// Copyright (c) 2015 John Pan
// Distributed under the MIT License.
// (See accompanying file LICENSE or copy at
// http://opensource.org/licenses/MIT)
//=======================================================================

#if UNITY_EDITOR
#pragma warning disable 0168 // variable declared but not used.
#pragma warning disable 0219 // variable assigned but not used.
#pragma warning disable 0414 // private field assigned but not used.
#endif

/*
 * Call Pattern
 * ------------
 * Setup: Called once per run for setting up any values
 * Initialize: Called once per instance. On managers, called in new game. On agents, called when unpooled.
 * GameStart: Called when game first starts.
 * Execute: called when Commands are received.
 * Simulate: Called once every simulation frame. 
 * Visualize: Called once every rendering/commander interfacing frame
 * Deactivate: Called upon deactivation. On managers, called when game is ended. On agents, called when pooled.
 */

using UnityEngine;

using System;

using RTSLockstep.Data;
using RTSLockstep.Simulation.Grid;
using RTSLockstep.Agents.AgentController;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Effects;
using RTSLockstep.Managers.Input;
using RTSLockstep.Networking;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player;
using RTSLockstep.Player.Commands;
using RTSLockstep.Player.Utility;
using RTSLockstep.Projectiles;
using RTSLockstep.Simulation.LSPhysics;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;

namespace RTSLockstep.Managers
{
    //TODO: Set up default functions to implement LSManager
    public static class LockstepManager
    {
        #region Properties
        public const int FrameRate = 32;
        public const int InfluenceResolution = 2;
        public const long DeltaTime = FixedMath.One / FrameRate;
        public const float DeltaTimeF = DeltaTime / FixedMath.OneF;

        private static int InfluenceCount;

        public static int InfluenceFrameCount { get; private set; }

        /// <summary>
        /// Number of frames that have passed. FrameCount/FrameRate = duration of game session in seconds.
        /// </summary>
        /// <value>The frame count.</value>
        public static int FrameCount { get; private set; }

        public static bool GameStarted { get; private set; }

        public static bool Loaded { get; private set; }

        //for testing purposes
        public static bool PoolingEnabled = true;

        public static event Action OnSetup;
        public static event Action OnInitialize;

        public static int PauseCount { get; private set; }

        public static bool IsPaused { get { return PauseCount > 0; } }

        public static NetworkHelper MainNetworkHelper;

        private static long _playRate = FixedMath.One;
        public static long PlayRate
        {
            get
            {
                return _playRate;
            }
            set
            {
                if (value != _playRate)
                {
                    _playRate = value;
                    Time.timeScale = PlayRate.ToFloat();
                    //Time.fixedDeltaTime = BaseDeltaTime / _playRate.ToFloat();
                }
            }
        }

        public static float FloatPlayRate
        {
            get { return _playRate.ToFloat(); }
            set
            {
                PlayRate = FixedMath.Create(value);
            }
        }

        private static bool Stalled;
        #endregion

        #region Event Behavior
        internal static void Setup()
        {
            DefaultMessageRaiser.EarlySetup();

            LSDatabaseManager.Setup();
            Command.Setup();

            GridManager.Setup();
            InputCodeManager.Setup();
            AbilityDataItem.Setup();

            GameResourceManager.Setup();

            ProjectileManager.Setup();
            EffectManager.Setup();

            PhysicsManager.Setup();
            ClientManager.Setup();

            Time.fixedDeltaTime = DeltaTimeF;
            Time.maximumDeltaTime = Time.fixedDeltaTime * 2;

            DefaultMessageRaiser.LateSetup();
            OnSetup?.Invoke();
        }

        internal static void Initialize(ILockstepEventsHandler[] helpers, NetworkHelper networkHelper)
        {
            PlayRate = FixedMath.One;
            //PauseCount = 0;

            if (!Loaded)
            {
                Setup();
                Loaded = true;
            }

            DefaultMessageRaiser.EarlyInitialize();

            LSDatabaseManager.Initialize();
            LSUtility.Initialize(1);
            InfluenceCount = 0;
            Time.timeScale = 1f;

            Stalled = true;

            FrameCount = 0;
            InfluenceFrameCount = 0;
            MainNetworkHelper = networkHelper;

            GlobalAgentController.Initialize();

            ClientManager.Initialize(MainNetworkHelper);

            BehaviourHelperManager.Initialize(helpers);

            GridManager.Initialize();

            CoroutineManager.Initialize();
            FrameManager.Initialize();

            CommandManager.Initialize();

            PhysicsManager.Initialize();
            PlayerManager.Initialize();

            ProjectileManager.Initialize();

            DefaultMessageRaiser.LateInitialize();
            BehaviourHelperManager.LateInitialize();
            OnInitialize?.Invoke();
        }

        internal static void Simulate()
        {
            MainNetworkHelper.Simulate();
            DefaultMessageRaiser.EarlySimulate();
            if (InfluenceCount == 0)
            {
                InfluenceSimulate();
                InfluenceCount = InfluenceResolution - 1;
                if (!FrameManager.CanAdvanceFrame)
                {
                    Stalled = true;
                    return;
                }
                Stalled = false;

                FrameManager.Simulate();
                InfluenceFrameCount++;
            }
            else
            {
                InfluenceCount--;
            }

            if (Stalled || IsPaused)
            {
                return;
            }

            if (FrameCount == 0)
            {
                GameStart();
            }
            BehaviourHelperManager.Simulate();
            GlobalAgentController.Simulate();
            PhysicsManager.Simulate();
            CoroutineManager.Simulate();
            ProjectileManager.Simulate();

            LateSimulate();
            FrameCount++;
        }

        internal static void Execute(Command com)
        {
            if (!GameStarted)
            {
                Debug.LogError("BOOM");
                return;
            }

            // Check if command is for a player command or a behavior helper
            if (com.ControllerID != byte.MaxValue)
            {
                LocalAgentController cont = GlobalAgentController.InstanceManagers[com.ControllerID];
                cont.Execute(com);
            }
            else
            {
                BehaviourHelperManager.Execute(com);
            }

            DefaultMessageRaiser.Execute(com);
        }

        internal static void Visualize()
        {
            if (!GameStarted)
            {
                return;
            }

            DefaultMessageRaiser.EarlyVisualize();
            PlayerManager.Visualize();
            BehaviourHelperManager.Visualize();
            GlobalAgentController.Visualize();
            ProjectileManager.Visualize();
            EffectManager.Visualize();
            CommandManager.Visualize();
            PhysicsManager.Visualize();
        }

        internal static void LateVisualize()
        {
            DefaultMessageRaiser.LateVisualize();
            GlobalAgentController.LateVisualize();
            PhysicsManager.LateVisualize();
            BehaviourHelperManager.LateVisualize();
        }

        internal static void UpdateGUI()
        {
            PlayerManager.UpdateGUI();
            BehaviourHelperManager.UpdateGUI();
        }

        internal static void Deactivate()
        {
            DefaultMessageRaiser.EarlyDeactivate();

            if (!GameStarted)
            {
                return;
            }

            Selector.Clear();
            GlobalAgentController.Deactivate();
            BehaviourHelperManager.Deactivate();
            ProjectileManager.Deactivate();
            EffectManager.Deactivate();
            ClientManager.Deactivate();

            ClientManager.Quit();
            PhysicsManager.Deactivate();
            GameStarted = false;
            LSServer.Deactivate();
            DefaultMessageRaiser.LateDeactivate();
            CoroutineManager.Deactivate();

            DefaultMessageRaiser.Reset();
        }
        #endregion

        public static void Pause()
        {
            PauseCount++;
        }

        public static void Unpause()
        {
            PauseCount--;
        }

        public static void Reset()
        {
            Deactivate();
        }

        //Called on the first frame of the game
        private static void GameStart()
        {
            BehaviourHelperManager.GameStart();
            GameStarted = true;
        }

        private static void LateSimulate()
        {
            BehaviourHelperManager.LateSimulate();
            GlobalAgentController.LateSimulate();
            PhysicsManager.LateSimulate();
            DefaultMessageRaiser.LateSimulate();
        }

        internal static void InfluenceSimulate()
        {
            CommandManager.Simulate();
            ClientManager.Simulate();
        }

        public static void Quit()
        {
            ClientManager.Quit();
        }

        public static int GetStateHash()
        {
            int hash = LSUtility.PeekRandom(int.MaxValue);
            hash += 1;
            hash ^= GlobalAgentController.GetStateHash();
            hash += 1;
            hash ^= ProjectileManager.GetStateHash();
            hash += 1;
            return hash;
        }
    }
}