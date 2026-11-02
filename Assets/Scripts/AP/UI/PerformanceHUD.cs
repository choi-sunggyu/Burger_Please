
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;
using TMPro;
using System.Text;

public class PerformanceHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text performanceText;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private int targetFPS = 60;

    // Rendering
    private ProfilerRecorder vertexRecorder;
    private ProfilerRecorder triangleRecorder;
    private ProfilerRecorder drawCallRecorder;
    private ProfilerRecorder batchRecorder;
    private ProfilerRecorder setPassRecorder;

    // CPU / GPU Frame Timing
    private ProfilerRecorder cpuMainRecorder;
    private ProfilerRecorder cpuRenderRecorder;
    private ProfilerRecorder gpuFrameRecorder;

    private float elapsedTime;
    private int frameCount;

    private readonly StringBuilder sb = new StringBuilder(512);

    private void Awake()
    {
        Application.targetFrameRate = targetFPS;
    }

    private void OnEnable()
    {
        // Rendering statistics
        vertexRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Vertices Count");

        triangleRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Triangles Count");

        drawCallRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Draw Calls Count");

        batchRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Batches Count");

        setPassRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "SetPass Calls Count");

        // Frame timing
        cpuMainRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Internal,
            "CPU Main Thread Frame Time");

        cpuRenderRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Internal,
            "CPU Render Thread Frame Time");

        gpuFrameRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Internal,
            "GPU Frame Time");
    }

    private void Update()
    {
        elapsedTime += Time.unscaledDeltaTime;
        frameCount++;

        if (elapsedTime < updateInterval)
            return;

        float fps = frameCount / elapsedTime;
        float frameTimeMs = elapsedTime / frameCount * 1000f;

        // Rendering
        long vertices = ReadRecorder(vertexRecorder);
        long triangles = ReadRecorder(triangleRecorder);
        long drawCalls = ReadRecorder(drawCallRecorder);
        long batches = ReadRecorder(batchRecorder);
        long setPass = ReadRecorder(setPassRecorder);

        // Frame timing: nanoseconds -> milliseconds
        double cpuMainMs = ReadTimeMs(cpuMainRecorder);
        double cpuRenderMs = ReadTimeMs(cpuRenderRecorder);
        double gpuFrameMs = ReadTimeMs(gpuFrameRecorder);

        // Unity memory: bytes -> MB
        float allocatedMB =
            Profiler.GetTotalAllocatedMemoryLong()
            / (1024f * 1024f);

        float reservedMB =
            Profiler.GetTotalReservedMemoryLong()
            / (1024f * 1024f);

        float managedMB =
            Profiler.GetMonoUsedSizeLong()
            / (1024f * 1024f);

        // Device memory capacity
        int systemMemoryMB = SystemInfo.systemMemorySize;

        sb.Clear();

        sb.AppendLine($"FPS: {fps:F1}");
        sb.AppendLine($"Frame Time: {frameTimeMs:F2} ms");

        sb.AppendLine();
        sb.AppendLine($"CPU Main: {FormatTime(cpuMainMs)}");
        sb.AppendLine($"CPU Render: {FormatTime(cpuRenderMs)}");
        sb.AppendLine($"GPU Frame: {FormatTime(gpuFrameMs)}");

        sb.AppendLine();
        sb.AppendLine($"Vertices: {FormatValue(vertices)}");
        sb.AppendLine($"Triangles: {FormatValue(triangles)}");
        sb.AppendLine($"Draw Calls: {FormatValue(drawCalls)}");
        sb.AppendLine($"Batches: {FormatValue(batches)}");
        sb.AppendLine($"SetPass: {FormatValue(setPass)}");

        sb.AppendLine();
        sb.AppendLine($"Unity Allocated: {allocatedMB:F1} MB");
        sb.AppendLine($"Unity Reserved: {reservedMB:F1} MB");
        sb.AppendLine($"Managed Heap: {managedMB:F1} MB");
        sb.AppendLine(
            $"System RAM: {systemMemoryMB:N0} MB (Total)");

        if (performanceText != null)
            performanceText.text = sb.ToString();

        elapsedTime = 0f;
        frameCount = 0;
    }

    private long ReadRecorder(ProfilerRecorder recorder)
    {
        if (!recorder.Valid || recorder.Count == 0)
            return -1;

        return recorder.LastValue;
    }

    private double ReadTimeMs(ProfilerRecorder recorder)
    {
        long value = ReadRecorder(recorder);

        if (value <= 0)
            return -1;

        return value / 1_000_000.0;
    }

    private string FormatValue(long value)
    {
        return value < 0 ? "N/A" : value.ToString("N0");
    }

    private string FormatTime(double value)
    {
        return value < 0 ? "N/A" : $"{value:F2} ms";
    }

    private void OnDisable()
    {
        vertexRecorder.Dispose();
        triangleRecorder.Dispose();
        drawCallRecorder.Dispose();
        batchRecorder.Dispose();
        setPassRecorder.Dispose();

        cpuMainRecorder.Dispose();
        cpuRenderRecorder.Dispose();
        gpuFrameRecorder.Dispose();
    }
}
