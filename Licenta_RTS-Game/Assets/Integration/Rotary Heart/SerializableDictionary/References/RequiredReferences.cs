﻿using UnityEngine;

using RTSLockstep.LSResources;
using RTSLockstep.RawMaterials;

namespace RotaryHeart.Lib.SerializableDictionary
{
    /// <summary>
    /// This class is used so that the dictionary keys can have a default value, unity editor will give the default value, because it can't be null.
    /// This should only be used for UnityEngine.Object inherited classes
    /// </summary>
    public class RequiredReferences : ScriptableObject
    {
        //Important note, the fields need to be private so that the reflection code can find them.
        //Use [SerializeField] so that the editor draws the property field and sets a default value
        [SerializeField]
        private GameObject _gameObject;
        [SerializeField]
        private Material _material;
        [SerializeField]
        private AudioClip _audioClip;
        [SerializeField]
        private UserInputKeyMappings _userInputKeyMappings;
        [SerializeField]
        private KeyCode _keyCode;
        [SerializeField]
        private RawMaterialType _resourceType;
        [SerializeField]
        private RawMaterialLimited _startingResourceInfo;
    }
}