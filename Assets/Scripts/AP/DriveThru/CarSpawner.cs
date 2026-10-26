using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] Car carPrefab;            // GameObject가 아니라 Car 타입으로 받기
    [SerializeField] DriveThruLane lane;
    [SerializeField] float spawnInterval = 6f;
    [SerializeField] int minOrder = 1;
    [SerializeField] int maxOrder = 3;         // 포함

    float timer;

    void Start() => timer = spawnInterval;     // 시작하자마자 첫 차

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < spawnInterval || !lane.CanAccept) return;

        timer = 0f;
        Car car = Instantiate(carPrefab);
        car.Init(lane, Random.Range(minOrder, maxOrder + 1));
    }
}