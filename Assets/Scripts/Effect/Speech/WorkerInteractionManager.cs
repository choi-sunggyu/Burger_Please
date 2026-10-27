
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkerInteractionManager : MonoBehaviour
{
    [Serializable]
    private class InteractionPair
    {
        public WorkerInteraction workerA;
        public WorkerInteraction workerB;

        [TextArea] public string lineA = "오늘 물량 많네";
        [TextArea] public string lineB = "퇴근하고 싶다";

        [NonSerialized] public bool inProgress;
        [NonSerialized] public bool requiresSeparation;
        [NonSerialized] public float nextAllowedTime;
    }

    [Header("Detection")]
    [SerializeField] private float detectionDistance = 2f;
    [SerializeField] private float separationDistance = 3f;
    [SerializeField] private float scanInterval = 0.25f;

    [Header("Conversation")]
    [SerializeField] private float talkDuration = 2f;
    [SerializeField] private float cooldown = 5f;

    [Header("Allowed Pairs")]
    [SerializeField]
    private List<InteractionPair> pairs = new();

    private void OnEnable()
    {
        StartCoroutine(DetectRoutine());
    }

    private IEnumerator DetectRoutine()
    {
        // 모든 NPC의 Awake 이후 검사 시작
        yield return null;

        var wait = new WaitForSeconds(
            Mathf.Max(0.05f, scanInterval)
        );

        while (true)
        {
            foreach (var pair in pairs)
            {
                if (pair.workerA == null ||
                    pair.workerB == null ||
                    pair.workerA == pair.workerB)
                    continue;

                if (pair.inProgress)
                    continue;

                if (!pair.workerA.isActiveAndEnabled ||
                    !pair.workerB.isActiveAndEnabled)
                    continue;

                Vector3 delta =
                    pair.workerA.transform.position -
                    pair.workerB.transform.position;

                float distanceSqr = delta.sqrMagnitude;

                // 한 번 헤어진 후에 다시 대화 가능
                if (pair.requiresSeparation)
                {
                    float resetRange = Mathf.Max(
                        separationDistance,
                        detectionDistance + 0.5f
                    );

                    if (distanceSqr >= resetRange * resetRange)
                        pair.requiresSeparation = false;

                    continue;
                }

                if (Time.time < pair.nextAllowedTime)
                    continue;

                if (distanceSqr >
                    detectionDistance * detectionDistance)
                    continue;

                // 두 알바 모두 상호작용 상태 확보
                if (!pair.workerA.TryBeginInteraction())
                    continue;

                if (!pair.workerB.TryBeginInteraction())
                {
                    pair.workerA.EndInteraction();
                    continue;
                }

                pair.inProgress = true;
                StartCoroutine(TalkRoutine(pair));
            }

            yield return wait;
        }
    }

    private IEnumerator TalkRoutine(InteractionPair pair)
    {
        float halfDuration =
            Mathf.Max(0.1f, talkDuration) * 0.5f;

        if (!AreWorkersValid(pair))
        {
            FinishInteraction(pair);
            yield break;
        }

        // 첫 번째 알바 대사
        pair.workerA.ShowBubble(pair.lineA);

        yield return new WaitForSeconds(halfDuration);

        if (!AreWorkersValid(pair))
        {
            FinishInteraction(pair);
            yield break;
        }

        pair.workerA.HideBubble();

        // 두 번째 알바 대사
        pair.workerB.ShowBubble(pair.lineB);

        yield return new WaitForSeconds(halfDuration);

        FinishInteraction(pair);
    }

    private bool AreWorkersValid(InteractionPair pair)
    {
        return pair.workerA != null &&
               pair.workerB != null &&
               pair.workerA.isActiveAndEnabled &&
               pair.workerB.isActiveAndEnabled;
    }

    private void FinishInteraction(InteractionPair pair)
    {
        if (pair.workerA != null)
            pair.workerA.EndInteraction();

        if (pair.workerB != null)
            pair.workerB.EndInteraction();

        pair.inProgress = false;

        // 같은 위치에서 연속 대화하는 현상 방지
        pair.requiresSeparation = true;
        pair.nextAllowedTime = Time.time + cooldown;
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        foreach (var pair in pairs)
        {
            if (pair.inProgress)
                FinishInteraction(pair);
        }
    }
}
