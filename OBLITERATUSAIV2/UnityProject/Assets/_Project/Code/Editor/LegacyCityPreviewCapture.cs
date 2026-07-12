using System.IO;
using UnityEditor;
using UnityEngine;

namespace ObliteratusAI.EditorTools
{
    internal static class LegacyCityPreviewCapture
    {
        private const int Width = 1280;
        private const int Height = 720;

        [MenuItem("OBLITERATUS AI/Validation/Capture Legacy City Preview")]
        public static void Capture()
        {
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
                new Vector3(12f, 7f, -20f),
                new Vector3(0f, 3f, 18f),
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
