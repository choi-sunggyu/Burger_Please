
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkerEncounterManager : MonoBehaviour
{
    [Serializable]
    private struct Conversation
    {
        public string lineA;
        public string lineB;
        public EmotionType emotionA;
        public EmotionType emotionB;
    }

    [Header("Detection")]
    [SerializeField] private float detectionRange = 2f;
    [SerializeField] private float separationRange = 3f;
    [SerializeField] private float scanInterval = 0.3f;

    [Header("Interaction")]
    [SerializeField, Range(0f, 1f)]
    private float encounterChance = 0.8f;

    [SerializeField] private float interactionDuration = 2f;

    [Header("Speech Bubble (Optional)")]
    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private SpeechBubbleUI bubblePrefab;

    [Header("Conversations")]
    [SerializeField] private Conversation[] conversations =
    {
        new Conversation
        {
            lineA = "오늘 물량 많네...",
            lineB = "조금만 버티자!",
            emotionA = EmotionType.Tired,
            emotionB = EmotionType.Happy
        },
        new Conversation
        {
            lineA = "벌써 퇴근 시간이야?",
            lineB = "아직 멀었어!",
            emotionA = EmotionType.Surprised,
            emotionB = EmotionType.Angry
        },
        new Conversation
        {
            lineA = "오늘 작업 잘했네!",
            lineB = "고마워!",
            emotionA = EmotionType.Happy,
            emotionB = EmotionType.Happy
        }
    };

    private readonly List<WorkerEncounter> workers = new();

    private readonly HashSet<ulong> attemptedPairs = new();
    private readonly HashSet<ulong> nearbyPairs = new();
    private readonly List<ulong> releaseKeys = new();

    private readonly HashSet<WorkerEncounter> busyWorkers = new();

    private readonly Stack<SpeechBubbleUI> bubblePool = new();
    private readonly HashSet<SpeechBubbleUI> activeBubbles = new();

    private void OnEnable()
    {
        StartCoroutine(ScanRoutine());
    }

    private IEnumerator ScanRoutine()
    {
        yield return null;

        var wait = new WaitForSeconds(
            Mathf.Max(0.05f, scanInterval)
        );

        while (true)
        {
            ScanWorkers();
            yield return wait;
        }
    }

    private static ulong GetPairKey(
        WorkerEncounter a, WorkerEncounter b)
    {
        uint idA = unchecked((uint)a.GetInstanceID());
        uint idB = unchecked((uint)b.GetInstanceID());

        if (idA > idB)
            (idA, idB) = (idB, idA);

        return ((ulong)idA << 32) | idB;
    }

    private void ScanWorkers()
    {
        workers.Clear();
        nearbyPairs.Clear();

        foreach (var worker in WorkerEncounter.ActiveWorkers)
        {
            if (worker != null && worker.isActiveAndEnabled)
                workers.Add(worker);
        }

        float detectSqr = detectionRange * detectionRange;

        float releaseRange = Mathf.Max(
            separationRange,
            detectionRange + 0.1f
        );

        float releaseSqr = releaseRange * releaseRange;

        for (int i = 0; i < workers.Count; i++)
        {
            for (int j = i + 1; j < workers.Count; j++)
            {
                WorkerEncounter a = workers[i];
                WorkerEncounter b = workers[j];

                Vector3 delta =
                    a.transform.position - b.transform.position;

                float distanceSqr = delta.sqrMagnitude;

                if (distanceSqr > releaseSqr)
                    continue;

                ulong key = GetPairKey(a, b);
                nearbyPairs.Add(key);

                if (distanceSqr > detectSqr)
                    continue;

                // 이번 만남에서 이미 시도했는가?
                if (attemptedPairs.Contains(key))
                    continue;

                if (!a.CanInteract || !b.CanInteract)
                    continue;

                if (conversations == null ||
                    conversations.Length == 0)
                    continue;

                // 이번 만남은 한 번만 시도
                attemptedPairs.Add(key);

                float chance =
                    encounterChance *
                    (a.Sociability + b.Sociability) * 0.5f;

                if (UnityEngine.Random.value > chance)
                    continue;

                // 두 알바의 상호작용 상태 확보
                if (!a.Interaction.TryBeginInteraction())
                    continue;

                if (!b.Interaction.TryBeginInteraction())
                {
                    a.Interaction.EndInteraction();
                    continue;
                }

                busyWorkers.Add(a);
                busyWorkers.Add(b);

                Conversation dialogue =
                    conversations[
                        UnityEngine.Random.Range(
                            0, conversations.Length
                        )
                    ];

                StartCoroutine(
                    ConversationRoutine(a, b, dialogue)
                );
            }
        }

        // 서로 멀어진 조합은 다시 만날 수 있음
        releaseKeys.Clear();

        foreach (ulong key in attemptedPairs)
        {
            if (!nearbyPairs.Contains(key))
                releaseKeys.Add(key);
        }

        foreach (ulong key in releaseKeys)
            attemptedPairs.Remove(key);
    }

    private SpeechBubbleUI AcquireBubble(
        WorkerEncounter worker)
    {
        if (bubblePrefab == null || canvasRoot == null)
            return null;

        SpeechBubbleUI bubble =
            bubblePool.Count > 0
                ? bubblePool.Pop()
                : Instantiate(bubblePrefab, canvasRoot);

        bubble.gameObject.SetActive(true);

        bubble.Bind(
            worker.Emotion.FollowTarget,
            worker.Emotion.Offset
        );

        bubble.Hide();
        activeBubbles.Add(bubble);

        return bubble;
    }

    private void ReleaseBubble(SpeechBubbleUI bubble)
    {
        if (bubble == null || !activeBubbles.Remove(bubble))
            return;

        bubble.Hide();
        bubble.gameObject.SetActive(false);
        bubblePool.Push(bubble);
    }

    private static bool IsValid(WorkerEncounter worker)
    {
        return worker != null &&
               worker.isActiveAndEnabled &&
               worker.Interaction != null &&
               worker.Interaction.IsInteracting;
    }

    private IEnumerator ConversationRoutine(
        WorkerEncounter a,
        WorkerEncounter b,
        Conversation dialogue)
    {
        SpeechBubbleUI bubbleA = AcquireBubble(a);
        SpeechBubbleUI bubbleB = AcquireBubble(b);

        // 두 캐릭터에 감정 이모티콘 표시
        a.Emotion.ShowEmotion(dialogue.emotionA);
        b.Emotion.ShowEmotion(dialogue.emotionB);

        if (bubbleA != null)
            bubbleA.Show(dialogue.lineA);

        float half = Mathf.Max(0.2f, interactionDuration) * 0.5f;

        yield return new WaitForSeconds(half);

        if (bubbleA != null)
            bubbleA.Hide();

        if (IsValid(a) && IsValid(b))
        {
            if (bubbleB != null)
                bubbleB.Show(dialogue.lineB);

            yield return new WaitForSeconds(half);
        }

        ReleaseBubble(bubbleA);
        ReleaseBubble(bubbleB);

        FinishInteraction(a, b);
    }

    private void FinishInteraction(
        WorkerEncounter a, WorkerEncounter b)
    {
        if (a != null)
        {
            if (a.Interaction != null)
                a.Interaction.EndInteraction();

            busyWorkers.Remove(a);
        }

        if (b != null)
        {
            if (b.Interaction != null)
                b.Interaction.EndInteraction();

            busyWorkers.Remove(b);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        foreach (var worker in busyWorkers)
        {
            if (worker != null && worker.Interaction != null)
                worker.Interaction.EndInteraction();
        }

        busyWorkers.Clear();

        foreach (var bubble in activeBubbles)
        {
            if (bubble == null)
                continue;

            bubble.Hide();
            bubble.gameObject.SetActive(false);
            bubblePool.Push(bubble);
        }

        activeBubbles.Clear();
        attemptedPairs.Clear();
        nearbyPairs.Clear();
    }
}
