#region

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

#endregion

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// A generic event for signaling changes in a criterion.
    /// Parameters: sender.
    /// </summary>
    [Serializable]
    public class CriterionEvent : UnityEvent<Criterion>
    {
    }

    /// <summary>
    /// Base class for Criterion implementations.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public abstract class Criterion : ScriptableObject
    {
        /// <summary>
        /// Raised when any Criterion is completed.
        /// </summary>
        public static CriterionEvent CriterionCompleted = new();

        /// <summary>
        /// Raised when any Criterion is invalidated.
        /// </summary>
        public static CriterionEvent CriterionInvalidated = new();

        /// <summary>
        /// Raised when this criterion is completed.
        /// </summary>
        [Header("Events")]
        [Tooltip("Raised when this criterion is completed.")]
        public CriterionEvent Completed = new();

        /// <summary>
        /// Raised when this criterion is invalidated.
        /// </summary>
        [Tooltip("Raised when this criterion is invalidated.")]
        public CriterionEvent Invalidated = new();

        private bool m_Completed;

        /// <summary>
        /// Is the Criterion completed. Setting this raises CriterionCompleted/CriterionInvalidated.
        /// </summary>
        public bool IsCompleted
        {
            get => m_Completed;
            internal set
            {
                if (performedAtLeastOneEvaluationSinceTestingStarted
                && (value == m_Completed))
                {
                    return;
                }

                m_Completed = value;
                if (m_Completed)
                {
                    Completed?.Invoke(this);
                    CriterionCompleted?.Invoke(this);
                }
                else
                {
                    Invalidated?.Invoke(this);
                    CriterionInvalidated?.Invoke(this);
                }
                performedAtLeastOneEvaluationSinceTestingStarted = true;
            }
        }

        /// <summary>
        /// Resets the completion state.
        /// </summary>
        public void ResetCompletionState()
        {
            IsCompleted = false;
        }

        /// <summary>
        /// Starts testing of the criterion.
        /// </summary>
        public virtual void StartTesting()
        {
            isTesting = true;
            performedAtLeastOneEvaluationSinceTestingStarted = false;
        }

        /// <summary>
        /// Stops testing of the criterion.
        /// </summary>
        public virtual void StopTesting()
        {
            isTesting = false;
        }

        /// <summary>
        /// Is this criterion being tested right now?
        /// </summary>
        [SerializeField, HideInInspector]
        protected bool isTesting;

        /// <summary>
        /// Has at least one evaluation been performed since testing started?
        /// </summary>
        protected bool performedAtLeastOneEvaluationSinceTestingStarted;

        /// <summary>
        /// Runs update logic for the criterion.
        /// </summary>
        public virtual void UpdateCompletion()
        {
            if (!isTesting)
            {
                return;
            }
            IsCompleted = EvaluateCompletion();
        }

        /// <summary>
        /// Evaluates if the criterion is completed.
        /// </summary>
        /// <returns>True if the criterion is completed, false otherwise</returns>
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
        /// <returns>An IEnumerable of all future objects references for this Criterion</returns>
        protected virtual IEnumerable<FutureObjectReference> GetFutureObjectReferences()
        {
            return Enumerable.Empty<FutureObjectReference>();
        }

        /// <summary>
        /// Destroys unreferenced future references.
        /// </summary>
        /// <seealso href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnValidate.html"/>
        protected virtual void OnValidate()
        {
            // Find IDs of referenced future references
#if UNITY_6000_3_OR_NEWER
            HashSet<EntityId> referencedFutureReferenceIDs = new();
#else
            HashSet<int> referencedFutureReferenceIDs = new();
#endif
            
            
            foreach (FutureObjectReference futureReference in GetFutureObjectReferences())
                referencedFutureReferenceIDs.Add(IdUtils.GetIdFor(futureReference));

            // Destroy unreferenced future references
            string assetPath = AssetDatabase.GetAssetPath(this);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is FutureObjectReference
                    && ((FutureObjectReference)asset).Criterion == this
                    && !referencedFutureReferenceIDs.Contains(IdUtils.GetIdFor(asset)))
                {
                    DestroyImmediate(asset, true);
                }
            }
        }

        /// <summary>
        /// Creates a default FutureObjectReference for this Criterion.
        /// </summary>
        /// <returns>The new FutureObjectReference instance</returns>
        protected FutureObjectReference CreateFutureObjectReference()
        {
            return CreateFutureObjectReference("Future Reference");
        }

        /// <summary>
        /// Creates a FutureObjectReference by specific name for this Criterion.
        /// </summary>
        /// <param name="referenceName">The reference name to which the created FutureObjectReference point to</param>
        /// <returns>The new FutureObjectReference instance</returns>
        protected FutureObjectReference CreateFutureObjectReference(string referenceName)
        {
            FutureObjectReference futureReference = CreateInstance<FutureObjectReference>();
            futureReference.Criterion = this;
            futureReference.ReferenceName = referenceName;

            string assetPath = AssetDatabase.GetAssetPath(this);
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

        private void UpdateFutureObjectReferenceNamesPostponed()
        {
            // Unsubscribe immediately since it should only be called once
            EditorApplication.update -= UpdateFutureObjectReferenceNamesPostponed;

            string assetPath = AssetDatabase.GetAssetPath(this);
            TutorialPage tutorialPage = (TutorialPage)AssetDatabase.LoadMainAssetAtPath(assetPath);
            IEnumerable<FutureObjectReference> futureReferences = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .Where(o => o is FutureObjectReference)
                .Cast<FutureObjectReference>();
            foreach (FutureObjectReference futureReference in futureReferences)
                tutorialPage.UpdateFutureObjectReferenceName(futureReference);
        }
    }
}
