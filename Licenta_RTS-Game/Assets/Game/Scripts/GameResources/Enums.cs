﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTS
{
    public enum CursorState { Select, Move, Attack, PanLeft, PanRight, PanUp, PanDown, Harvest, RallyPoint, Unload, Load }
    public enum ResourceType { Spacing, Copper, Iron, Oil, Gold, CopperOre, IronOre, OilDeposit, GoldOre, ResearchPoint, Unknown }

    public enum GameSize { Small = 400, Medium = 600, Big = 800, Huge = 1000}

    public enum UpgradeableObjects { CityHall, University, WarFactory, Refinery, Worker, Harvester, OilPump, Tank, BatteringRam, Dock, CargoShip, BattleShip, Turret, Wonder, ConvoyTruck }
    public enum Entity { Worker, Harvester, RustyHarvester, Tank, ConvoyTruck, CargoShip, BattleShip, BatteringRam, TownCenter, CityHall, Dock, Refinery, OilPump, WarFactory, University, Turret, Wonder }
}
