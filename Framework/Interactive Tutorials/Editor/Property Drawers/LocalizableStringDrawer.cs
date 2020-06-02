using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    [CustomPropertyDrawer(typeof(LocalizableString), true)]
    class LocalizableStringDrawer : PropertyDrawer
    {
        static GUIContent s_IconContent;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var origIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var value = property.FindPropertyRelative(LocalizableString.PropertyPath);
            EditorGUI.PropertyField(position, value, GUIContent.none);
            position.x -= 25;
            EditorGUI.LabelField(position, IconContent());
            EditorGUI.EndProperty();
            EditorGUI.indentLevel = origIndentLevel;
        }

        static GUIContent IconContent()
        {
            if (s_IconContent == null)
                s_IconContent = EditorGUIUtility.IconContent("console.infoicon.sml"); // TODO create and use a proper localization/translation icon
            s_IconContent.tooltip = Localization.Tr("Localizable string");
            return s_IconContent;
        }
    }
}
