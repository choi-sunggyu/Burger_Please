
using System;
using UnityEngine;

public enum EmotionType
{
    Happy,
    Angry,
    Tired,
    Surprised
}

public class WorkerEmotion : MonoBehaviour
{
    public static event Action<WorkerEmotion, EmotionType>
        EmotionRequested;

    public static event Action<WorkerEmotion>
        WorkerDisabled;

    [Header("Position")]
    [SerializeField] private Transform headAnchor;

    [SerializeField]
    private Vector3 rootOffset = new Vector3(0, 2.2f, 0);

    [SerializeField]
    private Vector3 anchorOffset = new Vector3(0, 0.3f, 0);

    public Transform FollowTarget =>
        headAnchor != null ? headAnchor : transform;

    public Vector3 Offset =>
        headAnchor != null ? anchorOffset : rootOffset;

    public void ShowEmotion(EmotionType emotion)
    {
        if (!isActiveAndEnabled)
            return;

        EmotionRequested?.Invoke(this, emotion);
    }

    private void OnDisable()
    {
        WorkerDisabled?.Invoke(this);
    }

    // Inspector에서 테스트
    [ContextMenu("Test/Happy")]
    private void TestHappy()
    {
        if (Application.isPlaying)
            ShowEmotion(EmotionType.Happy);
    }

    [ContextMenu("Test/Angry")]
    private void TestAngry()
    {
        if (Application.isPlaying)
            ShowEmotion(EmotionType.Angry);
    }

    [ContextMenu("Test/Tired")]
    private void TestTired()
    {
        if (Application.isPlaying)
            ShowEmotion(EmotionType.Tired);
    }

    [ContextMenu("Test/Surprised")]
    private void TestSurprised()
    {
        if (Application.isPlaying)
            ShowEmotion(EmotionType.Surprised);
    }
}
