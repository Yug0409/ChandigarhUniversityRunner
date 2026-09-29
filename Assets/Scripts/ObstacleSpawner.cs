using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [System.Serializable]
    private struct SpawnRow
    {
        [SerializeField] private Transform leftSpawnPoint;
        [SerializeField] private Transform centerSpawnPoint;
        [SerializeField] private Transform rightSpawnPoint;

        public Transform GetSpawnPoint(int laneIndex)
        {
            switch (laneIndex)
            {
                case 0:
                    return leftSpawnPoint;

                case 1:
                    return centerSpawnPoint;

                case 2:
                    return rightSpawnPoint;

                default:
                    return null;
            }
        }
    }

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Obstacle Prefabs")]
    [SerializeField] private GameObject[] obstaclePrefabs;

    [Header("Coins")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private float coinHeight = 0.9f;
    [SerializeField] private float coinCollectionDistance = 1.25f;

    [Header("Spawn Rows")]
    [Tooltip("Each row contains one spawn point for the left, center, and right lanes.")]
    [SerializeField] private SpawnRow[] obstacleRows = new SpawnRow[6];

    [Header("Obstacle Start")]
    [Tooltip("The first rows stay empty.")]
    [SerializeField] private int firstObstacleSpawnPoint = 3;

    [Header("Obstacle Probability")]
    [Range(0f, 1f)]
    [SerializeField] private float obstacleChance = 0.5f;

    [Header("Obstacles Per Row")]
    [Tooltip("Minimum number of obstacles when a row is selected.")]
    [SerializeField] private int minimumObstaclesPerRow = 1;

    [Tooltip("Maximum is limited to 2 so one lane is always open.")]
    [SerializeField] private int maximumObstaclesPerRow = 2;

    [Header("Spawn Position")]
    [SerializeField] private float verticalSpawnOffset = 0f;

    [SerializeField] private float forwardSpawnOffset = 0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool obstaclesSpawned;

    private readonly List<GameObject> spawnedObstacles =
        new List<GameObject>();

    private readonly List<GameObject> spawnedCoins =
        new List<GameObject>();

    private Material generatedCoinMaterial;

    private void Awake()
    {
        maximumObstaclesPerRow =
            Mathf.Clamp(maximumObstaclesPerRow, 1, 2);

        minimumObstaclesPerRow =
            Mathf.Clamp(
                minimumObstaclesPerRow,
                1,
                maximumObstaclesPerRow
            );

        obstacleChance =
            Mathf.Clamp01(obstacleChance);

        firstObstacleSpawnPoint =
            Mathf.Max(0, firstObstacleSpawnPoint);

        ValidateSetup();
    }

    public void SetSpawning(bool value)
    {
        /*
         * For this game version, "starting spawning"
         * means generating the whole obstacle layout once.
         */
        if (value)
        {
            if (obstaclesSpawned)
                return;

            SpawnAllObstacles();
            obstaclesSpawned = true;
        }
        else
        {
            // We don't destroy obstacles when spawning is disabled.
            // This simply prevents another spawn call.
        }
    }

    public bool HasPassedAllRows(
        Vector3 playerPosition,
        float distancePastLastRow)
    {
        if (obstacleRows == null || obstacleRows.Length == 0)
            return false;

        Transform finishPoint = null;
        float furthestDistance = float.NegativeInfinity;

        for (int rowIndex = 0; rowIndex < obstacleRows.Length; rowIndex++)
        {
            for (int laneIndex = 0; laneIndex < 3; laneIndex++)
            {
                Transform spawnPoint =
                    obstacleRows[rowIndex].GetSpawnPoint(laneIndex);

                if (spawnPoint == null)
                    continue;

                float distanceAlongCourse =
                    Vector3.Dot(spawnPoint.position, Vector3.back);

                if (distanceAlongCourse > furthestDistance)
                {
                    furthestDistance = distanceAlongCourse;
                    finishPoint = spawnPoint;
                }
            }
        }

        if (finishPoint == null)
            return false;

        float distancePastFinish =
            Vector3.Dot(
                playerPosition - finishPoint.position,
                Vector3.back
            );

        return distancePastFinish >= Mathf.Max(0f, distancePastLastRow);
    }

    public void SpawnAllObstacles()
    {
        if (obstaclePrefabs == null ||
            obstaclePrefabs.Length == 0)
        {
            Debug.LogError(
                "ObstacleSpawner: No obstacle prefabs assigned."
            );

            return;
        }

        if (obstacleRows == null ||
            obstacleRows.Length == 0)
        {
            Debug.LogError(
                "ObstacleSpawner: No spawn rows assigned."
            );

            return;
        }

        /*
         * Clear previously spawned obstacles first.
         * This is useful if you restart the game.
         */
        ClearSpawnedObstacles();

        int startIndex =
            Mathf.Clamp(
                firstObstacleSpawnPoint,
                0,
                obstacleRows.Length
            );

        for (int i = 0; i < obstacleRows.Length; i++)
        {
            if (i < startIndex)
            {
                SpawnCoinInRandomLane(obstacleRows[i]);

                if (showDebugLogs)
                {
                    Debug.Log(
                        $"Spawn Row {i}: SAFE - starting safe rows."
                    );
                }

                continue;
            }

            /*
             * 50% obstacle / 50% empty.
             */
            float randomRoll =
                Random.value;

            if (randomRoll > obstacleChance)
            {
                SpawnCoinInRandomLane(obstacleRows[i]);

                if (showDebugLogs)
                {
                    Debug.Log(
                        $"Spawn Row {i}: EMPTY"
                    );
                }

                continue;
            }

            if (showDebugLogs)
            {
                Debug.Log(
                    $"Spawn Row {i}: OBSTACLE ROW"
                );
            }

            int openLane = SpawnObstacleRow(obstacleRows[i], i);

            if (openLane >= 0)
            {
                SpawnCoin(
                    obstacleRows[i].GetSpawnPoint(openLane)
                );
            }
        }
    }

    private int SpawnObstacleRow(
        SpawnRow row,
        int rowIndex)
    {
        List<int> availableLanes = new List<int>();

        for (int laneIndex = 0; laneIndex < 3; laneIndex++)
        {
            if (row.GetSpawnPoint(laneIndex) != null)
            {
                availableLanes.Add(laneIndex);
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning(
                    $"ObstacleSpawner: Row {rowIndex} is missing its " +
                    $"{GetLaneName(laneIndex)} spawn point."
                );
            }
        }

        if (availableLanes.Count == 0)
            return -1;

        int maximumCount =
            Mathf.Min(
                maximumObstaclesPerRow,
                availableLanes.Count
            );

        int minimumCount =
            Mathf.Min(
                minimumObstaclesPerRow,
                maximumCount
            );

        int obstacleCount =
            Random.Range(
                minimumCount,
                maximumCount + 1
            );

        for (int i = 0;
             i < obstacleCount;
             i++)
        {
            /*
             * Select a random unused lane.
             */
            int randomIndex =
                Random.Range(
                    0,
                    availableLanes.Count
                );

            int selectedLane =
                availableLanes[randomIndex];

            Transform spawnPoint =
                row.GetSpawnPoint(selectedLane);

            availableLanes.RemoveAt(
                randomIndex
            );

            SpawnObstacle(
                spawnPoint,
                selectedLane,
                rowIndex
            );
        }

        return availableLanes.Count > 0
            ? availableLanes[0]
            : -1;
    }

    private void SpawnCoinInRandomLane(SpawnRow row)
    {
        List<Transform> availablePoints = new List<Transform>();

        for (int laneIndex = 0; laneIndex < 3; laneIndex++)
        {
            Transform spawnPoint = row.GetSpawnPoint(laneIndex);

            if (spawnPoint != null)
            {
                availablePoints.Add(spawnPoint);
            }
        }

        if (availablePoints.Count == 0)
            return;

        SpawnCoin(
            availablePoints[Random.Range(0, availablePoints.Count)]
        );
    }

    private void SpawnCoin(Transform spawnPoint)
    {
        if (spawnPoint == null)
            return;

        Vector3 coinPosition =
            spawnPoint.position + Vector3.up * coinHeight;

        GameObject coin;

        if (coinPrefab != null)
        {
            coin = Instantiate(
                coinPrefab,
                coinPosition,
                spawnPoint.rotation
            );
        }
        else
        {
            coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.transform.SetPositionAndRotation(
                coinPosition,
                spawnPoint.rotation * Quaternion.Euler(90f, 0f, 0f)
            );
            coin.transform.localScale = new Vector3(0.65f, 0.1f, 0.65f);

            Collider coinCollider = coin.GetComponent<Collider>();

            if (coinCollider != null)
            {
                Destroy(coinCollider);
            }

            Renderer coinRenderer = coin.GetComponent<Renderer>();
            Material coinMaterial = GetCoinMaterial();

            if (coinRenderer != null && coinMaterial != null)
            {
                coinRenderer.sharedMaterial = coinMaterial;
            }
        }

        coin.name = "RunnerCoin";
        spawnedCoins.Add(coin);
    }

    private Material GetCoinMaterial()
    {
        if (generatedCoinMaterial != null)
            return generatedCoinMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader != null)
        {
            generatedCoinMaterial = new Material(shader);
            generatedCoinMaterial.color = new Color(1f, 0.72f, 0.08f);
        }

        return generatedCoinMaterial;
    }

    public bool HasObstacleCollision(Vector3 playerPosition)
    {
        Vector3 playerCollisionPoint =
            playerPosition + Vector3.up * 0.9f;

        for (int i = 0; i < spawnedObstacles.Count; i++)
        {
            GameObject obstacle = spawnedObstacles[i];

            if (obstacle == null)
                continue;

            Renderer[] renderers =
                obstacle.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
                continue;

            Bounds obstacleBounds = renderers[0].bounds;

            for (int rendererIndex = 1;
                 rendererIndex < renderers.Length;
                 rendererIndex++)
            {
                obstacleBounds.Encapsulate(
                    renderers[rendererIndex].bounds
                );
            }

            obstacleBounds.Expand(new Vector3(0.9f, 1.2f, 0.9f));

            if (obstacleBounds.Contains(playerCollisionPoint))
                return true;
        }

        return false;
    }

    public int CollectCoinsNearPlayer(Vector3 playerPosition)
    {
        int collectedCoins = 0;
        Vector3 playerCollectionPoint =
            playerPosition + Vector3.up * 0.9f;

        for (int i = spawnedCoins.Count - 1; i >= 0; i--)
        {
            GameObject coin = spawnedCoins[i];

            if (coin == null)
            {
                spawnedCoins.RemoveAt(i);
                continue;
            }

            coin.transform.Rotate(
                Vector3.up,
                120f * Time.deltaTime,
                Space.Self
            );

            if (Vector3.Distance(
                    playerCollectionPoint,
                    coin.transform.position
                ) > coinCollectionDistance)
            {
                continue;
            }

            Destroy(coin);
            spawnedCoins.RemoveAt(i);
            collectedCoins++;
        }

        return collectedCoins;
    }

    private void SpawnObstacle(
        Transform spawnPoint,
        int laneIndex,
        int rowIndex)
    {
        GameObject selectedPrefab =
            obstaclePrefabs[
                Random.Range(
                    0,
                    obstaclePrefabs.Length
                )
            ];

        if (selectedPrefab == null)
        {
            Debug.LogWarning(
                "ObstacleSpawner: Selected obstacle prefab is null."
            );

            return;
        }

        Vector3 spawnPosition =
            spawnPoint.position;

        spawnPosition.y +=
            verticalSpawnOffset;

        spawnPosition.z +=
            forwardSpawnOffset;

        GameObject obstacle =
            Instantiate(
                selectedPrefab,
                spawnPosition,
                spawnPoint.rotation
            );

        spawnedObstacles.Add(
            obstacle
        );

        if (showDebugLogs)
        {
            Debug.Log(
                $"Obstacle Spawned | " +
                $"Row: {rowIndex} | " +
                $"Lane: {GetLaneName(laneIndex)} | " +
                $"Position: {spawnPosition}"
            );
        }
    }

    private string GetLaneName(int laneIndex)
    {
        switch (laneIndex)
        {
            case 0:
                return "LEFT";

            case 1:
                return "CENTER";

            case 2:
                return "RIGHT";

            default:
                return "UNKNOWN";
        }
    }

    public void ClearSpawnedObstacles()
    {
        for (int i = 0;
             i < spawnedObstacles.Count;
             i++)
        {
            if (spawnedObstacles[i] != null)
            {
                Destroy(
                    spawnedObstacles[i]
                );
            }
        }

        spawnedObstacles.Clear();

        for (int i = 0; i < spawnedCoins.Count; i++)
        {
            if (spawnedCoins[i] != null)
            {
                Destroy(spawnedCoins[i]);
            }
        }

        spawnedCoins.Clear();

        obstaclesSpawned = false;
    }

    private void OnDestroy()
    {
        if (generatedCoinMaterial != null)
        {
            Destroy(generatedCoinMaterial);
        }
    }

    public void ResetSpawner()
    {
        ClearSpawnedObstacles();
    }

    private void ValidateSetup()
    {
        if (player == null)
        {
            Debug.LogWarning(
                "ObstacleSpawner: Player reference is not assigned. " +
                "It is currently not required for spawning."
            );
        }

        if (obstaclePrefabs == null ||
            obstaclePrefabs.Length == 0)
        {
            Debug.LogError(
                "ObstacleSpawner: No obstacle prefabs assigned."
            );
        }

        if (obstacleRows == null ||
            obstacleRows.Length == 0)
        {
            Debug.LogError(
                "ObstacleSpawner: No spawn rows assigned."
            );

            return;
        }

        for (int rowIndex = 0; rowIndex < obstacleRows.Length; rowIndex++)
        {
            for (int laneIndex = 0; laneIndex < 3; laneIndex++)
            {
                if (obstacleRows[rowIndex].GetSpawnPoint(laneIndex) == null)
                {
                    Debug.LogWarning(
                        $"ObstacleSpawner: Row {rowIndex} is missing its " +
                        $"{GetLaneName(laneIndex)} spawn point."
                    );
                }
            }
        }
    }
}