
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;
using TMPro;

public class PerformanceHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text performanceText;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private int targetFPS = 60;

    private ProfilerRecorder vertexRecorder;
    private ProfilerRecorder triangleRecorder;
    private ProfilerRecorder drawCallRecorder;

    private float elapsedTime;
    private int frameCount;

    private void Awake()
    {
        // 모바일의 기본 FPS 제한으로 인한 측정 왜곡 방지
        Application.targetFrameRate = targetFPS;
    }

    private void OnEnable()
    {
        vertexRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Vertices Count");

        triangleRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Triangles Count");

        drawCallRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Draw Calls Count");
    }

    private void Update()
    {
        elapsedTime += Time.unscaledDeltaTime;
        frameCount++;

        if (elapsedTime < updateInterval)
            return;

        float fps = frameCount / elapsedTime;
        float frameTimeMs = elapsedTime / frameCount * 1000f;

        long vertices = vertexRecorder.Valid
            ? vertexRecorder.LastValue : -1;

        long triangles = triangleRecorder.Valid
            ? triangleRecorder.LastValue : -1;

        long drawCalls = drawCallRecorder.Valid
            ? drawCallRecorder.LastValue : -1;

        performanceText.text =
            $"FPS: {fps:F1}\n" +
            $"Frame Time: {frameTimeMs:F2} ms\n" +
            $"Vertices: {FormatValue(vertices)}\n" +
            $"Triangles: {FormatValue(triangles)}\n" +
            $"Draw Calls: {FormatValue(drawCalls)}";

        elapsedTime = 0f;
        frameCount = 0;
    }

    private string FormatValue(long value)
    {
        return value < 0 ? "N/A" : value.ToString("N0");
    }

    private void OnDisable()
    {
        vertexRecorder.Dispose();
        triangleRecorder.Dispose();
        drawCallRecorder.Dispose();
    }
}
