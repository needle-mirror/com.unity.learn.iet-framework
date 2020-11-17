using System;
using UnityEngine;

namespace Unity.Tutorials.Core
{
    /// <summary>
    /// Broadcasts 2D collision events for IPlayerAvatar.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CollisionBroadcaster2D : BaseCollisionBroadcaster
    {
        /// <summary>
        /// Raised upon OnCollisionEnter2D.
        /// </summary>
        public static event Action<CollisionBroadcaster2D> PlayerEnteredCollision;

        /// <summary>
        /// Raised upon OnCollisionExit2D.
        /// </summary>
        public static event Action<CollisionBroadcaster2D> PlayerExitedCollision;

        /// <summary>
        /// Raised upon OnTriggerEnter2D.
        /// </summary>
        public static event Action<CollisionBroadcaster2D> PlayerEnteredTrigger;

        /// <summary>
        /// Raised upon OnTriggerExit2D.
        /// </summary>
        public static event Action<CollisionBroadcaster2D> PlayerExitedTrigger;

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.GetComponentInChildren<IPlayerAvatar>() != null)
            {
                PlayerEnteredCollision?.Invoke(this);
            }
        }

        void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.GetComponentInChildren<IPlayerAvatar>() != null)
            {
                PlayerExitedCollision?.Invoke(this);
            }
        }

        void OnTriggerEnter2D(Collider2D collider)
        {
            if (collider.GetComponentInChildren<IPlayerAvatar>() != null)
            {
                PlayerEnteredTrigger?.Invoke(this);
            }
        }

        void OnTriggerExit2D(Collider2D collider)
        {
            if (collider.GetComponentInChildren<IPlayerAvatar>() != null)
            {
                PlayerExitedTrigger?.Invoke(this);
            }
        }
    }
}
