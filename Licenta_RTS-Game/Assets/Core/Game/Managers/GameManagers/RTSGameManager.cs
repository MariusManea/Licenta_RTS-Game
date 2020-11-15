﻿using UnityEngine;
using UnityEngine.SceneManagement;

namespace RTSLockstep.Managers.GameManagers
{
    public class RTSGameManager : GameManager
    {
        #region Properties
        private static Replay LastSave = new Replay();
        #endregion

        #region MonoBehavior
        protected override void OnGUI()
        {
            base.OnGUI();
            //GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, new Vector3(2.5f, 2.5f, 1));

            //if (GUILayout.Button("Restart"))
            //{
            //    ReplayManager.Stop();
            //    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            //}

            if (GUILayout.Button("Playback"))
            {
                LastSave = ReplayManager.SerializeCurrent();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                ReplayManager.Play(LastSave);
            }
        }
        #endregion
    }
}