﻿using RTSLockstep.Agents;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using RTSLockstep.LSResources;

namespace RTSLockstep.Managers
{
    public static class WorkManager
    {
        //no longer needed?
        public static Rect CalculateSelectionBox(Bounds selectionBounds, Rect playingArea)
        {
            //shorthand for the coordinates of the centre of the selection bounds
            float cx = selectionBounds.center.x;
            float cy = selectionBounds.center.y;
            float cz = selectionBounds.center.z;
            //shorthand for the coordinates of the extents of the selection bounds
            float ex = selectionBounds.extents.x;
            float ey = selectionBounds.extents.y;
            float ez = selectionBounds.extents.z;

            //Determine the screen coordinates for the corners of the selection bounds
            List<Vector3> corners = new List<Vector3>();
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + ex, cy + ey, cz + ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + ex, cy + ey, cz - ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + ex, cy - ey, cz + ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - ex, cy + ey, cz + ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx + ex, cy - ey, cz - ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - ex, cy - ey, cz + ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - ex, cy + ey, cz - ez)));
            corners.Add(Camera.main.WorldToScreenPoint(new Vector3(cx - ex, cy - ey, cz - ez)));

            //Determine the bounds on screen for the selection bounds
            Bounds screenBounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Count; i++)
            {
                screenBounds.Encapsulate(corners[i]);
            }

            //Screen coordinates start in the bottom left corner, rather than the top left corner
            //this correction is needed to make sure the selection box is drawn in the correct place
            float selectBoxTop = playingArea.height - (screenBounds.center.y + screenBounds.extents.y);
            float selectBoxLeft = screenBounds.center.x - screenBounds.extents.x;
            float selectBoxWidth = 2 * screenBounds.extents.x;
            float selectBoxHeight = 2 * screenBounds.extents.y;

            return new Rect(selectBoxLeft, selectBoxTop, selectBoxWidth, selectBoxHeight);
        }

        public static RawMaterialType GetResourceType(string resourceName)
        {
            return (RawMaterialType)Enum.Parse(typeof(RawMaterialType), resourceName);            
        }

        public static FlagState GetFlagState(string type)
        {
            switch (type)
            {
                case "SetFlag":
                    return FlagState.SetFlag;
                case "SettingFlag":
                    return FlagState.SettingFlag;
                case "FlagSet":
                    return FlagState.FlagSet;
                default:
                    return FlagState.SetFlag;
            }
        }

        //currently used by harvest ability, switch to influence?
        public static LSAgent FindNearestWorldObjectInListToPosition(List<LSAgent> objects, Vector3 position)
        {
            if (objects == null || objects.Count == 0)
            {
                return null;
            }

            LSAgent nearestObject = objects[0];
            float sqrDistanceToNearestObject = Vector3.SqrMagnitude(position - nearestObject.transform.position);
            for (int i = 1; i < objects.Count; i++)
            {
                float sqrDistanceToObject = Vector3.SqrMagnitude(position - objects[i].transform.position);
                if (sqrDistanceToObject < sqrDistanceToNearestObject)
                {
                    sqrDistanceToNearestObject = sqrDistanceToObject;
                    nearestObject = objects[i];
                }
            }

            return nearestObject;
        }

        //get size based on the mesh renderer attached to the empty GO since it's not an agent yet...
        public static Bounds CalculateBounds(GameObject obj, Vector3 position)
        {
            Bounds rendererBounds = new Bounds(position, Vector3.zero);
            foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
            {
                rendererBounds.Encapsulate(r.bounds);
            }

            return rendererBounds;
        }

        //This extension method gets a copy of a component on an existing game object:
        public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component
        {
            return go.AddComponent<T>().GetCopyOf(toAdd) as T;
        }

        public static T GetCopyOf<T>(this Component comp, T other) where T : Component
        {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp as T;
        }
    }
}
