
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WorkerInteraction : MonoBehaviour
{
    [SerializeField] private SpeechBubbleUI speechBubble;

    private NavMeshAgent agent;
    private bool wasStopped;

    public bool IsInteracting { get; private set; }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (speechBubble == null)
        {
            speechBubble =
                GetComponentInChildren<SpeechBubbleUI>(true);
        }
    }

    public bool TryBeginInteraction()
    {
        if (!isActiveAndEnabled || IsInteracting)
            return false;

        if (!agent.enabled || !agent.isOnNavMesh)
            return false;

        // 상호작용 상태 선점
        IsInteracting = true;

        // 기존 이동 상태 저장 후 정지
        wasStopped = agent.isStopped;
        agent.isStopped = true;

        return true;
    }

    public void ShowBubble(string message)
    {
        if (speechBubble != null)
            speechBubble.Show(message);
    }

    public void HideBubble()
    {
        if (speechBubble != null)
            speechBubble.Hide();
    }

    public void EndInteraction()
    {
        if (!IsInteracting)
            return;

        HideBubble();

        // 대화 이전의 이동 상태 복구
        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = wasStopped;
        }

        IsInteracting = false;
    }

    private void OnDisable()
    {
        EndInteraction();
    }
}
