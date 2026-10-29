
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(
    typeof(WorkerInteraction),
    typeof(WorkerEmotion))]
public class WorkerEncounter : MonoBehaviour
{
    public static readonly HashSet<WorkerEncounter>
        ActiveWorkers = new();

    [Header("Social Settings")]
    [SerializeField, Range(0f, 1f)]
    private float sociability = 0.7f;

    [SerializeField]
    private bool canSocialize = true;

    public WorkerInteraction Interaction { get; private set; }
    public WorkerEmotion Emotion { get; private set; }

    public float Sociability => sociability;

    public bool CanInteract =>
        canSocialize &&
        isActiveAndEnabled &&
        Interaction != null &&
        Emotion != null &&
        Emotion.isActiveAndEnabled &&
        Interaction.isActiveAndEnabled &&
        !Interaction.IsInteracting;

    private void Awake()
    {
        Interaction = GetComponent<WorkerInteraction>();
        Emotion = GetComponent<WorkerEmotion>();
    }

    private void OnEnable()
    {
        ActiveWorkers.Add(this);
    }

    private void OnDisable()
    {
        ActiveWorkers.Remove(this);
    }

    // 업무 수행 중에는 대화하지 않도록 제어 가능
    public void SetCanSocialize(bool value)
    {
        canSocialize = value;
    }
}
