using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    // 스폰 위치 받기
    [Header("위치")]
    [SerializeField] Transform spawnPoint;
    // 나가는 위치 받기
    [SerializeField] Transform exitPoint;

    // 스폰할 Customer 프리팹 받기
    [Header("참조")]
    [SerializeField] Customer customerPrefab;
    [SerializeField] public CounterQueue queue;
    [SerializeField] public TableManager tables;
    
    [Header("설정")]
    [SerializeField] float spawnInterval = 3f;
    [SerializeField] int minOrder = 1;
    [SerializeField] int maxOrder = 3;

    int aliveCount;
    float timer;

    void Start() => timer = spawnInterval;   // 시작하자마자 첫 손님 생성

    // 언제 스폰할지 결정
    // 대기열에 손님이 꽉 차있지 않으면 스폰
    public void Update()
    {
        timer += Time.deltaTime;
        if (timer < spawnInterval || queue.IsFull) return;   // 줄이 꽉 차면 대기

        timer = 0f;

        Customer customer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        customer.Init(queue, tables, exitPoint, Random.Range(minOrder, maxOrder + 1));
    } 

    void SpawnCustomer(int requiredCount)
    {
        var obj = Instantiate(customerPrefab, spawnPoint.position,
                             spawnPoint.rotation);
        if (!obj.TryGetComponent(out Customer customer))
        {
            Debug.LogError("[CustomerSpawner] 프리팹에 Customer 컴포넌트가 없습니다.", customerPrefab);
            Destroy(obj);
            return;
        }

        customer.OnLeft += () => aliveCount--;   // 손님이 사라질 때 수를 줄임
        aliveCount++;
        customer.Init(queue, tables, exitPoint, requiredCount);
    }
}
