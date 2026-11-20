using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Criterion for checking that a specific object is framed in the Scene view.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class FrameSelectedCriterion : Criterion
    {
        [Serializable]
        private class ObjectReferenceCollection : CollectionWrapper<ObjectReference>
        {
        }

        [Tooltip("Objects that must be framed in the Scene view for the criterion to complete.")]
        [SerializeField] private ObjectReferenceCollection m_ObjectReferences = new();

        /// <summary>
        /// Sets object references.
        /// </summary>
        /// <param name="objectReferences">A list of ObjectReference the Criterion need to check if selected</param>
        public void SetObjectReferences(IEnumerable<ObjectReference> objectReferences)
        {
            m_ObjectReferences.SetItems(objectReferences);
            UpdateCompletion();
        }

        /// <summary>
        /// Starts testing of the criterion.
        /// </summary>
        public override void StartTesting()
        {
            base.StartTesting();
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
        }

        /// <summary>
        /// Stops testing of the criterion.
        /// </summary>
        public override void StopTesting()
        {
            base.StopTesting();
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif
        }

        /// <summary>
        /// Runs update logic for the criterion.
        /// </summary>
        /// <remarks>
        /// Overriding the update completion state, as this criterion is not state based
        /// </remarks>
        public override void UpdateCompletion()
        {
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            Event evt = Event.current;
            if (evt.type == EventType.ExecuteCommand && evt.commandName == "FrameSelected")
            {
                if (m_ObjectReferences.Count == 0)
                    return;

                Object[] selection = Selection.objects;
                if (m_ObjectReferences.Count != selection.Length)
                    return;

                foreach (ObjectReference objectReference in m_ObjectReferences)
                {
                    Object referencedObject = objectReference.SceneObjectReference.ReferencedObject;
                    if (referencedObject == null)
                        return;

                    if (Array.IndexOf(selection, referencedObject) < 0)
                        return;
                }

                IsCompleted = true;
            }
        }

        /// <summary>
        /// Auto-completes the criterion.
        /// </summary>
        /// <returns>True if the auto-completion succeeded.</returns>
        public override bool AutoComplete()
        {
            if (m_ObjectReferences.Count == 0)
            {
                Debug.LogWarning("Cannot auto-complete FrameSelectedCriterion with no object references");
                return false;
            }

            Object[] referencedObjects = new Object[m_ObjectReferences.Count];
            int referencedObjectIndex = 0;
            foreach (ObjectReference objectReference in m_ObjectReferences)
            {
                Object referencedObject = objectReference.SceneObjectReference.ReferencedObject;
                if (referencedObject == null)
                {
                    Debug.LogWarning("Cannot auto-complete FrameSelectedCriterion with unresolved object references");
                    return false;
                }

                referencedObjects[referencedObjectIndex++] = referencedObject;
            }

            if (SceneView.lastActiveSceneView == null)
            {
                Debug.LogWarning("Cannot auto-complete FrameSelectedCriterion when there is no last active Scene View");
                return false;
            }

            Object previousActiveObject = Selection.activeObject;
            Object[] previousSelection = (Object[])Selection.objects.Clone();

            Selection.objects = referencedObjects;

            if (!SceneView.FrameLastActiveSceneView())
            {
                Debug.LogWarning("Cannot auto-complete FrameSelectedCriterion since framing last active Scene View failed");

                // Restore selection
                Selection.activeObject = previousActiveObject;
                Selection.objects = previousSelection;

                return false;
            }

            return true;
        }
    }
}
