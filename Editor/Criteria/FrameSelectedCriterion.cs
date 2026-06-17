using System;
using System.Collections.Generic;
using System.Linq;
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
                if (!m_ObjectReferences.Any())
                    return;

                if (m_ObjectReferences.Count() != Selection.objects.Length)
                    return;

                foreach (ObjectReference objectReference in m_ObjectReferences)
                {
                    Object referencedObject = objectReference.SceneObjectReference.ReferencedObject;
                    if (referencedObject == null)
                        return;

                    if (!Selection.objects.Contains(referencedObject))
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
            if (!m_ObjectReferences.Any())
            {
                Debug.LogWarning("Cannot auto-complete FrameSelectedCriterion with no object references");
                return false;
            }

            IEnumerable<Object> referencedObjects = m_ObjectReferences.Select(or => or.SceneObjectReference.ReferencedObject);
            if (referencedObjects.Any(obj => obj == null))
            {
                Debug.LogWarning("Cannot auto-complete FrameSelectedCriterion with unresolved object references");
                return false;
            }

            if (SceneView.lastActiveSceneView == null)
            {
                Debug.LogWarning("Cannot auto-complete FrameSelectedCriterion when there is no last active Scene View");
                return false;
            }

            Object previousActiveObject = Selection.activeObject;
            Object[] previousSelection = Selection.objects.ToArray();

            Selection.objects = referencedObjects.ToArray();

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
