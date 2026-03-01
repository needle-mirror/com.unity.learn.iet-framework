using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    [CustomPropertyDrawer(typeof(CollectionWrapper), true)]
    internal class CollectionWrapperDrawer : PropertyDrawer
    {
        private const string k_ItemsPath = "m_Items";
        protected SerializedProperty m_ListProperty;
        protected SerializedProperty m_ItemsProperty;

        protected virtual void OnListViewCreated(ListView listView) { }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_ListProperty = property;
            m_ItemsProperty = property.FindPropertyRelative(k_ItemsPath);

            ListView listView = new()
            {
                name = property.displayName,
                reorderable = true,
                showAddRemoveFooter = true,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                reorderMode = ListViewReorderMode.Animated,
            };

            listView.AddToClassList("inspector-list");

            listView.makeHeader += () =>
            {
                Label label = new(property.displayName);
                label.AddToClassList("inspector-list-header");
                return label;
            };

            listView.makeItem += () =>
            {
                VisualElement root = new();

                root.AddToClassList("inspector-list-element");

                PropertyField propertyElement = new();
                root.Add(propertyElement);

                return root;
            };

            listView.bindItem += (element, i) =>
            {
                if(m_ItemsProperty.arraySize <= i)
                    return;

                PropertyField propField = element.Q<PropertyField>();
                propField.BindProperty(m_ItemsProperty.GetArrayElementAtIndex(i));
                // TODO: Is this needed? It yields an error (on objectReferenceValue)
                // propField.label = m_ItemsProperty.GetArrayElementAtIndex(i).objectReferenceValue.name;
            };

            listView.unbindItem += (element, i) =>
            {
                PropertyField propField = element.Q<PropertyField>();
                propField.Unbind();
            };

            listView.onRemove += view =>
            {
                int selectedIndex = view.selectedIndex;
                if (selectedIndex == -1) selectedIndex = m_ItemsProperty.arraySize-1; // When nothing is selected

                m_ItemsProperty.DeleteArrayElementAtIndex(selectedIndex);
                m_ItemsProperty.serializedObject.ApplyModifiedProperties();
            };

            listView.BindProperty(property.FindPropertyRelative(k_ItemsPath));

            OnListViewCreated(listView);

            return listView;
        }
    }
}
