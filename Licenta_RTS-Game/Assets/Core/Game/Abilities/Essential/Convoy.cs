﻿using Newtonsoft.Json;
using RTSLockstep;
using UnityEngine;

namespace RTSLockstep.Abilities.Essential
{
    public class Convoy : Ability
    {
        #region Serialized Values (Further description in properties)
        #endregion

        protected override void OnInitialize()
        {
        }

        protected override void OnVisualize()
        {
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
        }

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
        }
    }
}