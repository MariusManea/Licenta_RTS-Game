using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FPSLog : MonoBehaviour
{
    private List<float> frameTimes;
    private List<float> memoryUsage;
    private float logTimer;
    private LevelLoader loader;
    void Start()
    {
        frameTimes = new List<float>();
        memoryUsage = new List<float>();
        logTimer = 0;
        loader = FindObjectOfType<LevelLoader>();
    }

    // Update is called once per frame
    void Update()
    {
        logTimer += Time.deltaTime;
        if (logTimer > 1)
        {
            logTimer = 0;
            frameTimes.Add(Time.deltaTime);
            memoryUsage.Add(System.GC.GetTotalMemory(true) / (1 << 20));
        }
    }

    private void OnDestroy()
    {
        if (loader)
        {
            Directory.CreateDirectory("PerformanceLogs");
            char separator = Path.DirectorySeparatorChar;
            string pathF = "PerformanceLogs" + separator + "Log" + loader.playersNumber + "_" + loader.mapSize.ToString() + "_frames.txt";
            string pathM = "PerformanceLogs" + separator + "Log" + loader.playersNumber + "_" + loader.mapSize.ToString() + "_memory.txt";
            using (StreamWriter sw = new StreamWriter(pathF))
            {
                foreach (float time in frameTimes)
                {
                    sw.Write(time + " ");
                }
            }
            using (StreamWriter sw = new StreamWriter(pathM))
            {
                foreach (float mem in memoryUsage)
                {
                    sw.Write(mem + " ");
                }
            }
        }
    }
}
