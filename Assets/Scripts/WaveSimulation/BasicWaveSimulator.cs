using UnityEngine;
using Utilities;

namespace WaveSimulation
{
    [RequireComponent(typeof(IDisplayBasicWaveSimulation))]
    // Runs a simple fluid simulation based on a cellular automaton. This generalizes the approach presented in
    // https://web.archive.org/web/20160607052007/http://freespace.virgin.net:80/hugo.elias/graphics/x_water.htm
    public class BasicWaveSimulator : MonoBehaviour
    {
        [Tooltip("Number of simulation grid columns")] [SerializeField]
        int _gridWidth = 200;

        [Tooltip("Number of simulation grid rows")] [SerializeField]
        int _gridDepth = 100;

        [Tooltip("At values higher than 1, wave propagation is artificially encouraged.")] [SerializeField, Range(1, 4)]
        float _propagationFactor = 2f;

        [Tooltip(
            "At 0, there is no oscillation and so no waves. At higher values, there is a greater degree of oscillation.")]
        [SerializeField, Range(0, 4)]
        float _velocityFactor = 1f;

        [Tooltip("At 0, waves don't propagate at all. At 1, they are undamped.")] [SerializeField, Range(0, 1)]
        float _dampingFactor = 0.9f;

        [Tooltip("Period of waves in seconds. An artifact of the simulation is that all waves have the same period.")]
        [SerializeField]
        float _wavePeriod = 1f;

        [Tooltip(
            "If true, waves reaching the edge of the simulation grid wrap around to the other side. If false, they bounce back from the edges.")]
        [SerializeField]
        bool _wrap = true;

        IDisplayBasicWaveSimulation _display;

        // The current value of a cell is determined by its neighbors' values in the previous buffer (_previousBuffer1)
        // and its value in the buffer before the previous buffer (_previousBuffer2). This could be done with two
        // buffers by writing the new values to the buffer holding the values from two frames before, but three buffers
        // are used here for better readability.
        float[] _currentBuffer;
        float[] _previousBuffer1;
        float[] _previousBuffer2;

        // The wave simulation updates less frequently than the frame rate. _animationBuffer holds values lerped between
        // _currentBuffer and _previousBuffer1 for the purpose of smooth animation.
        float[] _animationBuffer;

        // Time at which the next simulation step should be calculated.
        float _nextUpdateTime;

        // Animation buffer data is written here to be rendered by an IDisplayBasicWaveSimulator.
        Texture2D _heightMap;

        bool _hasInitialized;

        public Texture2D HeightMap => _heightMap;

        void Awake()
        {
            _display = GetComponent<IDisplayBasicWaveSimulation>();
        }

        void Update()
        {
            if (!_hasInitialized) return;

            var currentTime = Time.unscaledTime;

            // If it's not time to update, interpolate between the last two values for smooth animation.
            if (currentTime < _nextUpdateTime)
            {
                var timeLeft = _nextUpdateTime - currentTime;
                var normalizedTimeLeft = timeLeft / (0.25f * _wavePeriod);

                for (var i = 0; i < _animationBuffer.Length; ++i)
                {
                    _animationBuffer[i] = Mathf.Lerp(_currentBuffer[i], _previousBuffer1[i], normalizedTimeLeft);
                }

                _display.RespondToHeightmap(ref _animationBuffer);
                UpdateHeightMap(ref _animationBuffer);

                return;
            }

            // If it's time to update, first free up a new buffer to put new values into.
            SwapHeightBuffers();

            for (var i = 1; i <= _gridWidth; ++i)
            {
                for (var j = 1; j <= _gridDepth; ++j)
                {
                    // Each cell's new value is the sum of:
                    // 1. The mean of the previous values of its neighbors (which we can think of as the previous value
                    // with a smoothing function applied) multiplied by a propagation factor and
                    // 2. a negative constant times its value from two frames previously (which we can think of as its target vertical
                    // velocity (per frame) for the current frame, if waves have a period of four simulation frames).
                    // This sum is multiplied by a damping factor so the waves lose energy over time.
                    // The smoothing in (1) ensures that waves propagate from their source. Propagation factor values
                    // above 1 encourage further propagation of waves.
                    // (2) ensures that the waves oscillate.
                    CurrentBuffer(i, j) =
                        (0.25f * _propagationFactor * (PreviousBuffer(i - 1, j) + PreviousBuffer(i + 1, j) +
                                                       PreviousBuffer(i, j - 1) + PreviousBuffer(i, j + 1)) -
                         _velocityFactor * PreviousBuffer2(i, j)) * _dampingFactor;
                }
            }

            HandleEdges();

            // Update with the previous buffer so that we can lerp between previous and current buffer while waiting for
            // the next simulation step.
            _display.RespondToHeightmap(ref _previousBuffer1);
            UpdateHeightMap(ref _previousBuffer1);

            // Waves have a period of 4 simulation frames, so the simulation should be updated after 0.25 * _wavePeriod
            _nextUpdateTime = currentTime + 0.25f * _wavePeriod;
        }

        public void InitializeGrid(int width, int height)
        {
            _gridDepth = height;
            _gridWidth = width;

            var arraySize = (_gridDepth + 2) * (_gridWidth + 2);

            _currentBuffer = new float[arraySize];
            _previousBuffer1 = new float[arraySize];
            _previousBuffer2 = new float[arraySize];
            _animationBuffer = new float[arraySize];

            _heightMap = new Texture2D(_gridWidth + 2, _gridDepth + 2);

            _hasInitialized = true;
        }

        void UpdateHeightMap(ref float[] buffer)
        {
            for (var i = 0; i < buffer.Length; ++i)
            {
                var coordinate = GridUtilities.GridCoordinatesFromIndex(i, _gridWidth + 1);

                var colorValue = Mathf.Clamp01(0.5f + 0.5f * buffer[i]);

                _heightMap.SetPixel(coordinate.x, coordinate.y, new Color(colorValue, colorValue, colorValue, 1f));
            }

            _heightMap.Apply();
        }

        public void SetHeightAtPoint(int x, int y, float height) => CurrentBuffer(x, y) = height;

        void SwapHeightBuffers()
        {
            var temp = _previousBuffer2;
            _previousBuffer2 = _previousBuffer1;
            _previousBuffer1 = _currentBuffer;
            _currentBuffer = temp;
        }

        void HandleEdges()
        {
            // If wrapping is turned off, edges don't need to be treated specially.
            if (!_wrap) return;

            for (var i = 0; i < _gridWidth + 2; ++i)
            {
                CurrentBuffer(i, 0) = CurrentBuffer(i, _gridDepth);
                CurrentBuffer(i, _gridDepth + 1) = CurrentBuffer(i, 1);
            }

            for (var j = 1; j <= _gridDepth; ++j)
            {
                CurrentBuffer(0, j) = CurrentBuffer(_gridWidth, j);
                CurrentBuffer(_gridWidth + 1, j) = CurrentBuffer(1, j);
            }
        }

        ref float CurrentBuffer(int x, int y)
        {
            return ref _currentBuffer[GridUtilities.GetIndex(x, y, _gridWidth + 1)];
        }

        ref float PreviousBuffer(int x, int y)
        {
            return ref _previousBuffer1[GridUtilities.GetIndex(x, y, _gridWidth + 1)];
        }

        ref float PreviousBuffer2(int x, int y)
        {
            return ref _previousBuffer2[GridUtilities.GetIndex(x, y, _gridWidth + 1)];
        }
    }
}