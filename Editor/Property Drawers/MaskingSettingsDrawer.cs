using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    [CustomPropertyDrawer(typeof(MaskingSettings))]
    internal class MaskingSettingsDrawer : PropertyDrawer
    {
        private const string k_EnabledPath = "m_MaskingEnabled";
        private const string k_UnmaskedViewsPath = "m_UnmaskedViews";

        private SerializedProperty m_Property;
        private SerializedProperty m_unmaskedViewsProperty;
        private SerializedProperty m_presetProperty;

        private ListView m_unmaskedViewsList;
        private VisualElement m_collapsableVisualElement;
        private ObjectField m_maskingPresetField;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_Property = property;
            SerializedProperty enabledProperty = property.FindPropertyRelative(k_EnabledPath);
            m_unmaskedViewsProperty = m_Property.FindPropertyRelative(k_UnmaskedViewsPath);
            m_presetProperty = m_Property.FindPropertyRelative(nameof(MaskingSettings.MaskPreset));

            VisualElement root = new();

            PropertyField enableField = new(enabledProperty);
            root.Add(enableField);

            m_collapsableVisualElement = new(){ name = "Collapsable" };

            // Presets line
            m_maskingPresetField = new("Preset")
            {
                objectType = typeof(MaskingPreset),
            };
            m_maskingPresetField.Q<VisualElement>(className: "unity-base-field__input").style.height = 20;
            m_maskingPresetField.BindProperty(property.FindPropertyRelative(nameof(MaskingSettings.MaskPreset)));
            m_maskingPresetField.AddToClassList(ObjectField.alignedFieldUssClassName);
            m_maskingPresetField.RegisterValueChangedCallback(OnPresetChanged);

            Button saveAsPresetButton = new(OnSaveAsPresetButtonClicked)
            {
                text = "Save as Preset",
                tooltip = "Save the MaskingSettings below as a new MaskingPreset, to be able to reuse them in different pages.",
                style = { flexShrink = 1 }
            };
            m_maskingPresetField.Add(saveAsPresetButton);

            m_collapsableVisualElement.Add(m_maskingPresetField);

            m_unmaskedViewsList = new UnmaskedViewsListView(m_unmaskedViewsProperty);
            m_collapsableVisualElement.Add(m_unmaskedViewsList);

            root.Add(m_collapsableVisualElement);

            enableField.RegisterValueChangeCallback(evt => OnShowHideSettings(evt.changedProperty.boolValue));

            return root;
        }

        /// <summary>
        /// Saves the current MaskingSettings as a new MaskingPreset ScriptableObject,
        /// and connects it as the current preset in the preset field.
        /// </summary>
        private void OnSaveAsPresetButtonClicked()
        {
            m_Property.serializedObject.Update();

            string path = EditorUtility.SaveFilePanelInProject("Save MaskingPreset", "New Masking Preset",
                "asset", "Save current masking settings as a re-usable preset.");

            bool isValidPreset = !string.IsNullOrEmpty(path);
            if (isValidPreset)
            {
                MaskingPreset newSO = ScriptableObject.CreateInstance<MaskingPreset>();
                MaskingSettings maskingSettings = (MaskingSettings)m_Property.boxedValue;
                newSO.m_unmaskedViews = new List<UnmaskedView>();
                foreach (UnmaskedView view in maskingSettings.UnmaskedViews)
                    newSO.m_unmaskedViews.Add(view);

                AssetDatabase.CreateAsset(newSO, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                m_maskingPresetField.SetValueWithoutNotify(newSO);
                m_presetProperty.objectReferenceValue = newSO;
            }
            else
                m_presetProperty.objectReferenceValue = null;

            UpdateListInteractivity(!isValidPreset);

            m_Property.serializedObject.ApplyModifiedProperties();
        }

        private void OnShowHideSettings(bool val)
        {
            UIUtils.ShowOrHide(m_collapsableVisualElement, val);
        }

        private void OnPresetChanged(ChangeEvent<Object> evt)
        {
            m_Property.serializedObject.Update();

            UpdateListInteractivity(evt.newValue == null);

            int undoGroup = Undo.GetCurrentGroup();

            if (evt.newValue != null)
            {
                MaskingPreset preset = (MaskingPreset)evt.newValue;
                m_unmaskedViewsProperty.arraySize = preset.m_unmaskedViews.Count;
                for (int i = 0; i < preset.m_unmaskedViews.Count; i++)
                {
                    m_unmaskedViewsProperty.GetArrayElementAtIndex(i).boxedValue = preset.m_unmaskedViews[i];
                }
            }

            m_Property.serializedObject.ApplyModifiedProperties();

            Undo.SetCurrentGroupName("Apply Masking Preset");
            Undo.CollapseUndoOperations(undoGroup);
        }

        /// <summary>
        /// Enables/disables the ability to modify the list of UnmaskedViews,
        /// based on whether there is an assigned MaskingPreset or not.
        /// </summary>
        private void UpdateListInteractivity(bool isEnabled)
        {
            m_unmaskedViewsList.SetEnabled(isEnabled);
            m_unmaskedViewsList.tooltip = isEnabled ? "" : "Masking Settings for this paragraph are contained in the Masking Preset referenced above.\n\n" +
                                                           "Remove the Masking Preset to be able to edit Masking Settings for this paragraph independently.";
        }
    }
}
