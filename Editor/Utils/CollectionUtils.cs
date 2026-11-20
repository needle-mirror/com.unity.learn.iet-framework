using System;
using System.Collections.Generic;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Helpers for working with collections.
    /// </summary>
    internal static class CollectionUtils
    {
        /// <summary>
        /// Sorts a list in place, preserving the relative order of elements that compare equal.
        /// </summary>
        /// <remarks>
        /// List&lt;T&gt;.Sort and Array.Sort are unstable, so they cannot be used where equal elements
        /// must keep their original order (for example when authors leave several assets at the same
        /// sort index). An insertion sort is used because the collections sorted here are small.
        /// </remarks>
        /// <typeparam name="T">Type of the elements to sort.</typeparam>
        /// <param name="list">The list to sort in place.</param>
        /// <param name="comparison">Comparison used to order the elements.</param>
        public static void StableSort<T>(List<T> list, Comparison<T> comparison)
        {
            for (int i = 1; i < list.Count; i++)
            {
                T current = list[i];
                int j = i - 1;
                while (j >= 0 && comparison(list[j], current) > 0)
                {
                    list[j + 1] = list[j];
                    j--;
                }

                list[j + 1] = current;
            }
        }
    }
}
