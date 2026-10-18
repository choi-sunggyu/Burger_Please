using System.Collections.Generic;
using UnityEngine;

// PackingPoint(Is Trigger)에 부착
// 플레이어/직원이 서 있고 재료 큐브가 충분하면, 큐브 N개 → CubePack 1개를 포장대에 올림
public class Packing : MonoBehaviour
{
    [SerializeField] CubeSpot cubeSpot;          // 재료 큐브가 쌓인 곳 (DriveThru CubeZone)
    [SerializeField] CubePack packPrefab;
    [SerializeField] Transform packTable;        // 완성품을 쌓는 곳 (Scale 1,1,1)
    [SerializeField] int cubesPerPack = 4;
    [SerializeField] int maxPacksOnTable = 4;
    [SerializeField] float packInterval = 0.5f;
    [SerializeField] float packStackHeight = 0.4f;

    public Transform PackTable => packTable;

    // 지금 포장할 수 있나 (재료 충분 + 포장대에 자리 있음) → PackJob이 사용
    public bool CanPack => cubeSpot != null && packTable != null
                           && cubeSpot.Count >= cubesPerPack
                           && packTable.childCount < maxPacksOnTable;

    readonly HashSet<Collider> staff = new HashSet<Collider>();
    float timer;

    bool HasStaff
    {
        get
        {
            staff.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            return staff.Count > 0;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < packInterval) return;

        if (!HasStaff) return;
        if (!CanPack) return;                                    // 재료 부족 또는 포장대가 꽉 참

        for (int i = 0; i < cubesPerPack; i++)
        {
            Stuff cube = cubeSpot.TakeTop();
            if (cube != null) Destroy(cube.gameObject);
        }

        Vector3 pos = packTable.position + Vector3.up * packStackHeight * packTable.childCount;
        Instantiate(packPrefab, pos, packTable.rotation, packTable);
        timer = 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Carry>() != null) staff.Add(other);
    }

    void OnTriggerExit(Collider other) => staff.Remove(other);
}