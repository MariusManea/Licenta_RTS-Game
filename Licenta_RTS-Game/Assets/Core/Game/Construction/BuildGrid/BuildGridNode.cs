﻿using RTSLockstep.Simulation.LSMath;

namespace RTSLockstep.BuildSystem.BuildGrid
{
    public class BuildGridNode
    {
        public BuildGridManager ParentGrid { get; private set; }
        public Coordinate Position { get; private set; }
        public bool Occupied { get { return RegisteredBuilding != null; } }
        public IBuildable RegisteredBuilding { get; set; }
        public bool IsNeighbor
        {
            get
            {
                int buildSpacing = ParentGrid.BuildSpacing;
                for (int x = Position.x - buildSpacing; x <= Position.x + buildSpacing; x++)
                {
                    for (int y = Position.y - buildSpacing; y <= Position.y + buildSpacing; y++)
                    {
                        if (ParentGrid.IsOnGrid(x, y) && ParentGrid.Grid[x, y].Occupied)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public BuildGridNode(BuildGridManager parentGrid, Coordinate position)
        {
            Position = position;
            ParentGrid = parentGrid;
        }
    }
}