using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RTS
{
    public static class ResourceManager
    {
        public static float NormalScrollSpeed { get { return 0.5f; } }
        public static float FastScrollSpeed { get { return 1.5f; } }
        public static float CameraMovementTime { get { return 15; } }
        public static float RotateSpeed { get { return 3; } }

        private static Vector3 zoomAmount = new Vector3(0, -5, 5);
        public static Vector3 ZoomAmount { get { return zoomAmount; } }
        public static int ScrollWidth { get { return 30; } }
        public static float MinCameraHeight { get { return 10; } }
        public static float MaxCameraHeight { get { return 80; } }

        private static Vector3 invalidPosition = new Vector3(-99999, -99999, -99999);
        public static Vector3 InvalidPosition { get { return invalidPosition; } }

        private static Bounds invalidBounds = new Bounds(new Vector3(-99999, -99999, -99999), new Vector3(0, 0, 0));
        public static Bounds InvalidBounds { get { return invalidBounds; } }

        public static GUISkin SelectBoxSkin { get { return selectBoxSkin; } }

        public static int BuildSpeed { get { return 2; } }

        private static GUISkin selectBoxSkin;
        public static void StoreSelectBoxItems(GUISkin skin)
        {
            selectBoxSkin = skin;
        }


        private static GameObjectsList gameObjectList;
        public static void SetGameObjectList(GameObjectsList objectList)
        {
            gameObjectList = objectList;
        }
        public static GameObject GetBuilding(string name)
        {
            return gameObjectList.GetBuilding(name);
        }

        public static GameObject GetUnit(string name)
        {
            return gameObjectList.GetUnit(name);
        }

        public static GameObject GetWorldObject(string name)
        {
            return gameObjectList.GetWorldObject(name);
        }

        public static GameObject GetPlayerObject()
        {
            return gameObjectList.GetPlayerObject();
        }

        public static Texture2D GetBuildImage(string name)
        {
            return gameObjectList.GetBuildImage(name);
        }
    }
}