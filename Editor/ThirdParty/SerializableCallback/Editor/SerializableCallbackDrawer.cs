using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SerializableCallback.Attributes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializableCallback.Editor
{
    [CustomPropertyDrawer(typeof(TargetConstraintAttribute))]
    [CustomPropertyDrawer(typeof(SerializableCallbackBase), true)]
    public class SerializableCallbackDrawer : PropertyDrawer
    {
        /// <param name="position"></param>
        /// <param name="property"></param>
        /// <param name="label"></param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Indent label
            label.text = " " + label.text;

#if UNITY_2019_1_OR_NEWER
            GUI.Box(position, "");
#else
            GUI.Box(position, "", (GUIStyle)
                "flow overlay box");
#endif
            position.y += 4;
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);
            // Draw label
            Rect pos = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            Rect targetRect = new(pos.x, pos.y, pos.width, EditorGUIUtility.singleLineHeight);

            // Get target
            SerializedProperty targetProp = property.FindPropertyRelative("_target");
            object target = targetProp.objectReferenceValue;
            if (attribute != null && attribute is TargetConstraintAttribute)
            {
                Type targetType = (attribute as TargetConstraintAttribute).targetType;
                EditorGUI.ObjectField(targetRect, targetProp, targetType, GUIContent.none);
            }
            else  EditorGUI.PropertyField(targetRect, targetProp, GUIContent.none);

            if (target == null)
            {
                Rect helpBoxRect = new(position.x + 8, targetRect.max.y + EditorGUIUtility.standardVerticalSpacing, position.width - 16, EditorGUIUtility.singleLineHeight);
                string msg = "Call not set. Execution will be slower.";
                EditorGUI.HelpBox(helpBoxRect, msg, MessageType.Warning);
            }
            else if (target is MonoScript)
            {
                Rect helpBoxRect = new(position.x + 8, targetRect.max.y + EditorGUIUtility.standardVerticalSpacing, position.width - 16, EditorGUIUtility.singleLineHeight + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                string msg = "Assign a GameObject, Component or a ScriptableObject, not a script.";
                EditorGUI.HelpBox(helpBoxRect, msg, MessageType.Warning);
            }
            else
            {
                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel++;

                // Get method name
                SerializedProperty methodProp = property.FindPropertyRelative("_methodName");
                string methodName = methodProp.stringValue;

                // Get args
                SerializedProperty argProps = property.FindPropertyRelative("_args");
                Type[] argTypes = GetArgTypes(argProps);

                // Get dynamic
                SerializedProperty dynamicProp = property.FindPropertyRelative("_dynamic");
                bool dynamic = dynamicProp.boolValue;

                // Get active method
                MethodInfo activeMethod = GetMethod(target, methodName, argTypes);

                GUIContent methodlabel = new("n/a");
                if (activeMethod != null) methodlabel = new GUIContent(PrettifyMethod(activeMethod));
                else if (!string.IsNullOrEmpty(methodName)) methodlabel = new GUIContent("Missing (" + PrettifyMethod(methodName, argTypes) + ")");

                Rect methodRect = new(position.x, targetRect.max.y + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);

                // Method select button
                pos = EditorGUI.PrefixLabel(methodRect, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(dynamic ? "Method (dynamic)" : "Method"));
                if (EditorGUI.DropdownButton(pos, methodlabel, FocusType.Keyboard))
                {
                    MethodSelector(property);
                }

                if (activeMethod != null && !dynamic)
                {
                    // Args
                    ParameterInfo[] activeParameters = activeMethod.GetParameters();
                    Rect argRect = new(position.x, methodRect.max.y + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
                    string[] types = new string[argProps.arraySize];
                    for (int i = 0; i < types.Length; i++)
                    {
                        SerializedProperty argProp = argProps.FindPropertyRelative("Array.data[" + i + "]");
                        GUIContent argLabel = new(ObjectNames.NicifyVariableName(activeParameters[i].Name));

                        EditorGUI.BeginChangeCheck();
                        switch ((Arg.ArgType)argProp.FindPropertyRelative("argType").enumValueIndex)
                        {
                            case Arg.ArgType.Bool:
                                EditorGUI.PropertyField(argRect, argProp.FindPropertyRelative("boolValue"), argLabel);
                                break;
                            case Arg.ArgType.Int:
                                EditorGUI.PropertyField(argRect, argProp.FindPropertyRelative("intValue"), argLabel);
                                break;
                            case Arg.ArgType.Float:
                                EditorGUI.PropertyField(argRect, argProp.FindPropertyRelative("floatValue"), argLabel);
                                break;
                            case Arg.ArgType.String:
                                EditorGUI.PropertyField(argRect, argProp.FindPropertyRelative("stringValue"), argLabel);
                                break;
                            case Arg.ArgType.Object:
                                EditorGUI.PropertyField(argRect, argProp.FindPropertyRelative("objectValue"), argLabel);
                                break;
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            property.FindPropertyRelative("dirty").boolValue = true;
                        }
                        argRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
                EditorGUI.indentLevel = indent;
            }

            // Set indent back to what it was
            EditorGUI.EndProperty();
        }

        private class MenuItem
        {
            public GenericMenu.MenuFunction action;
            public string path;
            public GUIContent label;

            public MenuItem(string path, string name, GenericMenu.MenuFunction action)
            {
                this.action = action;
                label = new GUIContent(path + '/' + name);
                this.path = path;
            }
        }

        private void MethodSelector(SerializedProperty property)
        {
            // Return type constraint
            Type returnType = null;
            // Arg type constraint
            Type[] argTypes = new Type[0];

            // Get return type and argument constraints
            SerializableCallbackBase dummy = GetDummyFunction(property);
            Type[] genericTypes = dummy.GetType().BaseType.GetGenericArguments();
            // SerializableEventBase is always void return type
            if (dummy is SerializableEventBase)
            {
                returnType = typeof(void);
                if (genericTypes.Length > 0)
                {
                    argTypes = new Type[genericTypes.Length];
                    Array.Copy(genericTypes, argTypes, genericTypes.Length);
                }
            }
            else
            {
                if (genericTypes != null && genericTypes.Length > 0)
                {
                    // The last generic argument is the return type
                    returnType = genericTypes[genericTypes.Length - 1];
                    if (genericTypes.Length > 1)
                    {
                        argTypes = new Type[genericTypes.Length - 1];
                        Array.Copy(genericTypes, argTypes, genericTypes.Length - 1);
                    }
                }
            }

            SerializedProperty targetProp = property.FindPropertyRelative("_target");

            List<MenuItem> dynamicItems = new();
            List<MenuItem> staticItems = new();

            List<Object> targets = new() { targetProp.objectReferenceValue };
            if (targets[0] is Component)
            {
                targets = (targets[0] as Component).gameObject.GetComponents<Component>().ToList<Object>();
                targets.Add((targetProp.objectReferenceValue as Component).gameObject);
            }
            else if (targets[0] is GameObject)
            {
                targets = (targets[0] as GameObject).GetComponents<Component>().ToList<Object>();
                targets.Add(targetProp.objectReferenceValue as GameObject);
            }
            for (int c = 0; c < targets.Count; c++)
            {
                Object t = targets[c];
                MethodInfo[] methods = targets[c].GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    // Skip methods with wrong return type
                    if (returnType != null && method.ReturnType != returnType) continue;
                    // Skip methods with null return type
                    // if (method.ReturnType == typeof(void)) continue;
                    // Skip generic methods
                    if (method.IsGenericMethod) continue;

                    Type[] parms = method.GetParameters().Select(x => x.ParameterType).ToArray();

                    // Skip methods with more than 4 args
                    if (parms.Length > 4) continue;
                    // Skip methods with unsupported args
                    if (parms.Any(x => !Arg.IsSupported(x))) continue;

                    string methodPrettyName = PrettifyMethod(methods[i]);
                    staticItems.Add(new MenuItem(targets[c].GetType().Name + "/" + methods[i].DeclaringType.Name, methodPrettyName, () => SetMethod(property, t, method, false)));

                    // Skip methods with wrong constrained args
                    if (argTypes.Length == 0 || !argTypes.SequenceEqual(parms)) continue;

                    dynamicItems.Add(new MenuItem(targets[c].GetType().Name + "/" + methods[i].DeclaringType.Name, methods[i].Name, () => SetMethod(property, t, method, true)));
                }
            }

            // Construct and display context menu
            GenericMenu menu = new();
            if (dynamicItems.Count > 0)
            {
                string[] paths = dynamicItems.GroupBy(x => x.path).Select(x => x.First().path).ToArray();
                foreach (string path in paths)
                {
                    menu.AddItem(new GUIContent(path + "/Dynamic " + PrettifyTypes(argTypes)), false, null);
                }
                for (int i = 0; i < dynamicItems.Count; i++)
                {
                    menu.AddItem(dynamicItems[i].label, false, dynamicItems[i].action);
                }
                foreach (string path in paths)
                {
                    menu.AddItem(new GUIContent(path + "/  "), false, null);
                    menu.AddItem(new GUIContent(path + "/Static parameters"), false, null);
                }
            }
            for (int i = 0; i < staticItems.Count; i++)
            {
                menu.AddItem(staticItems[i].label, false, staticItems[i].action);
            }
            if (menu.GetItemCount() == 0) menu.AddDisabledItem(new GUIContent("No methods with return type '" + GetTypeName(returnType) + "'"));
            menu.ShowAsContext();
        }

        private string PrettifyMethod(string methodName, Type[] parmTypes)
        {
            string parmnames = PrettifyTypes(parmTypes);
            return methodName + "(" + parmnames + ")";
        }

        private string PrettifyMethod(MethodInfo methodInfo)
        {
            if (methodInfo == null) throw new ArgumentNullException("methodInfo");
            ParameterInfo[] parms = methodInfo.GetParameters();
            string parmnames = PrettifyTypes(parms.Select(x => x.ParameterType).ToArray());
            return GetTypeName(methodInfo.ReturnParameter.ParameterType) + " " + methodInfo.Name + "(" + parmnames + ")";
        }

        private string PrettifyTypes(Type[] types)
        {
            if (types == null) throw new ArgumentNullException("types");
            return string.Join(", ", types.Select(GetTypeName).ToArray());
        }

        private MethodInfo GetMethod(object target, string methodName, Type[] types)
        {
            MethodInfo activeMethod = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, CallingConventions.Any, types, null);
            return activeMethod;
        }

        private Type[] GetArgTypes(SerializedProperty argProp)
        {
            Type[] types = new Type[argProp.arraySize];
            for (int i = 0; i < argProp.arraySize; i++)
            {
                types[i] = Arg.RealType((Arg.ArgType)argProp.FindPropertyRelative("Array.data[" + i + "].argType").enumValueIndex);
            }
            return types;
        }

        private void SetMethod(SerializedProperty property, Object target, MethodInfo methodInfo, bool dynamic)
        {
            SerializedProperty targetProp = property.FindPropertyRelative("_target");
            targetProp.objectReferenceValue = target;
            SerializedProperty methodProp = property.FindPropertyRelative("_methodName");
            methodProp.stringValue = methodInfo.Name;
            SerializedProperty dynamicProp = property.FindPropertyRelative("_dynamic");
            dynamicProp.boolValue = dynamic;
            SerializedProperty argProp = property.FindPropertyRelative("_args");
            ParameterInfo[] parameters = methodInfo.GetParameters();
            argProp.arraySize = parameters.Length;
            for (int i = 0; i < parameters.Length; i++)
            {
                argProp.FindPropertyRelative("Array.data[" + i + "].argType").enumValueIndex = (int)Arg.FromRealType(parameters[i].ParameterType);
            }
            property.FindPropertyRelative("dirty").boolValue = true;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        }

        private static string GetTypeName(Type t)
        {
            if (t == typeof(int)) return "int";
            if (t == typeof(float)) return "float";
            if (t == typeof(string)) return "string";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(void)) return "void";
            return t.Name;
        }

        /// <param name="property"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineheight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
            SerializedProperty targetProp = property.FindPropertyRelative("_target");
            SerializedProperty argProps = property.FindPropertyRelative("_args");
            SerializedProperty dynamicProp = property.FindPropertyRelative("_dynamic");
            float height = lineheight + lineheight;
            if (targetProp.objectReferenceValue != null && targetProp.objectReferenceValue is MonoScript) height += lineheight;
            else if (targetProp.objectReferenceValue != null && !dynamicProp.boolValue) height += argProps.arraySize * lineheight;
            height += 8;
            return height;
        }

        private static SerializableCallbackBase GetDummyFunction(SerializedProperty prop)
        {
            string stringValue = prop.FindPropertyRelative("_typeName").stringValue;
            Type type = Type.GetType(stringValue, false);
            if (type == null)
            {
                return null;
            }

            SerializableCallbackBase result = (Activator.CreateInstance(type) as SerializableCallbackBase);
            return result;
        }
    }
}
