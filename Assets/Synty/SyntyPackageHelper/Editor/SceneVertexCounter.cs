
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static class SceneVertexCounter
{
    [MenuItem("Tools/Count Scene Vertices")]
    public static void CountVertices()
    {
        int totalVertices = 0;
        int totalTriangles = 0;

        Scene scene = SceneManager.GetActiveScene();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // 일반 Mesh
            foreach (MeshFilter filter in
                     root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null) continue;

                totalVertices += mesh.vertexCount;

                for (int i = 0; i < mesh.subMeshCount; i++)
                    totalTriangles += (int)
                        (mesh.GetIndexCount(i) / 3);
            }

            // 애니메이션 Mesh
            foreach (SkinnedMeshRenderer renderer in
                     root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh == null) continue;

                totalVertices += mesh.vertexCount;

                for (int i = 0; i < mesh.subMeshCount; i++)
                    totalTriangles += (int)
                        (mesh.GetIndexCount(i) / 3);
            }
        }

        Debug.Log(
            $"Scene: {scene.name}\n" +
            $"Total Vertices: {totalVertices:N0}\n" +
            $"Total Triangles: {totalTriangles:N0}"
        );
    }
}
