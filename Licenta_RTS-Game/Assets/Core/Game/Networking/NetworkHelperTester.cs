﻿using RTSLockstep.Agents.AgentController;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Managers;
using UnityEngine;

namespace RTSLockstep.Networking
{
    public class NetworkHelperTester : BehaviourHelper
    {
        public string IP = "127.0.0.1";
        public int RoomSize = 1;

        protected override void OnUpdateGUI()
        {
            GUILayout.Label("State Hash: " + GlobalAgentController.GetStateHash());

            if (LockstepManager.MainNetworkHelper != null && LockstepManager.MainNetworkHelper.IsConnected)
            {
                return;
            }
            #region Setting IP and RoomSize via GUI
            GUILayout.BeginVertical(GUILayout.Width(300f));
            GUILayout.Label("Time: " + Time.time.ToString());
            GUI.color = Color.white;
            GUILayout.Label("IP: ");
            IP = GUILayout.TextField(IP);

            GUILayout.Label("Room Size");
            int.TryParse(GUILayout.TextField(RoomSize.ToString()), out RoomSize);
            #endregion

            //Below = important!
            if (GUILayout.Button("Host"))
            {
                //Hosting with a room size of RoomSize
                ClientManager.HostGame(RoomSize);
            }
            if (GUILayout.Button("Connect"))
            {
                //Connecting to the server with ip address 'IP'
                ClientManager.ConnectGame(IP);
            }
            GUILayout.EndVertical();
        }

        void OnDisable()
        {
        }
    }
}