using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.Tutorials.Editor
{
    [CustomPropertyDrawer(typeof(ObjectReference))]
    internal class ObjectReferencePropertyDrawer : PropertyDrawer
    {
        private const string k_SceneObjectReferencePath = "m_SceneObjectReference";
        private const string k_FutureObjectReferencePath = "m_FutureObjectReference";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty sceneObjectReferenceProperty = property.FindPropertyRelative(k_SceneObjectReferencePath);
            SerializedProperty futureObjectReferenceProperty = property.FindPropertyRelative(k_FutureObjectReferencePath);

            Color origColor = GUI.color;

            UnityObject obj;
            SceneObjectReference sceneObjectReference = null;

            if (futureObjectReferenceProperty.objectReferenceValue != null)
            {
                label.text = "(Future) " + label.text;
                GUI.color = Color.cyan;

                obj = futureObjectReferenceProperty.objectReferenceValue;
            }
            else
            {
                sceneObjectReference = new SceneObjectReference(sceneObjectReferenceProperty);

                if (!sceneObjectReference.ReferenceResolved)
                {
                    label.text = "(Not resolved) " + label.text;
                    GUI.color = Color.red;
                }

                obj = sceneObjectReference.ReferencedObject;
                if (!sceneObjectReference.ReferenceResolved)
                {
                    obj = sceneObjectReference.ReferenceScene;
                }
            }

            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);
            GUI.color = origColor;

            EditorGUI.BeginChangeCheck();
            Object newObj = EditorGUI.ObjectField(position, obj, typeof(Object), true);
            if (EditorGUI.EndChangeCheck())
            {
                if (newObj is FutureObjectReference)
                    futureObjectReferenceProperty.objectReferenceValue = newObj;
                else
                {
                    futureObjectReferenceProperty.objectReferenceValue = null;

                    if (sceneObjectReference == null)
                        sceneObjectReference = new SceneObjectReference(sceneObjectReferenceProperty);
                    sceneObjectReference.Update(newObj);
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
