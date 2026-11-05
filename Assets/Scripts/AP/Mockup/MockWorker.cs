using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 목업용 알바: 하차 트럭 → 적재함, 적재함 → 상차 트럭을 번갈아 반복한다
// 씬이 큰 스케일이라 기존 GrabAction(부모 기준 스케일)을 쓰지 않고 박스를 직접 든다
[RequireComponent(typeof(NavMover))]
public class MockWorker : MonoBehaviour
{
    private const float ArriveDistance = 6f;      // 이 거리 안이면 도착 (여러 명이 겹쳐도 멈추지 않도록 넉넉하게)
    private const float WorkTime = 0.3f;          // 박스를 집거나 놓을 때 멈추는 시간(초)
    private const float SearchInterval = 0.3f;    // 일이 없을 때 다시 찾는 간격(초)
    private const int ShelfTargetCount = 8;       // 적재함에 이만큼 쌓일 때까지 하차를 우선

    private static readonly int WalkHash = Animator.StringToHash("Walk");

    private NavMeshAgent agent;
    private NavMover mover;
    private Animator anim;
    private Transform grabPoint;
    private CargoTruck unloadTruck;
    private CargoTruck loadTruck;
    private ShelfStorage storage;
    private GameObject heldBox;
    private Quaternion heldBoxRotation;

    public void Init(CargoTruck unloadTruck, CargoTruck loadTruck, ShelfStorage storage, float moveSpeed)
    {
        this.unloadTruck = unloadTruck;
        this.loadTruck = loadTruck;
        this.storage = storage;

        DisablePrefabBehaviours();

        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.acceleration = moveSpeed * 4f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance = 0f;

        mover = GetComponent<NavMover>();
        anim = GetComponentInChildren<Animator>();
        grabPoint = FindGrabPoint();

        StartCoroutine(WorkLoop());
    }

    // Employee 프리팹의 기존 두뇌/줍기/돌아다니기를 끄고 이 스크립트만 움직이게 함
    private void DisablePrefabBehaviours()
    {
        foreach (MonoBehaviour behaviour in GetComponentsInChildren<MonoBehaviour>())
        {
            if (behaviour != this && !(behaviour is NavMover)) behaviour.enabled = false;
        }

        foreach (NavMeshAgent childAgent in GetComponentsInChildren<NavMeshAgent>())
        {
            if (childAgent.gameObject != gameObject) childAgent.enabled = false;
        }
    }

    private Transform FindGrabPoint()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.name == "GrabPoint") return child;
        }
        return transform;
    }

    private void Update()
    {
        if (anim != null) anim.SetBool(WalkHash, mover.IsMoving);
    }

    private IEnumerator WorkLoop()
    {
        var wait = new WaitForSeconds(SearchInterval);

        while (true)
        {
            bool canUnload = unloadTruck.HasWork && storage.HasEmptySlot;
            bool canLoad = loadTruck.HasWork && storage.HasBox;

            // 둘 다 가능하면 적재함을 일정량 채운 뒤에 상차 (선반에 쌓이는 모습이 보이도록)
            if (canUnload && (!canLoad || storage.StoredCount < ShelfTargetCount))
                yield return UnloadOneBox();
            else if (canLoad)
                yield return LoadOneBox();
            else
                yield return wait;
        }
    }

    private IEnumerator UnloadOneBox()
    {
        unloadTruck.Reserve();
        ShelfStorage.Slot slot = storage.ReserveEmptySlot();

        yield return MoveTo(unloadTruck.WorkPoint);
        Hold(unloadTruck.TakeBox());
        yield return new WaitForSeconds(WorkTime);

        yield return MoveTo(storage.GetStandPoint(slot, transform.position));
        heldBox.transform.rotation = heldBoxRotation;   // 들고 도는 동안 바뀐 방향을 되돌림
        storage.Store(slot, heldBox);
        heldBox = null;
        yield return new WaitForSeconds(WorkTime);
    }

    private IEnumerator LoadOneBox()
    {
        loadTruck.Reserve();
        ShelfStorage.Slot slot = storage.ReserveBox();

        yield return MoveTo(storage.GetStandPoint(slot, transform.position));
        Hold(storage.Take(slot));
        yield return new WaitForSeconds(WorkTime);

        yield return MoveTo(loadTruck.WorkPoint);
        loadTruck.PutBox(heldBox);
        heldBox = null;
        yield return new WaitForSeconds(WorkTime);
    }

    private void Hold(GameObject box)
    {
        heldBox = box;
        heldBoxRotation = box.transform.rotation;
        box.transform.SetParent(grabPoint, true);   // 월드 크기 유지
        box.transform.position = grabPoint.position;
    }

    private IEnumerator MoveTo(Vector3 destination)
    {
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 30f, NavMesh.AllAreas))
            destination = hit.position;

        mover.MoveTo(destination);
        yield return null;

        while (agent.pathPending || agent.remainingDistance > ArriveDistance)
            yield return null;

        mover.Stop();
    }
}
