using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityObject = UnityEngine.Object;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Criterion for checking that a specific Prefab is instantiated.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class InstantiatePrefabCriterion : Criterion
    {
        [SerializeField] private GameObject m_PrefabParent;

        [SerializeField] private FuturePrefabInstanceCollection m_FuturePrefabInstances = new();

#if UNITY_6000_3_OR_NEWER
        // EntityIDs of existing GameObject Prefab instance roots we want to ignore
        private HashSet<EntityId> m_ExistingPrefabInstances = new();

        // EntityID of GameObject Prefab instance root that initially completed this criterion
        private EntityId m_PrefabInstance;
#else
        // InstanceIDs of existing GameObject Prefab instance roots we want to ignore
        private HashSet<int> m_ExistingPrefabInstances = new();

        // InstanceID of GameObject Prefab instance root that initially completed this criterion
        private int m_PrefabInstance;
#endif

        /// <summary>
        /// Prefab parent.
        /// </summary>
        public GameObject PrefabParent
        {
            get => m_PrefabParent;
            set
            {
                m_PrefabParent = value;
                OnValidate();
            }
        }

        /// <summary>
        /// Sets future Prefab instances.
        /// </summary>
        /// <param name="prefabParents">A list of Object the Prefab will be child of</param>
        public void SetFuturePrefabInstances(IList<UnityObject> prefabParents)
        {
            IEnumerable<FuturePrefabInstance> futurePrefabInstances = prefabParents.Select(prefabParent => new FuturePrefabInstance(prefabParent));
            m_FuturePrefabInstances.SetItems(futurePrefabInstances.ToList());
            OnValidate();
        }

        /// <summary>
        /// Runs validation logic.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();

            if (m_PrefabParent == null)
                return;

            // Ensure Prefab parent is in fact a Prefab parent
            if (PrefabUtility.GetPrefabAssetType(m_PrefabParent) != PrefabAssetType.NotAPrefab)
            {
                // Ensure Prefab parent is the Prefab root
                GameObject prefabRoot = m_PrefabParent.transform.root.gameObject;
                if (m_PrefabParent != prefabRoot)
                    m_PrefabParent = prefabRoot;
            }
            else
            {
                Debug.LogWarning("Prefab parent must either be a Prefab parent or a Prefab instance.");
                m_PrefabParent = null;
            }

            // Prevent aliasing of future reference whenever the last item is copied
            int count = m_FuturePrefabInstances.Count;
            if (count >= 2)
            {
                FuturePrefabInstance last = m_FuturePrefabInstances[count - 1];
                FuturePrefabInstance secondLast = m_FuturePrefabInstances[count - 2];
                if (last.FutureReference == secondLast.FutureReference)
                    last.FutureReference = null;
            }

            bool updateFutureReferenceNames = false;
            int futurePrefabInstanceIndex = -1;

            foreach (FuturePrefabInstance futurePrefabInstance in m_FuturePrefabInstances)
            {
                futurePrefabInstanceIndex++;

                // Destroy future reference if Prefab parent is null or it changed
                UnityObject prefabParent = futurePrefabInstance.PrefabParent;
                UnityObject previousPrefabParent = futurePrefabInstance.PreviousPrefabParent;
                futurePrefabInstance.PreviousPrefabParent = prefabParent;
                if (prefabParent == null || (previousPrefabParent != null && prefabParent != previousPrefabParent))
                {
                    if (futurePrefabInstance.FutureReference != null)
                    {
                        DestroyImmediate(futurePrefabInstance.FutureReference, true);
                        futurePrefabInstance.FutureReference = null;
                    }
                }

                if (prefabParent == null)
                    continue;

                // Ensure future Prefab parent is in fact a Prefab parent
                if (PrefabUtility.GetPrefabAssetType(prefabParent) != PrefabAssetType.NotAPrefab)
                {
                    // Find root game object of future Prefab parent
                    GameObject futurePrefabParentRoot = null;
                    if (prefabParent is GameObject)
                    {
                        GameObject gameObject = (GameObject)prefabParent;
                        futurePrefabParentRoot = gameObject.transform.root.gameObject;
                    }
                    else if (prefabParent is Component)
                    {
                        Component component = (Component)prefabParent;
                        futurePrefabParentRoot = component.transform.root.gameObject;
                    }

                    // Ensure Prefab parent and future Prefab parent belong to the same Prefab
                    if (futurePrefabParentRoot == m_PrefabParent)
                    {
                        // Create new future reference if it doesn't exist yet
                        if (futurePrefabInstance.FutureReference == null)
                        {
                            string referenceName = string.Format("{0}: {1} ({2})", futurePrefabInstanceIndex + 1,
                                prefabParent.name, prefabParent.GetType().Name);
                            futurePrefabInstance.FutureReference = CreateFutureObjectReference(referenceName);
                            updateFutureReferenceNames = true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Prefab parent and future Prefab parent have different Prefab objects.");
                        futurePrefabInstance.PrefabParent = null;
                    }
                }
                else
                {
                    Debug.LogWarning("Future Prefab parent must be either a Prefab parent or a Prefab instance.");
                    futurePrefabInstance.PrefabParent = null;
                }
            }

            if (updateFutureReferenceNames)
                UpdateFutureObjectReferenceNames();
        }

        /// <summary>
        /// Starts testing of the criterion.
        /// </summary>
        public override void StartTesting()
        {
            base.StartTesting();
            // Record existing Prefab instances
            m_ExistingPrefabInstances.Clear();
            foreach (GameObject gameObject in FindObjectUtils.FindObjectsByType<GameObject>())
            {
                if (PrefabUtilityShim.GetCorrespondingObjectFromSource(gameObject) != null)
                {
                    GameObject prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                    m_ExistingPrefabInstances.Add(IdUtils.GetIdFor(prefabInstanceRoot));
                }
            }

            Selection.selectionChanged += OnSelectionChanged;

            if (IsCompleted)
                EditorApplication.update += OnUpdateWhenCompleted;

            UpdateCompletion();
        }

        /// <summary>
        /// Stops testing of the criterion.
        /// </summary>
        public override void StopTesting()
        {
            base.StopTesting();
            m_ExistingPrefabInstances.Clear();

            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.update -= OnUpdateWhenCompleted;
        }

        private void OnSelectionChanged()
        {
            if (IsCompleted)
                return;

            foreach (GameObject gameObject in Selection.gameObjects)
            {
                if (PrefabUtilityShim.GetCorrespondingObjectFromSource(gameObject) != null)
                {
                    GameObject prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                    bool added = m_ExistingPrefabInstances.Add(IdUtils.GetIdFor(prefabInstanceRoot));
                    
                    if (prefabInstanceRoot == gameObject && added)
                        OnPrefabInstantiated(prefabInstanceRoot);
                }
            }
        }

        private void OnPrefabInstantiated(GameObject prefabInstanceRoot)
        {
            if (m_PrefabParent == null)
                return;

            if (PrefabUtilityShim.GetCorrespondingObjectFromSource(prefabInstanceRoot) == m_PrefabParent)
            {
                foreach (Component component in prefabInstanceRoot.GetComponentsInChildren<Component>())
                {
                    UpdateFutureReferences(component);

                    if (component is Transform)
                        UpdateFutureReferences(component.gameObject);
                }

                m_PrefabInstance = IdUtils.GetIdFor(prefabInstanceRoot);
                
                UpdateCompletion();
            }
        }

        private void OnUpdateWhenCompleted()
        {
            if (!IsCompleted)
            {
                EditorApplication.update -= OnUpdateWhenCompleted;
                return;
            }

            UpdateCompletion();
        }

        private bool EvaluateCompletionInternal()
        {
            if (IdUtils.IsIdNull(m_PrefabInstance)) return false;

            UnityObject prefabObject = IdUtils.IdToObject(m_PrefabInstance);

            if (prefabObject != null) return true;
            
            m_ExistingPrefabInstances.Remove(m_PrefabInstance);
            m_PrefabInstance = IdUtils.NullId;

            return false;

        }

        /// <summary>
        /// Evaluates if the criterion is completed.
        /// </summary>
        /// <returns>True when completed</returns>
        protected override bool EvaluateCompletion()
        {
            bool willBeCompleted = EvaluateCompletionInternal();
            if (!IsCompleted && willBeCompleted)
                EditorApplication.update += OnUpdateWhenCompleted;

            return willBeCompleted;
        }

        private void UpdateFutureReferences(UnityObject prefabInstance)
        {
            UnityObject prefabParent = PrefabUtilityShim.GetCorrespondingObjectFromSource(prefabInstance);
            foreach (FuturePrefabInstance futurePrefabInstance in m_FuturePrefabInstances)
            {
                if (futurePrefabInstance.PrefabParent == prefabParent)
                    futurePrefabInstance.FutureReference.SceneObjectReference.Update(prefabInstance);
            }
        }

        /// <summary>
        /// Returns FutureObjectReference for this Criterion.
        /// </summary>
        /// <returns>An IEnumerable of all the FutureObjectReference that Criterion depends on</returns>
        protected override IEnumerable<FutureObjectReference> GetFutureObjectReferences()
        {
            return m_FuturePrefabInstances
                .Select(futurePrefabInstance => futurePrefabInstance.FutureReference)
                .Where(futurePrefabInstance => futurePrefabInstance != null);
        }

        /// <summary>
        /// Auto-completes the criterion.
        /// </summary>
        /// <returns>True if the auto-completion succeeded.</returns>
        public override bool AutoComplete()
        {
            if (m_PrefabParent == null)
                return false;

            Selection.activeObject = PrefabUtility.InstantiatePrefab(m_PrefabParent);

            return true;
        }

        /// <summary>
        /// Future Prefab instance.
        /// </summary>
        [Serializable]
        public class FuturePrefabInstance
        {
            [SerializeField] private UnityObject m_PrefabParent;

            private UnityObject m_PreviousPrefabParent;

            [SerializeField, HideInInspector] private FutureObjectReference m_FutureReference;

            /// <summary>
            /// Prefab parent.
            /// </summary>
            public UnityObject PrefabParent { get => m_PrefabParent; set => m_PrefabParent = value; }

            /// <summary>
            /// Previous Prefab parent.
            /// </summary>
            public UnityObject PreviousPrefabParent { get => m_PreviousPrefabParent; set => m_PreviousPrefabParent = value; }

            /// <summary>
            /// Future reference.
            /// </summary>
            public FutureObjectReference FutureReference { get => m_FutureReference; set => m_FutureReference = value; }

            /// <summary>
            /// Constructs with a specific Prefab parent.
            /// </summary>
            /// <param name="prefabParent">The parent Object of this FuturePrefabInstance</param>
            public FuturePrefabInstance(UnityObject prefabParent)
            {
                m_PrefabParent = prefabParent;
            }
        }

        [Serializable]
        private class FuturePrefabInstanceCollection : CollectionWrapper<FuturePrefabInstance>
        {
        }
    }
}
