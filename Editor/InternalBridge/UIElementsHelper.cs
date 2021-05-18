using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using UnityEngine.UIElements;

namespace Unity.Tutorials.Core.Editor
{
    // Handle difference in UIElements/UIToolkit APIs between different Unity versions.
    // Initialize on load to surface potential reflection issues immediately.
    [InitializeOnLoad]
    internal static class UIElementsHelper
    {
        static PropertyInfo s_VisualTreeProperty;

        static UIElementsHelper()
        {
            s_VisualTreeProperty = GetProperty<GUIView>("visualTree", typeof(VisualElement))
                ?? GetProperty<GUIView>("visualTree", typeof(VisualElement))
#if UNITY_2020_1_OR_NEWER
                ?? GetProperty<IWindowBackend>("visualTree", typeof(object))
#endif
                ;
            if (s_VisualTreeProperty == null)
                Debug.LogError("Cannot find property GUIView/IWindowBackend.visualTree");
        }

        static PropertyInfo GetProperty<T>(string name, Type returnType)
        {
            return typeof(T).GetProperty(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                returnType,
                new Type[0],
                null
            );
        }

        public static void Add(GUIViewProxy view, VisualElement child)
        {
            if (view.IsDockedToEditor())
            {
                GetVisualTree(view).Add(child);
            }
            else
            {
                foreach (var visualElement in GetVisualTree(view).Children())
                {
                    visualElement.Add(child);
                }
            }
        }

        public static VisualElement GetVisualTree(GUIViewProxy guiViewProxy)
        {
            return (VisualElement)s_VisualTreeProperty.GetValue(
#if UNITY_2020_1_OR_NEWER
                guiViewProxy.GuiView.windowBackend,
#else
                guiViewProxy.GuiView,
#endif
                new object[0]
            );
        }

        public static void SetLayout(this VisualElement element, Rect rect)
        {
            var style = element.style;
            style.position = Position.Absolute;
            style.marginLeft = 0.0f;
            style.marginRight = 0.0f;
            style.marginBottom = 0.0f;
            style.marginTop = 0.0f;
            style.left = rect.x;
            style.top = rect.y;
            style.right = float.NaN;
            style.bottom = float.NaN;
            style.width = rect.width;
            style.height = rect.height;
        }
    }
}
