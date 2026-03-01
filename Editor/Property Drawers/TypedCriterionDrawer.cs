using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    [CustomPropertyDrawer(typeof(TypedCriterion))]
    internal class TypedCriterionDrawer : PropertyDrawer
    {
        // criterionProperty is a SerializedProperty on the SerializedObject for the Criterion
        private delegate void PropertyIteratorCallback(SerializedProperty criterionProperty);

        private const string k_TypeField = nameof(TypedCriterion.Type);

        private const string k_CriterionField = nameof(TypedCriterion.Criterion);
        // Base class properties we want to draw after the derived class properties
        private static readonly List<string> k_BaseClassProperties = new()
        {
            nameof(Criterion.Completed),
            nameof(Criterion.Invalidated),
        };

        private static readonly List<string> k_PropertiesToIgnore = new() { "m_Script" };

        private Dictionary<string, SerializedObject> m_PerPropertyCriterionSerializedObjects = new();

        private Rect m_CriterionPropertyRect;
        private bool m_InspectorRedrawn;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            VisualElement contentRoot = new();

            PropertyField propertyField = new();
            propertyField.BindProperty(property.FindPropertyRelative(k_TypeField));

            propertyField.RegisterValueChangeCallback(evt =>
            {
                OnCriterionTypeChanged(property);
                contentRoot.Clear();
                CreateElementIterateCriterion(property.FindPropertyRelative(k_CriterionField), contentRoot);
            });

            propertyField.RegisterCallbackOnce((AttachToPanelEvent evt) => {
                contentRoot.Clear();
                CreateElementIterateCriterion(property.FindPropertyRelative(k_CriterionField), contentRoot);
            });

            root.Add(propertyField);
            root.Add(contentRoot);

            return root;
        }

        private SerializedObject GetSerializedObject(SerializedProperty criterionProperty)
        {
            if (criterionProperty.objectReferenceValue == null)
                return null;

            string key = criterionProperty.propertyPath;
            bool found = m_PerPropertyCriterionSerializedObjects.TryGetValue(key, out SerializedObject serializedObject);
            if (!found || serializedObject.targetObject == null)
            {
                serializedObject = new SerializedObject(criterionProperty.objectReferenceValue);
                m_PerPropertyCriterionSerializedObjects[key] = serializedObject;
            }

            return serializedObject;
        }


        private void CreateElementIterateCriterion(SerializedProperty criterion, VisualElement root)
        {
            if (criterion.objectReferenceValue == null)
                return;

            SerializedObject serializedObject = new(criterion.objectReferenceValue);

            // First pass: draw properties of the derived class.
            SerializedProperty childProperty = serializedObject.GetIterator();
            childProperty.NextVisible(true);
            while (childProperty.NextVisible(childProperty.isExpanded))
            {
                if (k_PropertiesToIgnore.Contains(childProperty.propertyPath))
                    continue;
                if (k_BaseClassProperties.Contains(childProperty.propertyPath))
                    continue;

                PropertyField propElement = new(childProperty);
                propElement.BindProperty(childProperty);

                root.Add(propElement);
            }

            // Second pass: draw properties of the base class.
            childProperty = serializedObject.GetIterator();
            childProperty.NextVisible(true);
            while (childProperty.NextVisible(childProperty.isExpanded))
            {
                if (k_BaseClassProperties.Contains(childProperty.propertyPath))
                {
                    PropertyField propElement = new(childProperty);
                    propElement.BindProperty(childProperty);

                    root.Add(propElement);
                }
            }
        }

        private void IterateCriterion(SerializedProperty criterion, PropertyIteratorCallback onIterateChildProperty)
        {
            if (criterion.objectReferenceValue == null)
                return;

            SerializedObject serializedObject = GetSerializedObject(criterion);
            if (serializedObject == null)
                return;

            // First pass: draw properties of the derived class.
            SerializedProperty childProperty = serializedObject.GetIterator();
            childProperty.NextVisible(true);
            while (childProperty.NextVisible(childProperty.isExpanded))
            {
                if (k_PropertiesToIgnore.Contains(childProperty.propertyPath))
                    continue;
                if (k_BaseClassProperties.Contains(childProperty.propertyPath))
                    continue;

                onIterateChildProperty(childProperty);
            }

            // Second pass: draw properties of the base class.
            childProperty = serializedObject.GetIterator();
            childProperty.NextVisible(true);
            while (childProperty.NextVisible(childProperty.isExpanded))
            {
                if (k_BaseClassProperties.Contains(childProperty.propertyPath))
                    onIterateChildProperty(childProperty);
            }
        }

        private void OnGUIIterateCriterion(SerializedProperty criterionProperty)
        {
            criterionProperty.serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            m_CriterionPropertyRect.height = EditorGUI.GetPropertyHeight(criterionProperty);
            EditorGUI.PropertyField(m_CriterionPropertyRect, criterionProperty, true);
            m_CriterionPropertyRect.y += m_CriterionPropertyRect.height + EditorGUIUtility.standardVerticalSpacing;

            if (EditorGUI.EndChangeCheck())
            {
                criterionProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        private void OnCriterionTypeChanged(SerializedProperty parentProperty)
        {
            SerializedProperty criterionProperty = parentProperty.FindPropertyRelative(k_CriterionField);

            if (criterionProperty.objectReferenceValue != null)
                Undo.DestroyObjectImmediate(criterionProperty.objectReferenceValue);

            Type criterionType = Type.GetType(
                parentProperty.FindPropertyRelative(k_TypeField).FindPropertyRelative("m_TypeName").stringValue
            );

            if (criterionType != null)
            {
                ScriptableObject criterion = ScriptableObject.CreateInstance(criterionType);
                Undo.RegisterCreatedObjectUndo(criterion, "Change Criterion");
                criterion.hideFlags |= HideFlags.HideInHierarchy;
                criterion.name = criterionType.Name;

                AssetDatabase.AddObjectToAsset(criterion, parentProperty.serializedObject.targetObject);

                criterionProperty.objectReferenceValue = criterion;

                m_PerPropertyCriterionSerializedObjects.Clear();
            }
            else
            {
                criterionProperty.objectReferenceValue = null;
            }

            AssetDatabase.SaveAssets();
            parentProperty.serializedObject.ApplyModifiedProperties();
        }

        private IEnumerator ImportCriterionParentAssetWhenReady(SerializedProperty criterionProperty, ScriptableObject criterion, string parentAssetPath)
        {
            do
            {
                yield return null;
            }
            while (criterionProperty.objectReferenceValue != criterion);

            //this seems to be necessary in order to prevent errors when multiple criteria are on the same tutorial page
            m_InspectorRedrawn = false;

            do
            {
                yield return null;
            }
            while (!m_InspectorRedrawn);

            AssetDatabase.ImportAsset(parentAssetPath);
        }
    }
}
