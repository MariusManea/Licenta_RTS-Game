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
        public static int ScrollWidth { get { return 20; } }
        public static float MinCameraHeight { get { return 10; } }
        public static float MaxCameraHeight { get { return 80; } }

        private static Vector3 invalidPosition = new Vector3(-99999, -99999, -99999);
        public static Vector3 InvalidPosition { get { return invalidPosition; } }

        private static Bounds invalidBounds = new Bounds(new Vector3(-99999, -99999, -99999), new Vector3(0, 0, 0));
        public static Bounds InvalidBounds { get { return invalidBounds; } }

        private static GUISkin selectBoxSkin;
        public static GUISkin SelectBoxSkin { get { return selectBoxSkin; } }
        public static void StoreSelectBoxItems(GUISkin skin, Texture2D healthy, Texture2D damaged, Texture2D critical)
        {
            selectBoxSkin = skin;
            healthyTexture = healthy;
            damagedTexture = damaged;
            criticalTexture = critical;
        }

        public static int BuildSpeed { get { return 2; } }


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
        public static GameObject GetGameObject(string name)
        {
            return gameObjectList.GetGameObject(name);
        }

        public static GameObject GetPlayerObject()
        {
            return gameObjectList.GetPlayerObject();
        }

        public static Texture2D GetBuildImage(string name)
        {
            return gameObjectList.GetBuildImage(name);
        }

        public static Texture2D[] GetAvatars()
        {
            return gameObjectList.GetAvatars();
        }

        private static GameSize[] gameSizes = new GameSize[]
            {
                GameSize.Small, GameSize.Medium, GameSize.Big, GameSize.Huge
            };
        public static GameSize[] GetGameSizes()
        {
            return gameSizes;
        }

        private static Texture2D healthyTexture, damagedTexture, criticalTexture;
        public static Texture2D HealthyTexture { get { return healthyTexture; } }
        public static Texture2D DamagedTexture { get { return damagedTexture; } }
        public static Texture2D CriticalTexture { get { return criticalTexture; } }

        private static Dictionary<ResourceType, Texture2D> resourceHealthBarTextures;
        public static void SetResourceHealthBarTextures(Dictionary<ResourceType, Texture2D> images)
        {
            resourceHealthBarTextures = images;
        }
        public static Texture2D GetResourceHealthBar(ResourceType resourceType)
        {
            if (resourceHealthBarTextures != null && resourceHealthBarTextures.ContainsKey(resourceType)) return resourceHealthBarTextures[resourceType];
            return null;
        }

        public static bool MenuOpen { get; set; }

        private static float buttonHeight = 48;
        private static float headerHeight = 128, headerWidth = 256;
        private static float textHeight = 36, padding = 32;
        private static Vector2 multipleSelectionOffset = new Vector2(5, 25);
        public static float PauseMenuHeight { get { return headerHeight + 2 * buttonHeight + 4 * padding; } }
        public static float MenuWidth { get { return headerWidth + 2 * padding; } }
        public static float ButtonHeight { get { return buttonHeight; } }
        public static float ButtonWidth { get { return (MenuWidth - 3 * padding) / 2; } }
        public static float HeaderHeight { get { return headerHeight; } }
        public static float HeaderWidth { get { return headerWidth; } }
        public static float TextHeight { get { return textHeight; } }
        public static float Padding { get { return padding; } }
        public static float MultipleSelectionOffsetX { get { return multipleSelectionOffset.x; } }
        public static float MultipleSelectionOffsetY { get { return multipleSelectionOffset.y; } }

        public static float SelectedUnitButtonDimension { get { return 1.25f * buttonHeight; } }
        public static float SelectedUnitButtonSpacing { get { return buttonHeight / 16.0f; } }
        public static string LevelName { get; set; }

        public static int GetNewObjectId()
        {
            LevelLoader loader = (LevelLoader)GameObject.FindObjectOfType(typeof(LevelLoader));
            if (loader) return loader.GetNewObjectId();
            return -1;
        }
    }
}