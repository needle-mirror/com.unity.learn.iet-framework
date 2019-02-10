using Unity.InteractiveTutorials;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Readme))]
public class ReadmeEditor : Editor
{
    bool m_IsAuthoringMode;

    void OnEnable()
    {
        m_IsAuthoringMode = ProjectMode.IsAuthoringMode();
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Walkthroughs"))
            TutorialWindow.CreateWindow();

        if (m_IsAuthoringMode)
            base.OnInspectorGUI();
    }
}
