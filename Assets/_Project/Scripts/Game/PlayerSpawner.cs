using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Tooltip("Sem přetáhni objekt hráče (nebo nech prázdné a spawne se Player tag).")]
    public Transform player;

    [Tooltip("StartPoint objekt (generuje MazeGenerator). Pokud je null, hledá podle jména StartPoint.")]
    public Transform startPoint;

    [Tooltip("Pokud je player null, najde objekt s tagem Player.")]
    public string playerTag = "Player";

    private void Start()
    {
        // najdi hráče
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        // najdi start
        if (startPoint == null)
        {
            var sp = GameObject.Find("StartPoint");
            if (sp != null) startPoint = sp.transform;
        }

        if (player == null)
        {
            Debug.LogError("PlayerSpawner: Nenalezen hráč. Nastav reference nebo tag Player.");
            return;
        }

        if (startPoint == null)
        {
            Debug.LogError("PlayerSpawner: Nenalezen StartPoint. Nejdřív vygeneruj maze.");
            return;
        }

        // teleport hráče na start
        MovePlayerToStart(player, startPoint.position);
    }

private void MovePlayerToStart(Transform playerTr, Vector3 targetPos)
{
    // 1) CharacterController (typicky FPS) – musí se dočasně vypnout
    var cc = playerTr.GetComponent<CharacterController>();
    if (cc != null)
    {
        cc.enabled = false;
        playerTr.position = targetPos;
        cc.enabled = true;
        return;
    }

    // 2) Rigidbody – nastav pozici a vynuluj rychlosti
    var rb = playerTr.GetComponent<Rigidbody>();
    if (rb != null)
    {
        rb.position = targetPos;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        return;
    }

    // 3) fallback
    playerTr.position = targetPos;
}

}
