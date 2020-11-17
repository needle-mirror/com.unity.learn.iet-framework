using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// Criterion for physics collision events.
    /// </summary>
    public class TriggerTaskCriterion : Criterion
    {
        /// <summary>
        /// Different types of collision events.
        /// </summary>
        public enum TriggerTaskTestMode
        {
            /// <summary>
            /// Test for OnTriggerEnter events.
            /// </summary>
            TriggerEnter,
            /// <summary>
            /// Test for OnTriggerExit events.
            /// </summary>
            TriggerExit,
            /// <summary>
            /// Test for OnCollisionEnter events.
            /// </summary>
            CollisionEnter,
            /// <summary>
            /// Test for OnCollisionExit events.
            /// </summary>
            CollisionExit
        }

        [SerializeField]
        internal ObjectReference objectReference = new ObjectReference();

        /// <summary>
        /// For which type of event should we test for.
        /// </summary>
        [FormerlySerializedAs("testMode")]
        public TriggerTaskTestMode TestMode = TriggerTaskTestMode.TriggerEnter;

        /// <summary>
        /// Starts testing of the criterion.
        /// </summary>
        public override void StartTesting()
        {
            CollisionBroadcaster2D.PlayerEnteredCollision += OnPlayerEnteredCollision2D;
            CollisionBroadcaster2D.PlayerEnteredTrigger += OnPlayerEnteredTrigger2D;
            CollisionBroadcaster2D.PlayerExitedCollision += OnPlayerExitCollision2D;
            CollisionBroadcaster2D.PlayerExitedTrigger += OnPlayerExitTrigger2D;

            CollisionBroadcaster3D.PlayerEnteredCollision += OnPlayerEnteredCollision;
            CollisionBroadcaster3D.PlayerEnteredTrigger += OnPlayerEnteredTrigger;
            CollisionBroadcaster3D.PlayerExitedCollision += OnPlayerExitCollision;
            CollisionBroadcaster3D.PlayerExitedTrigger += OnPlayerExitTrigger;
        }

        /// <summary>
        /// Stops testing of the criterion.
        /// </summary>
        public override void StopTesting()
        {
            base.StopTesting();
            CollisionBroadcaster2D.PlayerEnteredCollision -= OnPlayerEnteredCollision2D;
            CollisionBroadcaster2D.PlayerEnteredTrigger -= OnPlayerEnteredTrigger2D;
            CollisionBroadcaster2D.PlayerExitedCollision -= OnPlayerExitCollision2D;
            CollisionBroadcaster2D.PlayerExitedTrigger -= OnPlayerExitTrigger2D;

            CollisionBroadcaster3D.PlayerEnteredCollision -= OnPlayerEnteredCollision;
            CollisionBroadcaster3D.PlayerEnteredTrigger -= OnPlayerEnteredTrigger;
            CollisionBroadcaster3D.PlayerExitedCollision -= OnPlayerExitCollision;
            CollisionBroadcaster3D.PlayerExitedTrigger -= OnPlayerExitTrigger;
        }

        /// <summary>
        /// Evaluates if the criterion is completed.
        /// </summary>
        /// <remarks>
        /// Overriding the update completion state, as this criterion is not state based
        /// </remarks>
        public override void UpdateCompletion()
        {
        }

        GameObject ReferencedGameObject => objectReference.SceneObjectReference.ReferencedObjectAsGameObject;

        void OnPlayerEnteredCollision2D(CollisionBroadcaster2D sender)
        {
            if (TestMode == TriggerTaskTestMode.CollisionEnter && ReferencedGameObject == sender.gameObject)
                Completed = true;
        }

        void OnPlayerEnteredTrigger2D(CollisionBroadcaster2D sender)
        {
            if (TestMode == TriggerTaskTestMode.TriggerEnter && ReferencedGameObject == sender.gameObject)
                Completed = true;
        }

        void OnPlayerExitCollision2D(CollisionBroadcaster2D sender)
        {
            if (TestMode == TriggerTaskTestMode.CollisionExit && ReferencedGameObject == sender.gameObject)
                Completed = true;
        }

        void OnPlayerExitTrigger2D(CollisionBroadcaster2D sender)
        {
            if (TestMode == TriggerTaskTestMode.TriggerExit && ReferencedGameObject == sender.gameObject)
                Completed = true;
        }

        void OnPlayerEnteredCollision(CollisionBroadcaster3D sender)
        {
            if (TestMode == TriggerTaskTestMode.CollisionEnter && ReferencedGameObject == sender.gameObject)
                Completed = true;
        }

        void OnPlayerEnteredTrigger(CollisionBroadcaster3D sender)
        {
            if (TestMode == TriggerTaskTestMode.TriggerEnter && ReferencedGameObject == sender.gameObject)
                Completed = true;
        }

        void OnPlayerExitCollision(CollisionBroadcaster3D sender)
        {
            if (TestMode == TriggerTaskTestMode.CollisionExit && ReferencedGameObject == sender.gameObject)
                Completed = true;
        }

        void OnPlayerExitTrigger(CollisionBroadcaster3D sender)
        {
            if (TestMode == TriggerTaskTestMode.TriggerExit && ReferencedGameObject == sender.gameObject)
                Completed = true;
        }

        /// <summary>
        /// Auto-completes the criterion.
        /// </summary>
        /// <returns>True if the auto-completion succeeded.</returns>
        public override bool AutoComplete()
        {
            if (ReferencedGameObject == null)
                return false;

            if (ReferencedGameObject.GetComponent<BaseCollisionBroadcaster>() == null)
                return false;

            var playerComponent = SceneManager.GetActiveScene().GetRootGameObjects()
                .Select(gameObject => gameObject.GetComponentInChildren<IPlayerAvatar>())
                .Cast<Component>()
                .FirstOrDefault(component => component != null);

            if (playerComponent == null)
                return false;

            var playerGameObject = HandleUtilityProxy.FindSelectionBase(playerComponent.gameObject);
            if (playerGameObject == null)
                playerGameObject = playerComponent.gameObject;

            switch (TestMode)
            {
                case TriggerTaskTestMode.TriggerEnter:
                case TriggerTaskTestMode.CollisionEnter:
                    playerGameObject.transform.position = ReferencedGameObject.transform.position;
                    return true;

                case TriggerTaskTestMode.TriggerExit:
                case TriggerTaskTestMode.CollisionExit:
                default:
                    return false;
            }
        }
    }
}
