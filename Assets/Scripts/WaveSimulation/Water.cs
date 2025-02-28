using UnityEngine;
using Utilities;

namespace WaveSimulation
{
    [RequireComponent(typeof(WaterGrid))]
    // Creates wave objects to visualize the water simulation and updates them with simulation data.
    public class Water : MonoBehaviour, IDisplayBasicWaveSimulation
    {
        [SerializeField] float _horizonHeight;
        [SerializeField] Material[] _waveMaterials;
        [SerializeField] GameObject _wavePrefab;
        [SerializeField] int _numberOfWaves = 10;
        [SerializeField] float _waveHeight = 0.1f;
        [SerializeField, Range(0, 1)] float _waveHeightYScaleFactor = 1f;
        [SerializeField] Color _waveColorTop = Color.cyan;
        [SerializeField] Color _waveColorBottom = Color.blue;

        Transform _wavesParent;
        Wave[] _waves;
        WaterGrid _waterGrid;

        float[] _heightMap;

        void Awake()
        {
            _waterGrid = GetComponent<WaterGrid>();
        }

        void Start()
        {
            GenerateWaves();
        }

        public void RespondToHeightmap(ref float[] heightMap)
        {
            _heightMap = heightMap;

            foreach (var wave in _waves)
            {
                wave.RespondToHeightMap(ref _heightMap, ref _waterGrid);
            }
        }

        float HeightAt(float x, float y)
        {
            if (_heightMap == null || _heightMap.Length == 0)
                return 0f;

            var xInt = Mathf.RoundToInt(x);
            var xFrac = x - xInt;
            var yInt = Mathf.RoundToInt(y);
            var yFrac = y - yInt;

            if (Mathf.Approximately(xFrac, 0f) && Mathf.Approximately(yFrac, 0f))
            {
                return HeightAt(xInt, yInt);
            }

            var interpolatedHeight = Mathf.Lerp(
                Mathf.Lerp(
                    _heightMap[GridUtilities.GetIndex(xInt, yInt, _waterGrid.GridWidth + 1)],
                    _heightMap[GridUtilities.GetIndex(xInt + 1, yInt, _waterGrid.GridWidth + 1)],
                    xFrac
                ),
                Mathf.Lerp(
                    _heightMap[GridUtilities.GetIndex(xInt, yInt + 1, _waterGrid.GridWidth + 1)],
                    _heightMap[GridUtilities.GetIndex(xInt + 1, yInt + 1, _waterGrid.GridWidth + 1)],
                    xFrac
                ),
                yFrac
            );

            return interpolatedHeight;
        }

        public float HeightAt(Vector2 gridPosition) => HeightAt(gridPosition.x, gridPosition.y);
        float HeightAt(int x, int y) => _heightMap[GridUtilities.GetIndex(x, y, _waterGrid.GridWidth + 1)];

        void GenerateWaves()
        {
            _numberOfWaves = _waterGrid.GridHeight;
            InitializeWaves();

            var gridWidth = _waterGrid.GridWidth;

            for (var y = 1; y <= _waves.Length; ++y)
            {
                var points = new Vector3[gridWidth + 2];
                for (var x = 0; x < gridWidth + 2; ++x)
                {
                    points[x] = _waterGrid.GridToWorld(x, y);
                }

                var color = Color.Lerp(_waveColorTop, _waveColorBottom,
                    _waterGrid.NormalizedHorizonDistance(points[0].y));
                var newObject = Instantiate(_wavePrefab, Vector3.zero, Quaternion.identity, _wavesParent);
                var wave = newObject.GetComponent<Wave>();
                wave.Initialize(points, _waveMaterials[Random.Range(0, _waveMaterials.Length)],
                    _waveHeight, _waveHeightYScaleFactor, color);
                _waves[y - 1] = wave;
            }
        }

        void InitializeWaves()
        {
            if (_wavesParent != null)
                Destroy(_wavesParent.gameObject);

            _wavesParent = new GameObject().transform;
            _wavesParent.SetParent(transform);
            _waves = new Wave[_numberOfWaves];
        }
    }
}