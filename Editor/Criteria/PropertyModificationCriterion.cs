using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityObject = UnityEngine.Object;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Criterion for checking a property modification.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class PropertyModificationCriterion : Criterion
    {
        internal enum ValueMode
        {
            TargetValue = 0,
            DifferentThanInitial
        }

        internal enum ValueType
        {
            Integer,
            Decimal,
            Text,
            Boolean,
            Color,
        }

        internal string PropertyPath { get => m_PropertyPath; set => m_PropertyPath = value; }
        [Tooltip("Serialized property path to watch on the target object.")]
        [SerializeField] private string m_PropertyPath;

        internal ValueMode TargetValueMode { get => m_TargetValueMode; set => m_TargetValueMode = value; }

        [Tooltip("Whether to compare against a target value or just require a change from the initial value.")]
        [SerializeField] private ValueMode m_TargetValueMode = ValueMode.TargetValue;

        // TODO: Make this more like TypedCriterion
        internal string TargetValue { get => m_TargetValue; set => m_TargetValue = value; }
        [SerializeField]
        [Tooltip("This value only applies if the TargetValueMode is set to TargetValue. This field will have no effects in other modes.")]
        private string m_TargetValue;

        internal ValueType TargetValueType { get => m_TargetValueType; set => m_TargetValueType = value; }
        [Tooltip("Data type used to parse and compare the target value.")]
        [SerializeField] private ValueType m_TargetValueType;

        internal SceneObjectReference Target { get => m_Target.SceneObjectReference; set => m_Target.SceneObjectReference = value; }
        [Tooltip("Object whose property is being watched.")]
        [SerializeField] private ObjectReference m_Target = new();

        [NonSerialized] private string m_InitialValue;

        /// <summary>
        /// Starts testing of the criterion.
        /// </summary>
        public override void StartTesting()
        {
            base.StartTesting();
            UnityObject target = m_Target.SceneObjectReference.ReferencedObject;
            if (m_TargetValueMode == ValueMode.TargetValue)
                IsCompleted = PropertyFulfillCriterion(target, m_PropertyPath);
            else
            {
                SerializedObject so = new(target);
                SerializedProperty sp = so.FindProperty(PropertyPath);

                if (sp == null)
                    Debug.LogWarningFormat("PropertyModificationCriterion: Cannot find property \"{0}\" on \"{1}\"", PropertyPath, target);
                else
                    m_InitialValue = GetPropertyValueAsString(sp);
            }

            Undo.postprocessModifications += PostprocessModifications;
            Undo.undoRedoPerformed += UpdateCompletion;
        }

        /// <summary>
        /// Stops testing of the criterion.
        /// </summary>
        public override void StopTesting()
        {
            base.StopTesting();
            Undo.postprocessModifications -= PostprocessModifications;
            Undo.undoRedoPerformed -= UpdateCompletion;
        }

        /// <summary>
        /// Evaluates if the criterion is completed.
        /// </summary>
        /// <returns>True if the right property were modified, false otherwise</returns>
        protected override bool EvaluateCompletion()
        {
            UnityObject targetObject = m_Target.SceneObjectReference.ReferencedObject;
            return PropertyFulfillCriterion(targetObject, m_PropertyPath);
        }

        private UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
        {
            UnityObject targetObject = m_Target.SceneObjectReference.ReferencedObject;
            IEnumerable<PropertyModification> modificationsToTest = GetPropertiesToTest(modifications, targetObject);
            if (modificationsToTest.Any())
            {
                IsCompleted = modificationsToTest.Any(m => PropertyFulfillCriterion(m.target, m.propertyPath));
            }

            return modifications;
        }

        private IEnumerable<PropertyModification> GetPropertiesToTest(UndoPropertyModification[] modifications, UnityObject target)
        {
            List<PropertyModification> result = new();
            foreach (UndoPropertyModification m in modifications)
            {
                if (m.currentValue.target == target)
                {
                    if (IsCompoundPropertyMatch(m.currentValue.propertyPath))
                    {
                        PropertyModification propertyModification = m.currentValue;
                        propertyModification.propertyPath = PropertyPath;
                        result.Add(m.currentValue);
                    }
                    else if (m.currentValue.propertyPath == m_PropertyPath)
                        result.Add(m.currentValue);
                }
            }
            return result;
        }

        private bool IsCompoundPropertyMatch(string propertyPath)
        {
            if (m_TargetValueType == ValueType.Color)
            {
                Regex coloRegex = new(m_PropertyPath + "\\.[rgba]");
                if (coloRegex.IsMatch(propertyPath))
                    return true;
            }
            return propertyPath == m_PropertyPath;
        }

        private bool DoPropertyTypeMatches(SerializedProperty property)
        {
            switch (m_TargetValueType)
            {
                case ValueType.Decimal:
                    return property.propertyType == SerializedPropertyType.Float;
                case ValueType.Integer:
                    return property.propertyType == SerializedPropertyType.Integer;
                case ValueType.Text:
                    return property.propertyType == SerializedPropertyType.String;
                case ValueType.Boolean:
                    return property.propertyType == SerializedPropertyType.Boolean;
                case ValueType.Color:
                    return property.propertyType == SerializedPropertyType.Color;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            throw new Exception("unknown TargetValueType");
        }

        private string GetPropertyValueAsString(SerializedProperty property)
        {
            switch (TargetValueType)
            {
                case ValueType.Decimal:
                    return property.floatValue.ToString();
                case ValueType.Integer:
                    return property.intValue.ToString();
                case ValueType.Text:
                    return property.stringValue;
                case ValueType.Boolean:
                    return property.boolValue.ToString();
                case ValueType.Color:
                    return property.colorValue.ToString();
            }

            throw new Exception("unknown TargetValueType");
        }

        private bool DoesPropertyMatches(SerializedProperty property, string value)
        {
            switch (TargetValueType)
            {
                case ValueType.Decimal:
                {
                    return float.TryParse(value, out float convertedValue) &&
                           Mathf.Approximately(property.floatValue, convertedValue);
                }

                case ValueType.Integer:
                {
                    return int.TryParse(value, out int convertedValue) && property.intValue == convertedValue;
                }
                case ValueType.Text:
                {
                    return property.stringValue == value;
                }
                case ValueType.Boolean:
                {
                    return bool.TryParse(value, out bool convertedValue) && property.boolValue == convertedValue;
                }
                case ValueType.Color:
                {
                    return ColorUtility.TryParseHtmlString(value, out Color convertedValue) && property.colorValue == convertedValue;
                }
            }

            return false;
        }

        private bool SetPropertyTo(SerializedProperty property, string value)
        {
            switch (TargetValueType)
            {
                case ValueType.Decimal:
                {
                    if (!float.TryParse(value, out float convertedTargetValue))
                        return false;

                    property.floatValue = convertedTargetValue;
                    return true;
                }
                case ValueType.Integer:
                {
                    if (!int.TryParse(value, out int convertedTargetValue))
                        return false;

                    property.intValue = convertedTargetValue;
                    return true;
                }
                case ValueType.Text:
                {
                    property.stringValue = value;
                    return true;
                }
                case ValueType.Boolean:
                {
                    if (!bool.TryParse(value, out bool convertedTargetValue))
                        return false;
                    property.boolValue = convertedTargetValue;
                    return true;
                }
                case ValueType.Color:
                {
                    if (!ColorUtility.TryParseHtmlString(value, out Color convertedTargetValue))
                        return false;
                    property.colorValue = convertedTargetValue;
                    return true;
                }
            }
            return false;
        }

        private bool SetPropertyToDifferentValueThan(SerializedProperty property, string value)
        {
            switch (TargetValueType)
            {
                case ValueType.Decimal:
                {
                    if (!float.TryParse(value, out float convertedTargetValue))
                        return false;

                    property.floatValue = convertedTargetValue + 1.0f;
                    return true;
                }
                case ValueType.Integer:
                {
                    if (!int.TryParse(value, out int convertedTargetValue))
                        return false;

                    property.intValue = convertedTargetValue + 1;
                    return true;
                }
                case ValueType.Text:
                {
                    property.stringValue = value + "different ";
                    return true;
                }
                case ValueType.Boolean:
                {
                    if (!bool.TryParse(value, out bool convertedTargetValue))
                        return false;
                    property.boolValue = !convertedTargetValue;
                    return true;
                }
                case ValueType.Color:
                {
                    if (!ColorUtility.TryParseHtmlString(value, out Color convertedTargetValue))
                        return false;
                    property.colorValue = convertedTargetValue + Color.gray;
                    return true;
                }
            }
            return false;
        }

        private bool PropertyFulfillCriterion(UnityObject target, string propertyPath)
        {
            if (target == null)
                return false;

            if (m_TargetValueMode == ValueMode.TargetValue &&  m_TargetValueType != ValueType.Text && string.IsNullOrEmpty(m_TargetValue))
                return true;

            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);

            if (property == null)
                return false;

            if (!DoPropertyTypeMatches(property))
                return false;

            switch (m_TargetValueMode)
            {
                case ValueMode.TargetValue:
                    return DoesPropertyMatches(property, m_TargetValue);
                case ValueMode.DifferentThanInitial:
                    return !DoesPropertyMatches(property, m_InitialValue);
            }

            return false;
        }

        /// <summary>
        /// Auto-completes the criterion.
        /// </summary>
        /// <returns>True if the auto-completion succeeded.</returns>
        public override bool AutoComplete()
        {
            UnityObject target = m_Target.SceneObjectReference.ReferencedObject;
            if (target == null)
                return false;

            if (m_TargetValueMode == ValueMode.TargetValue && m_TargetValueType != ValueType.Text && string.IsNullOrEmpty(m_TargetValue))
                return false;

            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.FindProperty(m_PropertyPath);

            if (property == null)
                return false;

            if (!DoPropertyTypeMatches(property))
                return false;

            switch (m_TargetValueMode)
            {
                case ValueMode.TargetValue:
                {
                    if (!SetPropertyTo(property, TargetValue))
                        return false;
                    break;
                }
                case ValueMode.DifferentThanInitial:
                {
                    if (!SetPropertyToDifferentValueThan(property, m_InitialValue))
                        return false;
                    break;
                }
            }

            serializedObject.ApplyModifiedProperties();

            return true;
        }
    }
}
