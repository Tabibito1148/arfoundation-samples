namespace UnityEngine.XR.ARFoundation.Samples
{
    /// <summary>
    /// Subscribes to an <see cref="ARRaycastHitEventAsset"/>. When the event is raised, the
    /// <see cref="prefabToPlace"/> is instantiated at, or moved to, the hit position.
    /// </summary>
    public class ARPlaceObject : MonoBehaviour
    {
        private const float k_PrefabHalfSize = 0.025f;

        [SerializeField]
        [Tooltip("The prefab to be placed or moved.")]
        private GameObject m_PrefabToPlace;

        [SerializeField]
        [Tooltip("The Scriptable Object Asset that contains the ARRaycastHit event.")]
        private ARRaycastHitEventAsset m_RaycastHitEvent;

        private GameObject m_SpawnedObject;

        /// <summary>
        /// The prefab to be placed or moved.
        /// </summary>
        public GameObject prefabToPlace
        {
            get => m_PrefabToPlace;
            set => m_PrefabToPlace = value;
        }

        /// <summary>
        /// The spawned prefab instance.
        /// </summary>
        public GameObject spawnedObject
        {
            get => m_SpawnedObject;
            set => m_SpawnedObject = value;
        }

        private void OnEnable()
        {
            if (m_RaycastHitEvent == null || m_PrefabToPlace == null)
            {
                Debug.LogWarning($"{nameof(ARPlaceObject)} component on {name} has null inputs and will have no effect in this scene.", this);
                return;
            }

            if (m_RaycastHitEvent != null)
                m_RaycastHitEvent.eventRaised += PlaceObjectAt;
        }

        private void OnDisable()
        {
            if (m_RaycastHitEvent != null)
                m_RaycastHitEvent.eventRaised -= PlaceObjectAt;
        }

        private void PlaceObjectAt(object sender, ARRaycastHit hitPose)
        {
            if (m_SpawnedObject == null)
            {
                m_SpawnedObject = Instantiate(m_PrefabToPlace, hitPose.trackable.transform.parent);
            }

            var forward = hitPose.pose.rotation * Vector3.up;
            var offset = forward * k_PrefabHalfSize;
            m_SpawnedObject.transform.position = hitPose.pose.position + offset;
            m_SpawnedObject.transform.parent = hitPose.trackable.transform.parent;
        }
    }
}