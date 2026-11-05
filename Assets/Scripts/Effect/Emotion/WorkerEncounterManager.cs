
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 태그로 직원을 찾아 가끔 근처의 두 명이 대화하게 한다. 대화는 표시만 하고 직원의 이동/업무는 멈추지 않는다.
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

    [Header("Discovery")]
    [SerializeField] private string employeeTag = "Employee";
    [SerializeField] private float scanInterval = 1f;

    // 스캔마다 태그로 다시 채우므로 중간에 고용/해고해도 자동 반영 (Inspector 확인용)
    [SerializeField] private List<WorkerEncounter> workers = new();

    [Header("Interaction")]
    [SerializeField] private float detectionRange = 3f;
    [SerializeField] private Vector2 eventInterval = new Vector2(45f, 120f);   // 다음 대화까지 대기 (최소, 최대 초)
    [SerializeField] private float lineDuration = 2f;

    [Header("Speech Bubble")]
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

    private SpeechBubbleUI bubble;   // 대사는 한 번에 한 명만 하므로 하나로 충분
    private float nextEventTime;

    private void OnEnable()
    {
        StartCoroutine(ScanRoutine());
    }

    private IEnumerator ScanRoutine()
    {
        ScheduleNextEvent();

        var wait = new WaitForSeconds(Mathf.Max(0.05f, scanInterval));

        while (true)
        {
            RefreshWorkers();

            if (Time.time >= nextEventTime &&
                conversations.Length > 0 &&
                TryPickNearbyPair(out WorkerEncounter a, out WorkerEncounter b))
            {
                // 사교성이 낮은 조합은 이번 기회를 넘길 수 있음
                if (UnityEngine.Random.value <= (a.Sociability + b.Sociability) * 0.5f)
                {
                    Conversation dialogue = conversations[UnityEngine.Random.Range(0, conversations.Length)];
                    yield return Say(a, dialogue.lineA, dialogue.emotionA);
                    if (a != null) yield return Say(b, dialogue.lineB, dialogue.emotionB);
                }

                ScheduleNextEvent();
            }

            yield return wait;
        }
    }

    private void ScheduleNextEvent()
    {
        nextEventTime = Time.time + UnityEngine.Random.Range(eventInterval.x, eventInterval.y);
    }

    private void RefreshWorkers()
    {
        workers.Clear();

        // 프리팹의 루트와 Worker 자식이 둘 다 태그를 달고 있어 같은 직원이 두 번 잡히므로 중복 제거
        foreach (GameObject tagged in GameObject.FindGameObjectsWithTag(employeeTag))
        {
            WorkerEncounter worker = tagged.GetComponentInChildren<WorkerEncounter>();
            if (worker != null && !workers.Contains(worker))
                workers.Add(worker);
        }
    }

    // 범위 안의 조합 중 무작위로 하나 (저수지 샘플링: 조합을 따로 모으지 않음)
    private bool TryPickNearbyPair(out WorkerEncounter a, out WorkerEncounter b)
    {
        a = b = null;
        int found = 0;
        float rangeSqr = detectionRange * detectionRange;

        for (int i = 0; i < workers.Count; i++)
        {
            if (!workers[i].CanInteract)
                continue;

            for (int j = i + 1; j < workers.Count; j++)
            {
                if (!workers[j].CanInteract)
                    continue;

                Vector3 delta = workers[i].transform.position - workers[j].transform.position;
                if (delta.sqrMagnitude > rangeSqr)
                    continue;

                found++;
                if (UnityEngine.Random.Range(0, found) == 0)
                {
                    a = workers[i];
                    b = workers[j];
                }
            }
        }

        return found > 0;
    }

    private IEnumerator Say(WorkerEncounter speaker, string line, EmotionType emotion)
    {
        if (speaker == null || !speaker.isActiveAndEnabled)
            yield break;

        speaker.Emotion.ShowEmotion(emotion);

        if (bubble == null && bubblePrefab != null && canvasRoot != null)
            bubble = Instantiate(bubblePrefab, canvasRoot);

        if (bubble != null)
        {
            bubble.Bind(speaker.Emotion.FollowTarget, speaker.Emotion.Offset);
            bubble.Show(line);
        }

        yield return new WaitForSeconds(lineDuration);

        if (bubble != null)
            bubble.Hide();
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (bubble != null)
            bubble.Hide();
    }

    // 드물게 일어나는 이벤트라 확인용으로 대기 시간을 건너뜀 (근처에 두 명이 있어야 시작됨)
    [ContextMenu("Test/Next Event Now")]
    private void TestNextEventNow()
    {
        nextEventTime = 0f;
    }
}
