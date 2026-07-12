using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ObliteratusAI.EditorTools
{
    /// <summary>
    /// Bakes the legacy Clouds.tsx radial-gradient blob canvas into a texture
    /// asset and an unlit transparent material for the CloudLayer billboards.
    /// </summary>
    internal static class CloudTextureBaker
    {
        private const string TextureFolder = "Assets/_Project/Art/Textures";
        private const string TexturePath = TextureFolder + "/T_Cloud.asset";
        private const string MaterialPath = "Assets/_Project/Art/Materials/M_Cloud.mat";
        private const int Size = 128;

        public static Material EnsureCloudMaterial()
        {
            Texture2D texture = EnsureTexture();

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "M_Cloud" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.82f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            // Behind scene transparents like the water plane.
            material.renderQueue = 2999;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D EnsureTexture()
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (existing != null)
            {
                return existing;
            }

            EditorBuildUtility.EnsureFolder(TextureFolder);
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true)
            {
                name = "T_Cloud",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            // Legacy blob stack (canvas y grows downward, so flip y).
            var blobs = new (float x, float y, float r, float a)[]
            {
                (64f, 70f, 52f, 0.85f),
                (42f, 62f, 34f, 0.7f),
                (88f, 60f, 30f, 0.75f),
                (64f, 52f, 26f, 0.6f)
            };

            Color32[] pixels = new Color32[Size * Size];
            for (int py = 0; py < Size; py++)
            {
                for (int px = 0; px < Size; px++)
                {
                    float canvasY = Size - 1 - py;
                    float alpha = 0f;
                    foreach (var blob in blobs)
                    {
                        float dx = px - blob.x;
                        float dy = canvasY - blob.y;
                        float f = Mathf.Sqrt(dx * dx + dy * dy) / blob.r;
                        float a;
                        if (f >= 1f)
                        {
                            a = 0f;
                        }
                        else if (f < 0.65f)
                        {
                            a = Mathf.Lerp(blob.a, blob.a * 0.5f, f / 0.65f);
                        }
                        else
                        {
                            a = Mathf.Lerp(blob.a * 0.5f, 0f, (f - 0.65f) / 0.35f);
                        }
                        alpha = alpha + a * (1f - alpha);
                    }
                    byte value = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
                    pixels[py * Size + px] = new Color32(255, 255, 255, value);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            AssetDatabase.CreateAsset(texture, TexturePath);
            return texture;
        }
    }
}
