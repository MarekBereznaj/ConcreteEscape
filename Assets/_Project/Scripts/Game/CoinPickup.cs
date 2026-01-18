using _Project.Scripts.Game;
using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    public float rotateSpeed = 120f;

    [Header("Sound")]
    public AudioClip pickupClip;
    [Range(0f, 1f)] public float volume = 1f;

    private Transform visual;

    private void Awake()
    {
        var r = GetComponentInChildren<Renderer>(true);
        visual = r != null ? r.transform : transform;
    }

    private void Update()
    {
        if (visual != null)
            visual.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;


        if (pickupClip != null)
            AudioSource.PlayClipAtPoint(pickupClip, transform.position, volume);

        CoinManager.Instance?.CollectOne();


        GameObject coinRoot = FindCoinInstanceRoot();
        Destroy(coinRoot);
    }

    private GameObject FindCoinInstanceRoot()
    {
        Transform t = transform;

        while (t != null)
        {
            if (t.name.StartsWith("Coin_"))
                return t.gameObject;

            t = t.parent;
        }

        return gameObject;
    }
}