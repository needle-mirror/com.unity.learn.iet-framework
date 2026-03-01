using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Editor
{
    [CustomPropertyDrawer(typeof(SceneObjectReference))]
    internal class SceneObjectReferencePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SceneObjectReference sor = new(property);

            Color origColor = GUI.color;
            if (!sor.ReferenceResolved)
            {
                label.text = "(Not resolved) " + label.text;
                GUI.color = Color.red;
            }

            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);
            GUI.color = origColor;

            Object obj = sor.ReferencedObject;
            if (!sor.ReferenceResolved)
            {
                obj = sor.ReferenceScene;
            }

            EditorGUI.BeginChangeCheck();
            Object newObj = EditorGUI.ObjectField(position, obj, typeof(Object), true);
            if (EditorGUI.EndChangeCheck())
            {
                sor.Update(newObj);
            }

            EditorGUI.EndProperty();
        }
    }
}
