using System;
using UnityEngine;

namespace Unity.Tutorials.Core
{
    /// <summary>
    /// Broadcasts 3D collision events for IPlayerAvatar.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CollisionBroadcaster3D : BaseCollisionBroadcaster
    {
        /// <summary>
        /// Raised upon OnCollisionEnter3D.
        /// </summary>
        public static event Action<CollisionBroadcaster3D> PlayerEnteredCollision;

        /// <summary>
        /// Raised upon OnCollisionExit3D.
        /// </summary>
        public static event Action<CollisionBroadcaster3D> PlayerExitedCollision;

        /// <summary>
        /// Raised upon OnTriggerEnter3D.
        /// </summary>
        public static event Action<CollisionBroadcaster3D> PlayerEnteredTrigger;

        /// <summary>
        /// Raised upon OnTriggerExit3D.
        /// </summary>
        public static event Action<CollisionBroadcaster3D> PlayerExitedTrigger;

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.GetComponentInChildren<IPlayerAvatar>() != null)
            {
                PlayerEnteredCollision?.Invoke(this);
            }
        }

        void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.GetComponentInChildren<IPlayerAvatar>() != null)
            {
                PlayerExitedCollision?.Invoke(this);
            }
        }

        void OnTriggerEnter(Collider collider)
        {
            if (collider.GetComponent<IPlayerAvatar>() != null)
            {
                PlayerEnteredTrigger?.Invoke(this);
            }
        }

        void OnTriggerExit(Collider collider)
        {
            if (collider.GetComponentInChildren<IPlayerAvatar>() != null)
            {
                PlayerExitedTrigger?.Invoke(this);
            }
        }
    }
}
