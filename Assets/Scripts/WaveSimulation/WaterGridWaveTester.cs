using UnityEngine;

namespace WaveSimulation
{
    [RequireComponent(typeof(WaterGrid))]
    [RequireComponent(typeof(BasicWaveSimulator))]
    // Allows waves to be created with mouse clicks for testing. Pressing space bar creates a small wave in a random location.
    public class WaterGridWaveTester : MonoBehaviour
    {
        WaterGrid _waterGrid;
        Camera _camera;
        BasicWaveSimulator _simulator;

        void Awake()
        {
            _waterGrid = GetComponent<WaterGrid>();
            _camera = Camera.main;
            _simulator = GetComponent<BasicWaveSimulator>();
        }

        void Start()
        {
            _simulator.InitializeGrid(_waterGrid.GridWidth, _waterGrid.GridHeight);
        }

        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                var gridPoint = _waterGrid.WorldToGrid(_camera.ScreenToWorldPoint(Input.mousePosition));

                var x = Mathf.RoundToInt(gridPoint.x);
                var y = Mathf.RoundToInt(gridPoint.y);
            
                _simulator.SetHeightAtPoint(x, y, 1f);
                _simulator.SetHeightAtPoint(x, y + 1, 0.25f);
                _simulator.SetHeightAtPoint(x, y - 1, 0.25f);
                _simulator.SetHeightAtPoint(x + 1, y, 0.25f);
                _simulator.SetHeightAtPoint(x - 1, y, 0.25f);
            }
        }
    }
}
