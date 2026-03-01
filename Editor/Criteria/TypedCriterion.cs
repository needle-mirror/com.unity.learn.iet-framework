using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Holder for SerializedType and Criterion.
    /// </summary>
    [Serializable]
    public class TypedCriterion
    {
        /// <summary>
        /// The Type.
        /// </summary>
        [SerializeField, FormerlySerializedAs("type")]
        [SerializedTypeFilter(typeof(Criterion), true)]
        public SerializedType Type;

        /// <summary>
        /// The Criterion.
        /// </summary>
        [SerializeField, FormerlySerializedAs("criterion")]
        public Criterion Criterion;

        /// <summary>
        /// Constructs with type and criterion.
        /// </summary>
        /// <param name="type">The SerializedType this criterion looks for</param>
        /// <param name="criterion">The Criterion this TypedCriterion uses</param>
        public TypedCriterion(SerializedType type, Criterion criterion)
        {
            Type = type;
            Criterion = criterion;
        }
    }

    /// <summary>
    /// A collection of <see cref="TypedCriterion"/>
    /// </summary>
    [Serializable]
    public class TypedCriterionCollection : CollectionWrapper<TypedCriterion>
    {
        /// <summary> Public constructor. </summary>
        public TypedCriterionCollection() { }

        /// <summary> Public constructor starting from a list of items. </summary>
        /// <param name="items">The items to add to the list.</param>
        public TypedCriterionCollection(IList<TypedCriterion> items) : base(items) { }
    }
}
