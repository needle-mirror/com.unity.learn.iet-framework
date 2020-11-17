using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// Base class for Criterion implementations.
    /// </summary>
    public abstract class Criterion : ScriptableObject
    {
        /// <summary>
        /// Raised when any Criterion is completed.
        /// </summary>
        public static event Action<Criterion> CriterionCompleted;

        /// <summary>
        /// Raised when any Criterion is invalidated.
        /// </summary>
        public static event Action<Criterion> CriterionInvalidated;

        bool m_Completed;

        /// <summary>
        /// Is the Criterion completed. Setting this raises CriterionCompleted/CriterionInvalidated.
        /// </summary>
        public bool Completed
        {
            get { return m_Completed; }
            protected set
            {
                if (value == m_Completed)
                    return;

                m_Completed = value;
                if (m_Completed)
                    CriterionCompleted?.Invoke(this);
                else
                    CriterionInvalidated?.Invoke(this);
            }
        }

        /// <summary>
        /// Resets the completion state.
        /// </summary>
        public void ResetCompletionState()
        {
            m_Completed = false;
        }

        /// <summary>
        /// Starts testing of the criterion.
        /// </summary>
        public virtual void StartTesting()
        {
        }

        /// <summary>
        /// Stops testing of the criterion.
        /// </summary>
        public virtual void StopTesting()
        {
        }

        /// <summary>
        /// Runs update logic for the criterion.
        /// </summary>
        public virtual void UpdateCompletion()
        {
            Completed = EvaluateCompletion();
        }

        /// <summary>
        /// Evaluates if the criterion is completed.
        /// </summary>
        /// <returns></returns>
        protected virtual bool EvaluateCompletion()
        {
            throw new NotImplementedException($"Missing implementation of EvaluateCompletion in: {GetType()}");
        }

        /// <summary>
        /// Auto-completes the criterion.
        /// </summary>
        /// <returns>True if the auto-completion succeeded.</returns>
        public abstract bool AutoComplete();

        /// <summary>
        /// Returns FutureObjectReference for this Criterion.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<FutureObjectReference> GetFutureObjectReferences()
        {
            return Enumerable.Empty<FutureObjectReference>();
        }

        /// <summary>
        /// Destroys unreferenced future references.
        /// </summary>
        /// <see>
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnValidate.html
        /// </see>
        protected virtual void OnValidate()
        {
            // Find instanceIDs of referenced future references
            var referencedFutureReferenceInstanceIDs = new HashSet<int>();
            foreach (var futureReference in GetFutureObjectReferences())
                referencedFutureReferenceInstanceIDs.Add(futureReference.GetInstanceID());

            // Destroy unreferenced future references
            var assetPath = AssetDatabase.GetAssetPath(this);
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in assets)
            {
                if (asset is FutureObjectReference
                    && ((FutureObjectReference)asset).Criterion == this
                    && !referencedFutureReferenceInstanceIDs.Contains(asset.GetInstanceID()))
                {
                    DestroyImmediate(asset, true);
                }
            }
        }

        /// <summary>
        /// Creates a default FutureObjectReference for this Criterion.
        /// </summary>
        /// <returns></returns>
        protected FutureObjectReference CreateFutureObjectReference()
        {
            return CreateFutureObjectReference("Future Reference");
        }

        /// <summary>
        /// Creates a FutureObjectReference by specific name for this Criterion.
        /// </summary>
        /// <param name="referenceName"></param>
        /// <returns></returns>
        protected FutureObjectReference CreateFutureObjectReference(string referenceName)
        {
            var futureReference = CreateInstance<FutureObjectReference>();
            futureReference.Criterion = this;
            futureReference.ReferenceName = referenceName;

            var assetPath = AssetDatabase.GetAssetPath(this);
            AssetDatabase.AddObjectToAsset(futureReference, assetPath);

            return futureReference;
        }

        /// <summary>
        /// Updates names of the references.
        /// </summary>
        protected void UpdateFutureObjectReferenceNames()
        {
            // Update future reference names in next editor update due to AssetDatase interactions
            EditorApplication.update += UpdateFutureObjectReferenceNamesPostponed;
        }

        void UpdateFutureObjectReferenceNamesPostponed()
        {
            // Unsubscribe immediately since it should only be called once
            EditorApplication.update -= UpdateFutureObjectReferenceNamesPostponed;

            var assetPath = AssetDatabase.GetAssetPath(this);
            var tutorialPage = (TutorialPage)AssetDatabase.LoadMainAssetAtPath(assetPath);
            var futureReferences = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .Where(o => o is FutureObjectReference)
                .Cast<FutureObjectReference>();
            foreach (var futureReference in futureReferences)
                tutorialPage.UpdateFutureObjectReferenceName(futureReference);
        }
    }
}
