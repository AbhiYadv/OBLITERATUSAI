using System;
using System.IO;
using ObliteratusAI.Pedestrians;
using ObliteratusAI.Player;
using ObliteratusAI.Traffic;
using ObliteratusAI.Vehicles;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

namespace ObliteratusAI.Debugging
{
    /// <summary>
    /// Lightweight runtime performance readout. F3 toggles the HUD; F4 records
    /// a fixed 10-second baseline JSON under Application.persistentDataPath.
    /// Profiler counters degrade to "n/a" when a player build does not expose
    /// a requested metric.
    /// </summary>
    public sealed class PerformanceHud : MonoBehaviour
    {
        private const float BaselineDuration = 10f;
        private const int MaxBaselineFrames = 3600;
        private const float Megabyte = 1024f * 1024f;

        [SerializeField] private bool showOnStart;
        [SerializeField, Range(0.05f, 1f)] private float refreshInterval = 0.25f;

        private readonly float[] _baselineFrameTimes = new float[MaxBaselineFrames];

        private ProfilerRecorder _mainThreadRecorder;
        private ProfilerRecorder _drawCallsRecorder;
        private ProfilerRecorder _trianglesRecorder;
        private PedestrianSystem _pedestrians;
        private TrafficSystem _traffic;
        private PlayerMotor _player;
        private VehicleSeat _vehicleSeat;
        private DebugTools _debugTools;
        private GUIStyle _panelStyle;
        private string _hudText = string.Empty;
        private string _lastBaselinePath = string.Empty;
        private float _smoothedDelta = 1f / 60f;
        private float _nextRefresh;
        private float _nextReferenceResolve;
        private bool _visible;
        private bool _recordersStarted;

        private bool _recording;
        private float _baselineStartedAt;
        private float _baselineDuration;
        private int _baselineFrameCount;
        private long _peakDrawCalls;
        private long _peakTriangles;
        private long _peakAllocatedMemory;
        private long _peakReservedMemory;

        public bool IsVisible => _visible;
        public bool IsRecording => _recording;
        public string LastBaselinePath => _lastBaselinePath;

        private void OnEnable()
        {
            _visible = showOnStart;
            if (_visible)
            {
                StartRecorders();
            }
        }

        private void StartRecorders()
        {
            if (_recordersStarted)
            {
                return;
            }
            _mainThreadRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal,
                "Main Thread",
                15);
            _drawCallsRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Render,
                "Draw Calls Count");
            _trianglesRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Render,
                "Triangles Count");
            _recordersStarted = true;
        }

        private void Update()
        {
            float unscaledDelta = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            _smoothedDelta += (unscaledDelta - _smoothedDelta) * 0.08f;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f3Key.wasPressedThisFrame)
                {
                    _visible = !_visible;
                    if (_visible)
                    {
                        StartRecorders();
                    }
                    else if (!_recording)
                    {
                        StopRecorders();
                    }
                }
                if (keyboard.f4Key.wasPressedThisFrame)
                {
                    if (_recording)
                    {
                        FinishBaseline();
                    }
                    else
                    {
                        StartBaseline();
                    }
                }
            }

            if (_recording)
            {
                RecordBaselineFrame(unscaledDelta);
            }

            if ((_visible || _recording) && Time.unscaledTime >= _nextReferenceResolve)
            {
                _nextReferenceResolve = Time.unscaledTime + 2f;
                ResolveReferences();
            }
            if ((_visible || _recording) && Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + refreshInterval;
                RefreshHudText();
            }
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }

            _panelStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 12,
                padding = new RectOffset(10, 10, 8, 8),
                normal = { textColor = new Color(0.86f, 0.91f, 0.95f) }
            };
            GUI.Box(new Rect(10f, 10f, 330f, 142f), _hudText, _panelStyle);
        }

        private void ResolveReferences()
        {
            if (_pedestrians == null)
            {
                _pedestrians = FindFirstObjectByType<PedestrianSystem>();
            }
            if (_traffic == null)
            {
                _traffic = FindFirstObjectByType<TrafficSystem>();
            }
            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerMotor>();
            }
            if (_vehicleSeat == null)
            {
                _vehicleSeat = FindFirstObjectByType<VehicleSeat>();
            }
            if (_debugTools == null)
            {
                _debugTools = FindFirstObjectByType<DebugTools>();
            }
        }

        private void RefreshHudText()
        {
            float fps = 1f / Mathf.Max(0.0001f, _smoothedDelta);
            float frameMilliseconds = _smoothedDelta * 1000f;
            string mainThread = _mainThreadRecorder.Valid && _mainThreadRecorder.Count > 0
                ? $"{_mainThreadRecorder.LastValue / 1000000f:0.0} ms"
                : "n/a";
            string draws = CounterText(_drawCallsRecorder);
            string triangles = _trianglesRecorder.Valid && _trianglesRecorder.Count > 0
                ? CompactCount(_trianglesRecorder.LastValue)
                : "n/a";
            float allocated = Profiler.GetTotalAllocatedMemoryLong() / Megabyte;
            float reserved = Profiler.GetTotalReservedMemoryLong() / Megabyte;
            int pedestrianCount = _pedestrians != null ? _pedestrians.ActiveCount : 0;
            int trafficCount = _traffic != null ? _traffic.ActiveCount : 0;
            Vector3 position = _player != null ? _player.transform.position : Vector3.zero;
            string mode = _debugTools != null && _debugTools.IsFlying
                ? "fly"
                : _vehicleSeat != null && _vehicleSeat.IsOccupied
                    ? "drive"
                    : _player != null && _player.IsWading ? "wade" : "walk";
            string baseline = _recording
                ? $"record {_baselineDuration:0.0}/{BaselineDuration:0}s"
                : string.IsNullOrEmpty(_lastBaselinePath) ? "idle" : "saved";

            _hudText =
                $"{fps:0} fps   {frameMilliseconds:0.0} ms   main {mainThread}\n"
                + $"draws {draws}   triangles {triangles}\n"
                + $"memory {allocated:0.0} / {reserved:0.0} MB alloc/reserved\n"
                + $"pedestrians {pedestrianCount}   traffic {trafficCount}\n"
                + $"{mode}   pos {position.x:0.0}, {position.y:0.0}, {position.z:0.0}\n"
                + $"baseline {baseline}";
        }

        private void StartBaseline()
        {
            StartRecorders();
            ResolveReferences();
            _recording = true;
            _baselineStartedAt = Time.unscaledTime;
            _baselineDuration = 0f;
            _baselineFrameCount = 0;
            _peakDrawCalls = 0;
            _peakTriangles = 0;
            _peakAllocatedMemory = 0;
            _peakReservedMemory = 0;
            _lastBaselinePath = string.Empty;
        }

        private void RecordBaselineFrame(float unscaledDelta)
        {
            if (_baselineFrameCount < _baselineFrameTimes.Length)
            {
                _baselineFrameTimes[_baselineFrameCount++] = unscaledDelta * 1000f;
            }
            _baselineDuration = Time.unscaledTime - _baselineStartedAt;
            if (_drawCallsRecorder.Valid && _drawCallsRecorder.Count > 0)
            {
                _peakDrawCalls = Math.Max(_peakDrawCalls, _drawCallsRecorder.LastValue);
            }
            if (_trianglesRecorder.Valid && _trianglesRecorder.Count > 0)
            {
                _peakTriangles = Math.Max(_peakTriangles, _trianglesRecorder.LastValue);
            }
            _peakAllocatedMemory = Math.Max(
                _peakAllocatedMemory,
                Profiler.GetTotalAllocatedMemoryLong());
            _peakReservedMemory = Math.Max(
                _peakReservedMemory,
                Profiler.GetTotalReservedMemoryLong());

            if (_baselineDuration >= BaselineDuration
                || _baselineFrameCount >= _baselineFrameTimes.Length)
            {
                FinishBaseline();
            }
        }

        private void FinishBaseline()
        {
            if (!_recording)
            {
                return;
            }
            _recording = false;
            if (_baselineFrameCount == 0)
            {
                return;
            }

            float sum = 0f;
            for (int i = 0; i < _baselineFrameCount; i++)
            {
                sum += _baselineFrameTimes[i];
            }
            Array.Sort(_baselineFrameTimes, 0, _baselineFrameCount);
            float averageFrameTime = sum / _baselineFrameCount;

            BaselineDump dump = new BaselineDump
            {
                capturedAtUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                operatingSystem = SystemInfo.operatingSystem,
                deviceModel = SystemInfo.deviceModel,
                processor = SystemInfo.processorType,
                graphicsDevice = SystemInfo.graphicsDeviceName,
                systemMemoryMb = SystemInfo.systemMemorySize,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                frameCount = _baselineFrameCount,
                durationSeconds = _baselineDuration,
                averageFps = averageFrameTime > 0f ? 1000f / averageFrameTime : 0f,
                onePercentLowFps = FpsFromPercentile(0.99f),
                p50FrameTimeMs = Percentile(0.5f),
                p95FrameTimeMs = Percentile(0.95f),
                p99FrameTimeMs = Percentile(0.99f),
                worstFrameTimeMs = _baselineFrameTimes[_baselineFrameCount - 1],
                peakDrawCalls = _peakDrawCalls,
                peakTriangles = _peakTriangles,
                peakAllocatedMemoryMb = _peakAllocatedMemory / Megabyte,
                peakReservedMemoryMb = _peakReservedMemory / Megabyte,
                activePedestrians = _pedestrians != null ? _pedestrians.ActiveCount : 0,
                activeTrafficVehicles = _traffic != null ? _traffic.ActiveCount : 0,
                measurementNotes =
                    "Profiler counters may be unavailable in non-development builds; "
                    + "memory is Unity allocated/reserved memory, not total process RSS."
            };

            try
            {
                string directory = Path.Combine(
                    Application.persistentDataPath,
                    "PerformanceBaselines");
                Directory.CreateDirectory(directory);
                _lastBaselinePath = Path.Combine(
                    directory,
                    $"baseline-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
                File.WriteAllText(_lastBaselinePath, JsonUtility.ToJson(dump, true));
                Debug.Log($"Performance baseline saved to {_lastBaselinePath}", this);
            }
            catch (Exception exception)
            {
                _lastBaselinePath = string.Empty;
                Debug.LogError($"Performance baseline could not be saved: {exception.Message}", this);
            }

            if (!_visible)
            {
                StopRecorders();
            }
        }

        private float Percentile(float fraction)
        {
            int index = Mathf.Clamp(
                Mathf.CeilToInt(_baselineFrameCount * fraction) - 1,
                0,
                _baselineFrameCount - 1);
            return _baselineFrameTimes[index];
        }

        private float FpsFromPercentile(float fraction)
        {
            float frameTime = Percentile(fraction);
            return frameTime > 0f ? 1000f / frameTime : 0f;
        }

        private static string CounterText(ProfilerRecorder recorder)
        {
            return recorder.Valid && recorder.Count > 0
                ? recorder.LastValue.ToString()
                : "n/a";
        }

        private static string CompactCount(long value)
        {
            if (value >= 1000000)
            {
                return $"{value / 1000000f:0.0}m";
            }
            if (value >= 1000)
            {
                return $"{value / 1000f:0}k";
            }
            return value.ToString();
        }

        private void StopRecorders()
        {
            if (_mainThreadRecorder.Valid)
            {
                _mainThreadRecorder.Dispose();
            }
            if (_drawCallsRecorder.Valid)
            {
                _drawCallsRecorder.Dispose();
            }
            if (_trianglesRecorder.Valid)
            {
                _trianglesRecorder.Dispose();
            }
            _mainThreadRecorder = default;
            _drawCallsRecorder = default;
            _trianglesRecorder = default;
            _recordersStarted = false;
        }

        private void OnDisable()
        {
            StopRecorders();
            _recording = false;
        }

        [Serializable]
        private sealed class BaselineDump
        {
            public string capturedAtUtc;
            public string unityVersion;
            public string platform;
            public string operatingSystem;
            public string deviceModel;
            public string processor;
            public string graphicsDevice;
            public int systemMemoryMb;
            public int screenWidth;
            public int screenHeight;
            public int frameCount;
            public float durationSeconds;
            public float averageFps;
            public float onePercentLowFps;
            public float p50FrameTimeMs;
            public float p95FrameTimeMs;
            public float p99FrameTimeMs;
            public float worstFrameTimeMs;
            public long peakDrawCalls;
            public long peakTriangles;
            public float peakAllocatedMemoryMb;
            public float peakReservedMemoryMb;
            public int activePedestrians;
            public int activeTrafficVehicles;
            public string measurementNotes;
        }
    }
}
