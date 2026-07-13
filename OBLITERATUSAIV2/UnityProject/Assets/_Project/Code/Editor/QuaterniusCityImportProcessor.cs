using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace ObliteratusAI.EditorTools
{
    internal sealed class QuaterniusCityImportProcessor : AssetPostprocessor
    {
        internal const string SourceRoot =
            "Assets/ThirdParty/Quaternius/DowntownCityMegaKitStandard";

        // Bumped whenever the import settings below change so Unity
        // reimports the already-imported kit files (v2: readable meshes,
        // no mesh compression, 2048 px textures).
        public override uint GetVersion()
        {
            return 2;
        }

        private static readonly HashSet<string> RecalculateNormalModels =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Brick_InteriorWall_1",
                "Brick_InteriorWall_3",
                "Brick_InteriorWall_4",
                "Brick_Plain_1",
                "Brick_Plain_3",
                "Brick_Plain_3_noWear",
                "Brick_Plain_4",
                "Brick_TopTrim",
                "Metal_Plain_1",
                "Metal_Plain_3",
                "Trim_Plain_3"
            };

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(SourceRoot, StringComparison.Ordinal))
            {
                return;
            }

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importConstraints = false;
            importer.addCollider = false;
            // Readable so the modular building baker can combine module
            // meshes into single per-building assets. Compression is off so
            // module edges stay exactly on the 2 m grid without hairline
            // cracks between combined tiles.
            importer.isReadable = true;
            importer.meshCompression = ModelImporterMeshCompression.Off;

            if (RecalculateNormalModels.Contains(Path.GetFileNameWithoutExtension(assetPath)))
            {
                importer.importNormals = ModelImporterNormals.Calculate;
                importer.normalCalculationMode = ModelImporterNormalCalculationMode.AngleWeighted;
                importer.normalSmoothingAngle = 60f;
            }
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SourceRoot, StringComparison.Ordinal))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = true;
            importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 70;

            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (fileName.EndsWith("_Normal", StringComparison.OrdinalIgnoreCase))
            {
                importer.textureType = TextureImporterType.NormalMap;
            }
            else if (fileName.EndsWith("_ORM", StringComparison.OrdinalIgnoreCase))
            {
                importer.sRGBTexture = false;
            }
        }
    }
}
