using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class Ship : Unit
{
    public int loadCapacity;

    protected override void Awake()
    {
        base.Awake();
        graph = FindObjectOfType<AstarPath>();
        navGraph = graph.data.graphs[1];
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
        this.transform.position = new Vector3(this.transform.position.x, 7, this.transform.position.z);
        CalculateBounds();
    }

    public override void StartMove(Vector3 destination)
    {
        if (audioElement != null) audioElement.Play(moveSound);
        if (navGraph.GetNearest(destination).node.Walkable)
        {
            this.destination = navGraph.GetNearest(destination).clampedPosition;
        }
        else
        {
            if (destinationTarget == null)
            {
                this.destination = GetClosestValidDestination(destination);
                if (this.destination == Vector3.negativeInfinity)
                {
                    return;
                }
            }
            else
            {
                this.destination = navGraph.GetNearest(destination).clampedPosition;
            }
        }
        this.destination = new Vector3(this.destination.x, 7, this.destination.z);
        destinationTarget = null;
        GetComponent<AIPath>().enabled = true;
        GetComponent<AIPath>().destination = this.destination;
        Vector3 rotateTo = this.transform.position;
        rotateTo = Vector3.Lerp(rotateTo, destination, 0.01f);
        rotateTo.y = 7;
        targetRotation = Quaternion.LookRotation(rotateTo - transform.position);
        rotating = true;
        moving = false;
    }
}
