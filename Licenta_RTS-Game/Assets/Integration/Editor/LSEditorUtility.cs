﻿#if UNITY_EDITOR
using System.Collections;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.IO;
using UnityEditor;
using UnityEngine;

using RTSLockstep.Rotorz.ReorderableList;
using RTSLockstep.Data;
using RTSLockstep.Managers;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;

namespace RTSLockstep
{
    public delegate void SerializeAction<T>(ref T value);
    public static partial class LSEditorUtility
    {
        public const ReorderableListFlags DisableReordering = ReorderableListFlags.DisableReordering;
        public const ReorderableListFlags DisableAddRemove = ReorderableListFlags.HideAddButton | ReorderableListFlags.HideRemoveButtons;
        public const ReorderableListFlags DefaultListFlags = ReorderableListFlags.ShowIndices;

        private static bool _showEnumArray;
        private static bool[] _showElements;

        private static bool _showBoolGrid;

        private const double _PI = 3.14159265359d;
        private const double _radToDeg = 180d / _PI;
        private const double _degToRad = _PI / 180d;

        private static Dictionary<object, bool[]> _foldouts = new Dictionary<object, bool[]>();

        private static Dictionary<string, bool> _persistentFlags = new Dictionary<string, bool>();
        private static Dictionary<string, float> _persistentValues = new Dictionary<string, float>();

        private static FieldInfo _lastPropertyFieldInfo;

        private static Func<ICollection<Type>> ExcludedTypeCollectionGetter { get; set; }

        public static void PropertyField(this SerializedObject so, string propertyName)
        {
            EditorGUILayout.PropertyField(so.FindProperty(propertyName));
        }

        public static void Toggle(string content, ref bool value)
        {
            value = EditorGUILayout.Toggle(content, value);
        }

        public static void EnumField<T>(string content, ref T value) where T : struct, IComparable, IConvertible, IFormattable
        {
            if (!value.GetType().IsEnum)
            {
                throw new ArgumentException("T must be enumerated type");
            }

            Enum enumVal = value as Enum;
            enumVal = EditorGUILayout.EnumPopup(content, enumVal);
            value = (T)(object)enumVal;
        }

        public static void EnumField(string content, ref object value)
        {
            if (!value.GetType().IsEnum)
            {
                throw new ArgumentException("T must be enumerated type");
            }

            value = EditorGUILayout.EnumPopup(content, (Enum)value);
        }

        public static void ArrayField<T>(string content, ref T[] values, SerializeAction<T> elementAction)
        {
            if (_showEnumArray = EditorGUILayout.Foldout(_showEnumArray, content))
            {
                if (values.IsNull())
                {
                    values = new T[0];
                }

                int size = EditorGUILayout.IntField("Size", values.Length);
                if (values.Length != size)
                {
                    Array.Resize(ref values, size);
                }

                BeginIndent();
                if (_showElements.IsNull())
                {
                    _showElements = new bool[values.Length];
                }
                else if (_showElements.Length != values.Length)
                {
                    Array.Resize(ref _showElements, values.Length);
                }

                for (int i = 0; i < values.Length; i++)
                {
                    if (_showElements[i] = EditorGUILayout.Foldout(_showElements[i], "Element " + i.ToString()))
                    {
                        elementAction(ref values[i]);
                    }
                }

                EndIndent();
            }
        }

        public static void FixedNumberField(GUIContent content, ref long Value)
        {
            Value = FixedMath.Create(EditorGUILayout.DoubleField(content, Math.Round(FixedMath.ToDouble(Value), 2, MidpointRounding.AwayFromZero)));
        }

        public static void FixedNumberField(GUIContent content, int Rounding, ref long Value)
        {
            Value = FixedMath.Create(EditorGUILayout.DoubleField(content, Math.Round(FixedMath.ToDouble(Value), Rounding, MidpointRounding.AwayFromZero)));
        }

        public static void FixedNumberField(string label, ref long Value, long max = 0)
        {
            var value = FixedMath.Create(EditorGUILayout.DoubleField(label, Math.Round(FixedMath.ToDouble(Value), 2, MidpointRounding.AwayFromZero)));
            if (max == 0 || value <= max)
            {
                Value = value;
            }
            else
            {
                Value = max;
            }
        }

        public static void FrameCountField(string label, ref int Value)
        {
            Value = (int)(EditorGUILayout.DoubleField(label, Value / (double)LockstepManager.FrameRate) * LockstepManager.FrameRate);
        }

        public static void Vector2dField(string Label, ref Vector2d vector)
        {
            vector = new Vector2d(EditorGUILayout.Vector2Field(Label, vector.ToVector2()));
        }


        public static void BoolGrid(string content, ref BoolArray2D values)
        {
            if (_showBoolGrid = EditorGUILayout.Foldout(_showBoolGrid, content))
            {
                int newWidth = EditorGUILayout.IntField("Width", values.Width);
                int newHeight = EditorGUILayout.IntField("Height", values.Height);
                values.Resize(newWidth, newHeight);
                const float cellWidth = 20;
                const float cellHeight = 20f;
                Rect drawSource = EditorGUILayout.GetControlRect();
                drawSource.width = cellWidth;
                drawSource.height = cellHeight;

                BeginIndent();
                EditorGUILayout.BeginVertical();
                for (int i = 0; i < values.Height; i++)
                {
                    GUILayout.Space(cellHeight);
                }
                EditorGUILayout.EndVertical();
                for (int i = 0; i < values.Height; i++)
                {
                    for (int j = 0; j < values.Width; j++)
                    {
                        Rect drawRect = drawSource;
                        drawRect.y += cellHeight * i;
                        drawRect.x += cellWidth * j;
                        values[j, i] = EditorGUI.Toggle(drawRect, values[j, i]);
                    }
                }

                EndIndent();
            }
        }

        public static void DatabaseObjectField<T>(string name, SerializedProperty property)
        {
            property.objectReferenceValue = EditorGUILayout.ObjectField(
                name,
                property.objectReferenceValue,
                typeof(T),
                false);
        }

        public static void FixedNumberAngle(string label, ref long Value)
        {
            double roundedDisplay = Math.Round(Math.Asin(Value.ToDouble()) * _radToDeg, 2, MidpointRounding.AwayFromZero);
            Value = FixedMath.Create(Math.Sin(_degToRad * EditorGUILayout.DoubleField(label, roundedDisplay)));
        }

        public static double Round(long value)
        {
            return Math.Round(FixedMath.ToDouble(value), 2, MidpointRounding.AwayFromZero);
        }

        private static bool[] GetFoldout(object source)
        {
            Array sourceArray = source as Array;
            if (!_foldouts.TryGetValue(source, out bool[] ret))
            {
                ret = new bool[sourceArray.IsNotNull() ? sourceArray.Length : 0];
                _foldouts.Add(source, ret);
            }
            else
            {
                if (sourceArray.IsNotNull())
                {
                    if (sourceArray.Length != ret.Length)
                    {
                        Array.Resize(ref ret, sourceArray.Length);
                    }
                }
            }

            return ret;
        }

        public static void EnumGOMap<EnumT, RequireT>(ref GameObject[] gos)
        {
            EnumMap<EnumT, GameObject>(ref gos, SerializeGameObjectAction<RequireT>);
        }

        private static void SerializeGameObjectAction<RequireT>(ref GameObject target)
        {
            GameObject temp = (GameObject)EditorGUILayout.ObjectField(
                                  target,
                                  typeof(GameObject),
                                  false);
            if (temp.IsNotNull() && temp.GetComponent<RequireT>().IsNull())
            {
                Debug.LogError(temp + " does not have the required component: " + typeof(RequireT));
            }

            target = temp;
        }

        public static void EnumMap<EnumT, MapT>(ref MapT[] enumObjects, SerializeAction<MapT> serialize)
        {
            Array enumArray = Enum.GetValues(typeof(EnumT));
            if (enumObjects.Length != enumArray.Length)
            {
                Array.Resize(ref enumObjects, enumArray.Length);
            }
            if (enumObjects.IsNull())
            {
                enumObjects = new MapT[enumArray.Length];
            }
            else if (enumObjects.Length != enumArray.Length)
            {
                Array.Resize(ref enumObjects, enumArray.Length);
            }

            bool[] foldout = GetFoldout(enumObjects);
            BeginIndent();
            for (int i = 0; i < enumArray.Length; i++)
            {
                string content = enumArray.GetValue(i).ToString();
                if (foldout[i] = EditorGUILayout.Foldout(foldout[i], content))
                {
                    serialize(ref enumObjects[i]);
                }
            }

            EndIndent();
        }

        public static void SerializeObjectAction<ObjectT>(ref ObjectT target) where ObjectT : UnityEngine.Object
        {
            ObjectT temp = (ObjectT)EditorGUILayout.ObjectField(
                               target,
                               typeof(ObjectT),
                               false);
            target = temp;
        }

        public static void BeginIndent()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(15f);
            GUILayout.BeginVertical();
        }

        public static void EndIndent()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        public static void GizmoCircle(Vector3 position, float Radius)
        {
            float theta = 0;
            float x = Radius * Mathf.Cos(theta);
            float y = Radius * Mathf.Sin(theta);
            Vector3 pos = position + new Vector3(x, 0, y);
            Vector3 newPos = pos;
            Vector3 lastPos = pos;
            for (theta = 0.1f; theta < Mathf.PI * 2f; theta += 0.1f)
            {
                x = Radius * Mathf.Cos(theta);
                y = Radius * Mathf.Sin(theta);
                newPos = position + new Vector3(x, 0, y);
                Gizmos.DrawLine(pos, newPos);
                pos = newPos;
            }

            Gizmos.DrawLine(pos, lastPos);
        }

        public static void GizmoPolyLine(Vector3[] polyLine)
        {
            for (int i = 0; i < polyLine.Length; i++)
            {
                if (i + 1 == polyLine.Length)
                {
                    Gizmos.DrawLine(polyLine[i], polyLine[0]);
                }
                else
                {
                    Gizmos.DrawLine(polyLine[i], polyLine[i + 1]);
                }
            }
        }

        public static double Scale(bool isScaled)
        {
            return isScaled ? LockstepManager.FrameRate : 1;
        }

        public static void DoubleField(Rect position, GUIContent label, ref long value, bool timescaled = false)
        {
            double scale = Scale(timescaled);
            value = FixedMath.Create(EditorGUI.DoubleField(position, label, value.ToDouble() * scale) / scale);
        }

        public static void ResizeArray<T>(ref T[,] original, int rows, int cols)
        {
            var newArray = new T[rows, cols];
            int minRows = Math.Min(rows, original.GetLength(0));
            int minCols = Math.Min(cols, original.GetLength(1));
            for (int i = 0; i < minRows; i++)
            {
                for (int j = 0; j < minCols; j++)
                {
                    newArray[i, j] = original[i, j];
                }
            }

            original = newArray;
        }

        public static void ListField(SerializedProperty property, ReorderableListFlags flags = DefaultListFlags)
        {
            ReorderableListGUI.ListField(property, flags);
        }

        public static bool GetPersistentFlagExists(string id)
        {
            return _persistentFlags.ContainsKey(id);
        }

        public static bool GetPersistentBool(string id)
        {
            if (_persistentFlags.TryGetValue(id, out bool flag))
            {
                return flag;
            }

            return false;
        }

        public static void SetPersistentFlag(string id, bool value)
        {
            if (_persistentFlags.ContainsKey(id))
            {
                _persistentFlags[id] = value;
            }
            else
            {
                _persistentFlags.Add(id, value);
            }
        }

        public static bool PersistentFoldout(Rect rect, string label, string id)
        {
            if (!_persistentFlags.TryGetValue(id, out bool show))
            {
                _persistentFlags.Add(id, false);
            }

            show = EditorGUI.Foldout(
                rect,
                show,
                label
            );
            _persistentFlags[id] = show;

            return show;
        }

        public static float GetPersistentValue(float defaultValue, string id)
        {
            if (_persistentValues.TryGetValue(id, out float value))
            {
                return value;
            }

            return defaultValue;
        }

        public static void SetPersistentValue(string id, float value)
        {
            if (!_persistentValues.ContainsKey(id))
            {
                _persistentValues.Add(id, value);
            }
            else
            {
                _persistentValues[id] = value;
            }
        }

        public static void GenerateEnum(string directory, string enumName, DataHelper data)
        {
            GenerateEnum(directory, enumName, data.Data as DataItem[], (item) => item.Name, (item) => (int)item.Name.GetHashCode());
        }

        public static void GenerateEnum(string directory, string enumName, DataItem[] data, Func<DataItem, string> getEnumElementName, Func<DataItem, int> getEnumElementValue)
        {
            string generationFolder = directory;
            string namespaceName = "RTSLockstep.Data";

            /*
            AppDomain currentDomain = AppDomain.CurrentDomain;
            // Create a dynamic assembly in the current application domain,
            // and allow it to be executed and saved to disk.
            AssemblyName aName = new AssemblyName("LockstepGenerated");
            string path = generationFolder;
            if (AssetDatabase.IsValidFolder (path) == false)
            {
                AssetDatabase.CreateFolder("/Assets", "/Lockstep/Database/Generated/Plugins");
            }
            AssemblyBuilder ab = currentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave, path);
            // Define a dynamic module in "TempAssembly" assembly. For a single-
            // module assembly, the module has the same name as the assembly.
            ModuleBuilder mb = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");

            // Define a public enumeration with the name "Elevation" and an 
            // underlying type of Integer.
            EnumBuilder eb = mb.DefineEnum("Elevation", TypeAttributes.Public, typeof(int));
            
            for (int i = 0; i < data.Length; i++) {
                TData item = data [i];
                string memberName = getEnumElementName (item);
                if (string.IsNullOrEmpty (memberName)) continue;
                int memberValue = getEnumElementValue (item);
                eb.DefineLiteral(memberName, memberValue);
            }
            
            // Create the type and save the assembly.
            Type finished = eb.CreateType();

            ab.Save(aName.Name + ".dll");
            */

            CodeCompileUnit codeBase = new CodeCompileUnit();
            CodeNamespace enumNamespace = new CodeNamespace(namespaceName);
            CodeTypeDeclaration newEnum = new CodeTypeDeclaration(enumName) { IsEnum = true };

            //Duplicate checker
            HashSet<int> enumValues = new HashSet<int>();
            HashSet<string> enumNames = new HashSet<string>();

            //Add default
            newEnum.Members.Add(new CodeMemberField
            {
                Name = "None",
                InitExpression = new CodePrimitiveExpression(0)
            });
            enumValues.Add(0);
            enumNames.Add("None");
            for (int i = 0; i < data.Length; i++)
            {
                DataItem item = data[i];
                string memberName = getEnumElementName(item);
                if (string.IsNullOrEmpty(memberName))
                {
                    throw new ArgumentException("Member cannot have empty name");
                    //continue;
                }

                if (memberName.Contains(" "))
                {
                    throw new ArgumentException("Member name cannot contain spaces");
                }

                int memberValue = getEnumElementValue(item);
                if (enumValues.Contains(memberValue) || enumNames.Contains(memberName))
                {
                    throw new ArgumentException("Duplicate member at index: " + i);
                    //continue;
                }
                CodeMemberField member = new CodeMemberField
                {
                    Name = memberName
                    ,
                    InitExpression = new CodePrimitiveExpression(memberValue)
                };

                newEnum.Members.Add(member);
                enumNames.Add(memberName);
                enumValues.Add(memberValue);
            }

            codeBase.ReferencedAssemblies.Add(typeof(string).Assembly.Location);
            codeBase.Namespaces.Add(enumNamespace);
            enumNamespace.Imports.Add(new CodeNamespaceImport("System"));
            enumNamespace.Types.Add(newEnum);

            CSharpCodeProvider provider = new CSharpCodeProvider();
            using (FileStream fs = new FileStream(
                                       generationFolder + newEnum.Name + ".cs",
                                       FileMode.Create,
                                       FileAccess.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {

                    CodeGeneratorOptions genOptions = new CodeGeneratorOptions
                    {
                        VerbatimOrder = true
                    };
                    provider.GenerateCodeFromCompileUnit(codeBase, sw, genOptions);
                }
            }

            provider.Dispose();
        }

        private static void FilterTypes(Assembly assembly, Type filterType, ICollection<Type> excludedTypes, List<Type> output)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsPublic || !type.IsClass)
                {
                    continue;
                }


                if (type.IsAbstract || !(filterType.IsAssignableFrom(type)))
                {
                    continue;
                }


                if (excludedTypes.IsNotNull() && excludedTypes.Contains(type))
                {
                    continue;
                }

                output.Add(type);
            }
        }

        public static List<Type> GetFilteredTypes(Type filterType)
        {
            var types = new List<Type>();

            var excludedTypes = (ExcludedTypeCollectionGetter.IsNotNull() ? ExcludedTypeCollectionGetter() : null);

            var assembly = Assembly.GetExecutingAssembly();
            FilterTypes(assembly, filterType, excludedTypes, types);

            foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
            {
                FilterTypes(Assembly.Load(referencedAssembly), filterType, excludedTypes, types);
            }

            types.Sort((a, b) => a.FullName.CompareTo(b.FullName));

            return types;
        }

        public static IEnumerable<Type> GetTypesWithAttribute(Assembly assembly, Type attributeType)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(attributeType, true).Length > 0)
                {
                    yield return type;
                }
            }
        }
        #region SerializedProperty helpers

        public static Type GetPropertyType(SerializedProperty property)
        {
            /*
            var path = property.propertyPath.Replace(".Array.data[", "[");
            string[] parts = path.Split('.');


            Type currentType = property.serializedObject.targetObject.GetType();
            for (int i = 0; i < parts.Length; i++)
            {
                string element = parts[i];
                if (element.Contains ("[")) {

                    string newElement = element;// = Regex.Replace(element, "[0-9]", "");
                    newElement = Regex.Replace(newElement, @"\[[^]]*\]", "");
                    element = newElement;
                    currentType = currentType.GetField(newElement, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance).FieldType;
                    return currentType.GetElementType();
                }
                //else {
                    currentType = currentType.GetField(element, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance).FieldType;

                //}
            }
            
            Type targetType = currentType;
            return targetType;*/
            object propertyTarget = GetPropertyTarget(property);
            if (propertyTarget.IsNull())
            {
                return null;
            }

            return propertyTarget.GetType();
        }

        public static IEnumerable<TAttribute> GetPropertyAttributes<TAttribute>(SerializedProperty prop)
        {
            GetPropertyTarget(prop);
            object[] attributes = _lastPropertyFieldInfo.GetCustomAttributes(typeof(TAttribute), true);

            for (int i = 0; i < attributes.Length; i++)
            {
                yield return (TAttribute)attributes[i];
            }
        }

        public static object GetPropertyTarget(SerializedProperty prop)
        {
            string path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            string[] elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    string elementName = element.Substring(0, element.IndexOf("["));
                    int index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue(obj, elementName, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }
            }

            return obj;
        }

        private static object GetValue(object source, string name)
        {
            if (source.IsNull())
            {
                return null;
            }

            Type type = source.GetType();
            _lastPropertyFieldInfo = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (_lastPropertyFieldInfo.IsNull())
            {
                PropertyInfo p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p.IsNull())
                {
                    return null;
                }

                return p.GetValue(source, null);
            }

            return _lastPropertyFieldInfo.GetValue(source);
        }

        private static object GetValue(object source, string name, int index)
        {
            IEnumerable enumerable = GetValue(source, name) as IEnumerable;
            IEnumerator enm = enumerable.GetEnumerator();
            while (index-- >= 0)
            {
                enm.MoveNext();
            }

            return enm.Current;
        }

        #endregion

        public static SerializedObject cerealObject(this ScriptableObject obj)
        {

            return new SerializedObject(obj);
        }

        public static string GetRelativeUnityAssetPath(this string absolutePath)
        {
            string relativePath;
            if (absolutePath.StartsWith(Application.dataPath))
            {
                relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }
            else
            {
                relativePath = absolutePath;
            }

            Path.GetFileNameWithoutExtension(relativePath);
            return relativePath;
        }


        public static void Draw(this SerializedProperty prop)
        {
            if (prop.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Fields with different values not multi-editable", UnityEditor.MessageType.None);
                //TODO: Throw something like what default inspectors do
                return;
            }

            EditorGUILayout.PropertyField(prop, true);
        }
    }
}
#endif