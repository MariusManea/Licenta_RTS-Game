using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathPoint : MonoBehaviour
{
    public bool isActive;
    public Transform north;
    public Transform east;
    public Transform south;
    public Transform west;

    public void Awake()
    {
        isActive = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetType() == typeof(WorldObjects))
        {
            isActive = false;
            Debug.Log(other.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetType() == typeof(WorldObjects))
        {
            isActive = true;
            Debug.Log(other.name);
        }
    }
}
