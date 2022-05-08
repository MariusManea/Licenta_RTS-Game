using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphInitializer : MonoBehaviour
{
    public Terrain terrain;
    void Awake()
    {
        AstarPath.active.Scan();
    }
}
