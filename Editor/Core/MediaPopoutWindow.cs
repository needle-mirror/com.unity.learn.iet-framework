using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Tutorials.Core.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

internal class MediaPopoutWindow : EditorWindow
{
    private VisualElement m_OriginalContainer;
    private int m_OriginalPositionIndex;

    private VisualElement m_PopOutElement;

    void Awake()
    {
        UIElementsUtils.LoadCommonStyleSheet(rootVisualElement);
    }

    private void OnFocus()
    {
        //needed to receive key event
        rootVisualElement.focusable = true;
        rootVisualElement.Focus();
        rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyPressed);
    }

    private void OnLostFocus()
    {
        //remove focus from the root
        rootVisualElement.Blur();
        rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyPressed);
    }

    void OnKeyPressed(KeyDownEvent evt)
    {
        if(evt.keyCode == KeyCode.Escape)
            Close();
    }

    public static void Popout(VisualElement element)
    {
        var win = CreateInstance<MediaPopoutWindow>();
        win.ShowUtility();

        win.m_PopOutElement = element;

        win.m_OriginalContainer = element.parent;
        win.m_OriginalPositionIndex = win.m_OriginalContainer.IndexOf(element);

        win.rootVisualElement.Add(element);
        win.m_PopOutElement.AddToClassList("popout-media");
    }

    //This is called by the tutorial window when changing page to make sure we don't have any left over pop out media
    public static void EnsureClosed()
    {
        if (HasOpenInstances<MediaPopoutWindow>())
        {
            var win = GetWindow<MediaPopoutWindow>();
            win.Close();
        }
    }
    private void OnDestroy()
    {
        m_PopOutElement.RemoveFromClassList("popout-media");
        if (m_OriginalContainer != null)
        {
            m_OriginalContainer.Insert(m_OriginalPositionIndex, m_PopOutElement);
        }
    }
}
