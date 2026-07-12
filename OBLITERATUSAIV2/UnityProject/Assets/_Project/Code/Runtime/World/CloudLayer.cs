using ObliteratusAI.Core;
using UnityEngine;

namespace ObliteratusAI.World
{
    /// <summary>
    /// Lightweight sky detail: seeded camera-facing cloud billboards drawn in
    /// one instanced call, drifting slowly east and wrapping at the world
    /// edge. Ported from the legacy Clouds.tsx (deliberately not volumetric).
    /// </summary>
    public sealed class CloudLayer : MonoBehaviour
    {
        private const float DriftSpeed = 1.6f;

        [SerializeField] private Material material;
        [SerializeField] private int seed = 20260702;
        [SerializeField] private int count = 46;
        [SerializeField] private float wrap = 800f;
        [SerializeField] private float minAltitude = 180f;
        [SerializeField] private float maxAltitude = 280f;

        private Vector3[] _positions;
        private Vector2[] _sizes;
        private Matrix4x4[] _matrices;
        private Mesh _quad;
        private RenderParams _renderParams;

        /// <summary>Editor-time wiring used by the sandbox builder.</summary>
        public void Configure(Material cloudMaterial)
        {
            material = cloudMaterial;
        }

        private void Start()
        {
            if (material == null)
            {
                Debug.LogWarning("CloudLayer has no material; clouds disabled.", this);
                enabled = false;
                return;
            }

            DeterministicRandom rand = new DeterministicRandom(unchecked((uint)seed ^ 0xc10bd5u));
            _positions = new Vector3[count];
            _sizes = new Vector2[count];
            _matrices = new Matrix4x4[count];
            for (int i = 0; i < count; i++)
            {
                float w = 110f + rand.NextFloat() * 190f;
                _positions[i] = new Vector3(
                    (rand.NextFloat() * 2f - 1f) * wrap,
                    minAltitude + rand.NextFloat() * (maxAltitude - minAltitude),
                    (rand.NextFloat() * 2f - 1f) * wrap);
                _sizes[i] = new Vector2(w, w * (0.32f + rand.NextFloat() * 0.16f));
            }

            _quad = BuildQuad();
            _renderParams = new RenderParams(material)
            {
                shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                receiveShadows = false,
                worldBounds = new Bounds(Vector3.zero, new Vector3(wrap * 2f + 400f, maxAltitude * 2f + 200f, wrap * 2f + 400f))
            };
        }

        private void LateUpdate()
        {
            Camera viewCamera = Camera.main;
            if (viewCamera == null || _matrices == null)
            {
                return;
            }

            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            Quaternion facing = viewCamera.transform.rotation;
            for (int i = 0; i < _positions.Length; i++)
            {
                Vector3 p = _positions[i];
                p.x += DriftSpeed * dt;
                if (p.x > wrap)
                {
                    p.x = -wrap;
                }
                _positions[i] = p;
                _matrices[i] = Matrix4x4.TRS(p, facing, new Vector3(_sizes[i].x, _sizes[i].y, 1f));
            }

            Graphics.RenderMeshInstanced(_renderParams, _quad, 0, _matrices);
        }

        private void OnDestroy()
        {
            if (_quad != null)
            {
                Destroy(_quad);
            }
        }

        private static Mesh BuildQuad()
        {
            Mesh mesh = new Mesh { name = "CloudQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.normals = new[]
            {
                new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0f, -1f)
            };
            return mesh;
        }
    }
}
