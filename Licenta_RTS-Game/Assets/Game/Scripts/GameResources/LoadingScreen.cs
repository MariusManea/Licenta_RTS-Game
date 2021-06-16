using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class LoadingScreen : MonoBehaviour
{
    private LevelLoader loader;
    public GUISkin loadingSkin;
    private string[][] messages = { new string[] { "Placing players", "Growing land", "Flattening land", "Generating relief", "Creating teren", "Fixing anomalies", "Distributing resources", "Texturing teren", "Setting up territories", "Initializing players" },
                                    new string[] { "Retrieving Data", "Setting up the world", "Forming terrain", "Setting up borders", "Recalibrating camera", "Loading resources", "Loading players" } };

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
        int loadingPercent = loader.loadingMethod == 0 ? (loader.loadingMax == 0 ? 0 : (int)(1.0f * loader.loadingPercent / loader.loadingMax * 100.0f)) :
            (LoadManager.loadingCap == 0 ? 0 : (int)(1.0f * LoadManager.loadingProgress / LoadManager.loadingCap * 100.0f));
        GUI.Label(new Rect(leftPos, topPos, ResourceManager.LogoWidth, ResourceManager.HeaderHeight), loadingPercent + "%");
        topPos += 3 * ResourceManager.Padding;
        GUI.Label(new Rect(leftPos, topPos, ResourceManager.LogoWidth, ResourceManager.HeaderHeight), messages[loader.loadingMethod][loader.loadingStep] + "...");
        GUI.EndGroup();
        GUI.depth = 1;
    }
}
