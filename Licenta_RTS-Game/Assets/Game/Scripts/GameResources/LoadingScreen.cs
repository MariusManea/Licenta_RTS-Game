using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class LoadingScreen : MonoBehaviour
{
    private LevelLoader loader;
    public GUISkin loadingSkin;
    void Start()
    {
        loader = GetComponent<LevelLoader>();   
    }
    void OnGUI()
    {
        GUI.depth = 0;
        GUI.skin = loadingSkin;
        GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
        float leftPos = Screen.width / 2 - ResourceManager.LogoWidth / 2;
        float topPos = Screen.height / 2 - ResourceManager.HeaderHeight / 2 - 2 * ResourceManager.Padding;
        GUI.Label(new Rect(leftPos, topPos, ResourceManager.LogoWidth, ResourceManager.HeaderHeight), (loader.loadingMax == 0 ? 0 : (int)(1.0f * loader.loadingPercent / loader.loadingMax * 100.0f)) + "%");
        GUI.EndGroup();
        GUI.depth = 1;
    }
}
