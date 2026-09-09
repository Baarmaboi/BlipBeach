using UnityEngine;

/// <summary>
/// Teleports the player to the morning spawn point at the start of each day.
/// Put this on GameSystems, or on the empty PlayerDaySpawn marker in the scene.
/// </summary>
public class PlayerDaySpawn : MonoBehaviour
{
    public static PlayerDaySpawn Instance { get; private set; }

    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Rigidbody playerRigidbody;

    private static readonly string[] SpawnMarkerNames = { "PlayerDaySpawn", "Player_DaySpawn" };

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void TeleportToSpawn()
    {
        ResolveReferences();

        if (spawnPoint == null)
        {
            Debug.LogWarning("PlayerDaySpawn: no spawn marker found. Create an empty named PlayerDaySpawn in the scene.");
            return;
        }

        if (playerMovement == null)
        {
            Debug.LogWarning("PlayerDaySpawn: no PlayerMovement found in the scene.");
            return;
        }

        Transform target = playerMovement.transform;
        target.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        if (playerRigidbody != null)
        {
            playerRigidbody.position = spawnPoint.position;
            playerRigidbody.rotation = spawnPoint.rotation;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        Physics.SyncTransforms();
    }

    private void ResolveReferences()
    {
        if (spawnPoint == null)
        {
            spawnPoint = FindSpawnMarker();
        }

        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (playerRigidbody == null && playerMovement != null)
        {
            playerRigidbody = playerMovement.GetComponent<Rigidbody>();
        }
    }

    private Transform FindSpawnMarker()
    {
        // Component on the marker itself — use this object's transform.
        for (int i = 0; i < SpawnMarkerNames.Length; i++)
        {
            if (gameObject.name == SpawnMarkerNames[i])
            {
                return transform;
            }
        }

        // Otherwise look for a named empty in the scene.
        for (int i = 0; i < SpawnMarkerNames.Length; i++)
        {
            GameObject marker = GameObject.Find(SpawnMarkerNames[i]);
            if (marker != null)
            {
                return marker.transform;
            }
        }

        return null;
    }
}
