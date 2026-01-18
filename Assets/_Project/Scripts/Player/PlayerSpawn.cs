using UnityEngine;

namespace _Project.Scripts
{
    public class PlayerSpawn : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private Transform startMarker;

        private CharacterController _characterController;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private void Start()
        {
            Spawn();
        }

        private void Spawn()
        {
            if (startMarker == null)
            {
                Debug.LogError("StartMarker není přiřazen!", this);
                return;
            }

            _characterController.enabled = false;
            transform.position = startMarker.position;
            transform.rotation = startMarker.rotation;
            _characterController.enabled = true;
        }
    }
}
