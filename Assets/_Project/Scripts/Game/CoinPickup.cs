using UnityEngine;

namespace _Project.Scripts.Game
{
    public class CoinPickup : MonoBehaviour
    {
        public float rotateSpeed = 120f;

        private void Update()
        {
            transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (CoinManager.Instance != null)
                CoinManager.Instance.CollectOne();

            Destroy(gameObject);
        }
    }
}
