using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using UnityObject = UnityEngine.Object;
using static Unity.Tutorials.Core.Editor.Localization;

namespace Unity.Tutorials.Core.Editor
{
    [CustomPropertyDrawer(typeof(SerializedType))]
    class SerializedTypeDrawer : PropertyDrawerExtended<SerializedTypeDrawerData>
    {
        const string k_TypeNamePath = "m_TypeName";

        internal static UserSetting<bool> ShowSimplifiedTypeNames = new UserSetting<bool>(
            "IET.ShowSimplifiedTypeNames",
            Tr("Show simplified type names"),
            true,
            Tr("Show simplified names instead of fully qualified names for SerializedTypes shown in the Inspector")
        );

        internal static UserSetting<bool> UseDefaultEditors = new UserSetting<bool>(
            "IET.UseDefaultEditors",
            Tr("Use default editors for editing tutorial assets"),
            false,
            Tr("View the tutorial asset contents using default editor views instead of custom editors")
        );

        static GUIStyle preDropGlow
        {
            get
            {
                if (s_PreDropGlow == null)
                {
                    s_PreDropGlow = new GUIStyle(GUI.skin.GetStyle("TL SelectionButton PreDropGlow"));
                    s_PreDropGlow.stretchHeight = true;
                    s_PreDropGlow.stretchWidth = true;
                }
                return s_PreDropGlow;
            }
        }
        static GUIStyle s_PreDropGlow;


        //NOTE: for lists, class fields' data is shared between all instances drawed

        Dictionary<string, Options> m_PropertyPathToOptions = new Dictionary<string, Options>();

        // Cached value for triggering renegeneration of the Options.
        bool m_ShowSimplifiedNames = ShowSimplifiedTypeNames;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }


#if UNITY_2019
        /*
         * This implementation of OnGUI differs from the other one by the fact that this uses Unity's built-in EditorGUI.Popup,
         * which has limited functionalities but has no serialization issues in 2019. The other version of OnGUI ("modern one") uses a custom, 
         * searchable Popup with improved UX but OnGUI changes detections problem that only appear in Unity 2019.
         * 
         * The reason behind this are that:
         * 1. SearchablePopup seems to not trigger any EditorGUI.changed event when an option is selected, 
         * menaning that EditorGUI.EndChangeCheck() will return false, but only in Unity 2019.
         * The OnItemSeleted callback works like a charm and the property value is correctly edited.
         * 
         * 2. The use of DropdownButton() in the modern one seems to not make EditorGUI.EndChangeCheck() work, but only in Unity 2019.
         * Removing SearchablePopup.Show and changing the call to DropdownButton with GUI.Button(position, buttonText) will 
         * have the curious effect of criteria changes being ignored once or twice before they are applied. (you'll need to invoke
         * onItemSelected manually to see this)
         * 
         * My assumption is that there's either a bug in 2019.X that has been fixed in 2020.X and prevents these features from working,
         * or the opposite.
         */
        public override void OnGUI(SerializedTypeDrawerData data, Rect position, SerializedProperty property, GUIContent label)
        {
            if (m_ShowSimplifiedNames != ShowSimplifiedTypeNames)
            {
                m_ShowSimplifiedNames = ShowSimplifiedTypeNames;
                // NOTE Ideally UserSetting.ValueChanged event would exist and we would react to its changes.
                m_PropertyPathToOptions.Clear();
            }

            Options options;
            if (!m_PropertyPathToOptions.TryGetValue(property.propertyPath, out options))
            {
                var filterAttribute = Attribute.GetCustomAttribute(fieldInfo, typeof(SerializedTypeFilterAttributeBase), true) as SerializedTypeFilterAttributeBase;
                options = new Options(filterAttribute.BaseType, filterAttribute.HideAbstractTypes);
                m_PropertyPathToOptions[property.propertyPath] = options;
            }

            var typeNameProperty = property.FindPropertyRelative(k_TypeNamePath);
            var selectedIndex = ArrayUtility.IndexOf(options.assemblyQualifiedNames, typeNameProperty.stringValue);

            if (selectedIndex == -1 || selectedIndex >= options.assemblyQualifiedNames.Length)
            {
                selectedIndex = 0;
            }

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUI.Popup(position, label, selectedIndex, options.displayedOptions);
            HandleDraggingToPopup(position, options, ref newIndex, property, typeNameProperty);
            if (EditorGUI.EndChangeCheck())
            {
                typeNameProperty.stringValue = options.assemblyQualifiedNames[newIndex];
            }

            EditorGUI.EndProperty();
        }
#else
        public override void OnGUI(SerializedTypeDrawerData data, Rect position, SerializedProperty property, GUIContent label)
        {
            int idHash = 0;

            // By manually creating the control ID, we can keep the ID for the
            // label and button the same. This lets them be selected together
            // with the keyboard in the inspector, much like a normal popup.
            if (idHash == 0)
            {
                idHash = "SerializedTypeDrawer".GetHashCode();
            }
            int id = GUIUtility.GetControlID(idHash, FocusType.Keyboard, position);

            if (m_ShowSimplifiedNames != ShowSimplifiedTypeNames)
            {
                m_ShowSimplifiedNames = ShowSimplifiedTypeNames;
                // NOTE Ideally UserSetting.ValueChanged event would exist and we would react to its changes.
                m_PropertyPathToOptions.Clear();
            }

            Options options;
            if (!m_PropertyPathToOptions.TryGetValue(property.propertyPath, out options))
            {
                var filterAttribute = Attribute.GetCustomAttribute(fieldInfo, typeof(SerializedTypeFilterAttributeBase), true) as SerializedTypeFilterAttributeBase;
                options = new Options(filterAttribute.BaseType, filterAttribute.HideAbstractTypes);
                m_PropertyPathToOptions[property.propertyPath] = options;
            }

            var typeNameProperty = property.FindPropertyRelative(k_TypeNamePath);
            int selectedIndex = ArrayUtility.IndexOf(options.assemblyQualifiedNames, typeNameProperty.stringValue);

            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, id, label);

            GUIContent buttonText;
            if (selectedIndex <= 0 || selectedIndex >= options.assemblyQualifiedNames.Length)
            {
                buttonText = options.displayedOptions[0]; //"None"
            }
            else
            {
                buttonText = options.displayedOptions[selectedIndex];
            }

            if (DropdownButton(id, position, buttonText))
            {
                Action<int> onItemSelected = (i) => OnItemSelected(i, position, options, property, typeNameProperty);
                SearchablePopup.Show(position, options.displayedOptions, selectedIndex, onItemSelected);
            }

            if (data.HasChanged)
            {
                /* HACK: removing those EndChangeCheck() will make ImGUI unable to detect changes in the SerializedTypes 
                 * edited through the popup. There's probably something internal happening here, as
                 * in the rest of the code there seems to already be an EndChangeCheck() for each BeginChangeCheck */
                EditorGUI.EndChangeCheck();
                EditorGUI.EndChangeCheck();

                data.HasChanged = false;
                SetDataForProperty(property, data);
                typeNameProperty.stringValue = options.assemblyQualifiedNames[data.NewTypeIndex];
            }

            EditorGUI.EndProperty();
        }
#endif

        void OnItemSelected(int indexInOptions, Rect position, Options options, SerializedProperty property, SerializedProperty typeNameProperty)
        {
            HandleDraggingToPopup(position, options, ref indexInOptions, property, typeNameProperty);

            SerializedTypeDrawerData data = GetDataForProperty(property);
            data.HasChanged = true;
            data.NewTypeIndex = indexInOptions;
            SetDataForProperty(property, data);
        }

        /// <summary>
        /// A custom button drawer that allows for a controlID so that we can
        /// sync the button ID and the label ID to allow for keyboard
        /// navigation like the built-in enum drawers.
        /// </summary>
        static bool DropdownButton(int id, Rect position, GUIContent content)
        {
            Event current = Event.current;
            switch (current.type)
            {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && current.button == 0)
                    {
                        Event.current.Use();
                        return true;
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id && current.character == '\n')
                    {
                        Event.current.Use();
                        return true;
                    }
                    break;
                case EventType.Repaint:
                    EditorStyles.popup.Draw(position, content, id, false);
                    break;
            }
            return false;
        }

        void RebuildOptions(SerializedProperty property)
        {
            m_PropertyPathToOptions.Remove(property.propertyPath);
        }

        void HandleDraggingToPopup(Rect dropPosition, Options options, ref int index, SerializedProperty property, SerializedProperty typeNameProperty)
        {
            if (dropPosition.Contains(Event.current.mousePosition))
            {
                switch (Event.current.type)
                {
                    case EventType.DragExited:
                        options.dragging = false;
                        RebuildOptions(property);
                        if (GUI.enabled)
                        {
                            HandleUtility.Repaint();
                        }
                        break;

                    case EventType.DragPerform:
                    case EventType.DragUpdated:
                        options.dragging = true;
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        if (Event.current.type == EventType.DragPerform)
                        {
                            UnityObject selection = DragAndDrop.objectReferences.FirstOrDefault(o => o != null);
                            if (selection != null)
                            {
                                var type = selection.GetType();
                                if (type == null)
                                {
                                    index = 0;
                                }
                                else
                                {
                                    index = ArrayUtility.IndexOf(options.assemblyQualifiedNames, type.AssemblyQualifiedName);
                                    if (index == -1)
                                    {
                                        index = 0;
                                    }
                                }

                                GUI.changed = true;
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.activeControlID = 0;
                                Event.current.Use();
                            }
                        }
                        break;
                }
            }
            else
            {
                if (options.dragging)
                {
                    if (GUI.enabled)
                    {
                        HandleUtility.Repaint();
                    }
                }
                options.dragging = false;
            }
            if (options.dragging)
            {
                GUI.Box(dropPosition, "", preDropGlow);
            }
        }

        public override SerializedTypeDrawerData CreatePropertyData(SerializedProperty property)
        {
            return new SerializedTypeDrawerData() { HasChanged = false, NewTypeIndex = -1 };
        }

        public override float GetPropertyHeight(SerializedTypeDrawerData hasChanged, SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }

    class Options
    {
        public GUIContent[] displayedOptions;
        public string[] assemblyQualifiedNames;
        public bool dragging;

        public Options(Type baseType, bool ignoreAbstractTypes)
        {
            var allowedTypes = new HashSet<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null) { continue; }

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (!baseType.IsAssignableFrom(type)) { continue; }
                        if (ignoreAbstractTypes && type.GetTypeInfo().IsAbstract) { /*Debug.LogFormat("Ignoring type: {0}", type);*/ continue; }

                        allowedTypes.Add(type);
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                }
            }

            allowedTypes.Remove(baseType);

            var optionCount = allowedTypes.Count() + 1;
            displayedOptions = new GUIContent[optionCount];
            assemblyQualifiedNames = new string[optionCount];

            var index = 0;
            displayedOptions[index] = new GUIContent(string.Format("None ({0})", baseType.FullName));
            assemblyQualifiedNames[index] = "";
            index++;

            //However, the non FQN might create ambiguity between
            //windows that share the same name but have different namespace.
            //A Smart way would be to use FQN anyway for those non-unique names
            bool displaySimplifiedNames = SerializedTypeDrawer.ShowSimplifiedTypeNames;

            foreach (var allowedType in allowedTypes.OrderBy(t => t.FullName))
            {
                displayedOptions[index] = new GUIContent(displaySimplifiedNames ? allowedType.Name : allowedType.FullName);
                assemblyQualifiedNames[index] = allowedType.AssemblyQualifiedName;
                index++;
            }
        }
    }

    /// <summary>
    /// Instance-specific data of elements redered by the drawer
    /// </summary>
    struct SerializedTypeDrawerData
    {
        public bool HasChanged;
        public int NewTypeIndex;
    }

    /// <summary>
    /// Supports drawing properties for lists.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    abstract class PropertyDrawerExtended<TData> : PropertyDrawer
    {
        Dictionary<string, TData> m_PropertyData = new Dictionary<string, TData>();

        protected TData GetDataForProperty(SerializedProperty property)
        {
            var propertyKey = GetPropertyId(property);
            if (!m_PropertyData.TryGetValue(propertyKey, out var propertyData))
            {
                propertyData = CreatePropertyData(property);
                m_PropertyData.Add(propertyKey, propertyData);
            }
            return propertyData;
        }

        protected void SetDataForProperty(SerializedProperty property, TData data)
        {
            var propertyKey = GetPropertyId(property);
            if (!m_PropertyData.ContainsKey(propertyKey)) { return; }
            m_PropertyData[propertyKey] = data;
        }

        static string GetPropertyId(SerializedProperty property)
        {
            // We use both the property name and the serialized object hash for the key as its possible the serialized object may have been disposed.
            return property.serializedObject.GetHashCode() + property.propertyPath;
        }

        public abstract TData CreatePropertyData(SerializedProperty property);
        public abstract void OnGUI(TData data, Rect position, SerializedProperty property, GUIContent label);
        public abstract float GetPropertyHeight(TData data, SerializedProperty property, GUIContent label);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var data = GetDataForProperty(property);
            OnGUI(data, position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var data = GetDataForProperty(property);
            return GetPropertyHeight(data, property, label);
        }
    }
}
