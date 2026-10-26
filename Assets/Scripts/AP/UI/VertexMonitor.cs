
using UnityEngine;
using Unity.Profiling;

public class VertexMonitor : MonoBehaviour
{
    private ProfilerRecorder vertexRecorder;
    private ProfilerRecorder drawCallRecorder;

    private float elapsed;
    private int frames;

    private void OnEnable()
    {
        vertexRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Vertices Count");

        drawCallRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Draw Calls Count");
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        frames++;

        if (elapsed < 1f) return;

        float fps = frames / elapsed;

        long vertices = vertexRecorder.Valid
            ? vertexRecorder.LastValue : -1;

        long drawCalls = drawCallRecorder.Valid
            ? drawCallRecorder.LastValue : -1;

        Debug.Log(
            $"FPS: {fps:F1} | " +
            $"Vertices: {vertices:N0} | " +
            $"Draw Calls: {drawCalls:N0}");

        elapsed = 0f;
        frames = 0;
    }

    private void OnDisable()
    {
        vertexRecorder.Dispose();
        drawCallRecorder.Dispose();
    }
}
