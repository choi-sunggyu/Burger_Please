using UnityEngine;

public class CubeSpawn : MonoBehaviour
{
    // 2초에 한 번씩 큐브 생성
    // maxCubeCount는 4개
    // max일 경우 "max"라고 TextMeshProUGUI에 표시
    // 생성된 큐브는 CubeSpawnPoint의 Transform 위치에 생성
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private Transform cubeSpawnPoint;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int columns = 2;            // 가로 칸 수
    [SerializeField] private int layers = 2;             // 높이 층 수
    [SerializeField] private Vector2 cellSize = new Vector2(0.4f, 0.3f); // (가로 간격, 층 높이)

    private float timer = 0f;

    public Transform SpawnPoint => cubeSpawnPoint;         // DeliverCubeJob이 큐브를 가지러 오는 곳

    private int MaxCubeCount => columns * layers;          // 2 x 2 = 4
    private int CurrentCount => cubeSpawnPoint.childCount; // 남아있는 큐브 수 = 자식 수
    public bool IsFull => CurrentCount >= MaxCubeCount;    // 나중에 "MAX" 표시용

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval && !IsFull)
        {
            SpawnCube();
            timer = 0f;
        }
    }

    private void SpawnCube()
    {
        GameObject cube = Instantiate(cubePrefab, cubeSpawnPoint.position, Quaternion.identity, cubeSpawnPoint);
        Relayout(); // 새 큐브 포함 전체 재배치
    }

    // 자식 순서대로 칸에 다시 배치: 중간 큐브를 가져가서 생긴 빈칸도 메워짐
    private void Relayout()
    {
        for (int i = 0; i < cubeSpawnPoint.childCount; i++)
        {
            cubeSpawnPoint.GetChild(i).localPosition = GetSlotPosition(i);
        }
    }

    private Vector3 GetSlotPosition(int index)
    {
        int col = index % columns;
        int layer = index / columns;
        return new Vector3(col * cellSize.x, layer * cellSize.y, 0f);
    }
}
