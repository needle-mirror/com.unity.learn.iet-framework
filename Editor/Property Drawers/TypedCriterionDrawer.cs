using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Core.Editor
{
    [CustomPropertyDrawer(typeof(TypedCriterion))]
    class TypedCriterionDrawer : PropertyDrawer
    {
        // criterionProperty is a SerializedProperty on the SerializedObject for the Criterion
        delegate void PropertyIteratorCallback(SerializedProperty criterionProperty);

        const string k_TypeField = "Type";
        const string k_CriterionField = "Criterion";

        Dictionary<String, SerializedObject> m_PerPropertyCriterionSerializedObjects =
            new Dictionary<String, SerializedObject>();

        Rect m_CriterionPropertyRect;
        bool m_InspectorRedrawn = false;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            property.isExpanded = true;
            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            IterateCriterion(
                property.FindPropertyRelative(k_CriterionField),
                p => height += EditorGUI.GetPropertyHeight(p) + EditorGUIUtility.standardVerticalSpacing
            );
            height -= EditorGUIUtility.standardVerticalSpacing;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_InspectorRedrawn = true;
            EditorGUI.BeginChangeCheck();
            Rect typeFieldPosition = position;
            typeFieldPosition.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(typeFieldPosition, property.FindPropertyRelative(k_TypeField));
            if (EditorGUI.EndChangeCheck() || GUI.changed)
            {
                OnCriterionTypeChanged(property);
            }

            position.y += typeFieldPosition.height + EditorGUIUtility.standardVerticalSpacing;
            position.height -= typeFieldPosition.height + EditorGUIUtility.standardVerticalSpacing;
            m_CriterionPropertyRect = position;
            IterateCriterion(property.FindPropertyRelative(k_CriterionField), OnGUIIterateCriterion);
        }

        SerializedObject GetSerializedObject(SerializedProperty criterionProperty)
        {
            if (criterionProperty.objectReferenceValue == null)
                return null;

            string key = criterionProperty.propertyPath;
            SerializedObject serializedObject;
            var found = m_PerPropertyCriterionSerializedObjects.TryGetValue(key, out serializedObject);
            if (!found || serializedObject.targetObject == null)
            {
                serializedObject = new SerializedObject(criterionProperty.objectReferenceValue);
                m_PerPropertyCriterionSerializedObjects[key] = serializedObject;
            }

            return serializedObject;
        }

        void IterateCriterion(SerializedProperty criterion, PropertyIteratorCallback onIterateChildProperty)
        {
            if (criterion.objectReferenceValue == null)
                return;

            var serializedObject = GetSerializedObject(criterion);
            if (serializedObject == null)
                return;

            var childProperty = serializedObject.GetIterator();
            childProperty.NextVisible(true);
            while (childProperty.NextVisible(childProperty.isExpanded))
            {
                if (string.Equals(childProperty.propertyPath, "m_Script", StringComparison.Ordinal))
                    continue;

                onIterateChildProperty(childProperty);
            }
        }

        void OnGUIIterateCriterion(SerializedProperty criterionProperty)
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

        void OnCriterionTypeChanged(SerializedProperty parentProperty)
        {
            var criterionProperty = parentProperty.FindPropertyRelative(k_CriterionField);

            if (criterionProperty.objectReferenceValue != null)
                Undo.DestroyObjectImmediate(criterionProperty.objectReferenceValue);

            var criterionType = System.Type.GetType(
                parentProperty.FindPropertyRelative(k_TypeField).FindPropertyRelative("m_TypeName").stringValue
            );

            if (criterionType != null)
            {
                var criterion = ScriptableObject.CreateInstance(criterionType);
                Undo.RegisterCreatedObjectUndo(criterion, "Change Criterion");
                criterion.hideFlags |= HideFlags.HideInHierarchy;

                AssetDatabase.AddObjectToAsset(criterion, parentProperty.serializedObject.targetObject);
                string parentAssetPath = AssetDatabase.GetAssetPath(parentProperty.serializedObject.targetObject);

                // Work around "NullReferenceException: SerializedObject of SerializedProperty has been Disposed.",
                // https://fogbugz.unity3d.com/f/cases/1318338/
#if UNITY_2020_2_6 || UNITY_2020_2_7 || (UNITY_2020_3_OR_NEWER && !UNITY_2021)
                EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(ImportCriterionParentAssetWhenReady(criterionProperty, criterion, parentAssetPath));
#else
                AssetDatabase.ImportAsset(parentAssetPath);
#endif
                criterionProperty.objectReferenceValue = criterion;

                m_PerPropertyCriterionSerializedObjects.Clear();
            }
            else
            {
                criterionProperty.objectReferenceValue = null;
            }
        }

        IEnumerator ImportCriterionParentAssetWhenReady(SerializedProperty criterionProperty, ScriptableObject criterion, string parentAssetPath)
        {
            do
            {
                yield return null;
            } while (criterionProperty.objectReferenceValue != criterion);

            //this seems to be necessary in order to prevent errors when multiple criteria are on the same tutorial page
            m_InspectorRedrawn = false;

            do
            {
                yield return null;
            } while (!m_InspectorRedrawn);

            AssetDatabase.ImportAsset(parentAssetPath);
        }
    }
}
