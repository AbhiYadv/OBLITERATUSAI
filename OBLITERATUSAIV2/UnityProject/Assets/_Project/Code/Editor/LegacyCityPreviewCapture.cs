using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObliteratusAI.EditorTools
{
    internal static class LegacyCityPreviewCapture
    {
        private const int Width = 1280;
        private const int Height = 720;
        private const string ScenePath = "Assets/_Project/Scenes/Test/PlayerSandbox.unity";

        [MenuItem("OBLITERATUS AI/Validation/Capture Legacy City Preview")]
        public static void Capture()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"Cannot capture city preview because the scene is missing: {ScenePath}");
                return;
            }

            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string logDirectory = Path.Combine(projectRoot, "Logs");
            Directory.CreateDirectory(logDirectory);

            RenderPreview(
                Path.Combine(logDirectory, "LegacyCityPreview.png"),
                new Vector3(190f, 145f, -190f),
                new Vector3(0f, 22f, 0f),
                52f);
            RenderPreview(
                Path.Combine(logDirectory, "LegacyCityStreetPreview.png"),
                new Vector3(4.2f, 5.5f, -42f),
                new Vector3(0f, 4f, 24f),
                62f);
            Debug.Log($"Legacy city validation previews captured in {logDirectory}");
        }

        private static void RenderPreview(string outputPath, Vector3 position, Vector3 target, float fieldOfView)
        {
            GameObject cameraObject = new GameObject("ValidationPreviewCamera");
            Camera previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.transform.position = position;
            previewCamera.transform.LookAt(target);
            previewCamera.fieldOfView = fieldOfView;
            previewCamera.nearClipPlane = 0.3f;
            previewCamera.farClipPlane = 700f;
            previewCamera.clearFlags = CameraClearFlags.Skybox;
            previewCamera.allowHDR = false;

            RenderTexture renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            previewCamera.targetTexture = renderTexture;
            previewCamera.Render();
            RenderTexture.active = renderTexture;
            image.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            image.Apply();
            File.WriteAllBytes(outputPath, image.EncodeToPNG());

            previewCamera.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(cameraObject);
        }
    }
}
