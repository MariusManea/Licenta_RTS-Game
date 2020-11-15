﻿using RTSLockstep.Player.Commands;

namespace RTSLockstep.BehaviourHelpers
{
    public interface ILockstepEventsHandler
    {
        ushort GetListenInput();
        void Initialize();
        void EarlyInitialize();
        void LateInitialize();
        void Simulate();
        void LateSimulate();
        void Visualize();
        void LateVisualize();
        void UpdateGUI();
        void GlobalExecute(Command com);
        void RawExecute(Command com);
        void GameStart();
        void Deactivate();
    }
}