using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Criterion for checking that specific objects are selected.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class RequiredSelectionCriterion : Criterion
    {
        [Serializable]
        private class ObjectReferenceCollection : CollectionWrapper<ObjectReference>
        {
        }

        [Tooltip("Objects that must all be selected for the criterion to complete.")]
        [SerializeField] private ObjectReferenceCollection m_ObjectReferences = new();

        /// <summary>
        /// Sets object references.
        /// </summary>
        /// <param name="objectReferences">The ObjectReference list that need to be selected for this Criterion to be satisfied</param>
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
            UpdateCompletion();
            Selection.selectionChanged += UpdateCompletion;
        }

        /// <summary>
        /// Stops testing of the criterion.
        /// </summary>
        public override void StopTesting()
        {
            base.StopTesting();
            Selection.selectionChanged -= UpdateCompletion;
        }

        /// <summary>
        /// Evaluates if the criterion is completed.
        /// </summary>
        /// <returns>True if the selection is the right one, false otherwise</returns>
        protected override bool EvaluateCompletion()
        {
            Object[] selection = Selection.objects;
            if (m_ObjectReferences.Count != selection.Length)
                return false;

            foreach (ObjectReference objectReference in m_ObjectReferences)
            {
                Object referencedObject = objectReference.SceneObjectReference.ReferencedObject;
                if (referencedObject == null)
                    return false;

                if (Array.IndexOf(selection, referencedObject) < 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Auto-completes the criterion.
        /// </summary>
        /// <returns>True if the auto-completion succeeded.</returns>
        public override bool AutoComplete()
        {
            Object[] referencedObjects = new Object[m_ObjectReferences.Count];
            int referencedObjectIndex = 0;
            foreach (ObjectReference objectReference in m_ObjectReferences)
            {
                Object referencedObject = objectReference.SceneObjectReference.ReferencedObject;
                if (referencedObject == null)
                {
                    Debug.LogWarning("Cannot auto-complete RequiredSelectionCriterion with unresolved object references");
                    return false;
                }

                referencedObjects[referencedObjectIndex++] = referencedObject;
            }

            Selection.objects = referencedObjects;
            return true;
        }
    }
}
