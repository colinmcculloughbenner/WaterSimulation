using UnityEngine;
using Utilities;

namespace WaveSimulation
{
    [RequireComponent(typeof(BasicWaveSimulator))]
    // A simple way to test the basic wave simulator by directly visualizing the heightmap it produces.
    public class BasicWaveSimulatorVisualTester : MonoBehaviour, IDisplayBasicWaveSimulation
    {
        // Material to assign to mesh renderer of each cell
        [SerializeField] Material _cellMaterial;
        
        // Grid dimensions in cells
        [SerializeField] int _gridHeight = 50;
        int _gridWidth;
        
        // Grid dimensions in units of Unity editor
        static float _heightInUnits;
        static float _widthInUnits;

        float _cellSizeInUnits;
        Vector3 _originPosition;

        Transform _parentOfCells; // Object to which all cells are childed
        MeshRenderer[] _cells;

        Camera _camera;
        BasicWaveSimulator _simulator;

        void Awake()
        {
            _camera = Camera.main;
            _heightInUnits = _camera.orthographicSize * 2f; 
            _widthInUnits = _camera.aspect * _heightInUnits;
            _simulator = GetComponent<BasicWaveSimulator>();
        }

        void Start()
        {
            SetUpGrid();            
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var mousePos = _camera.ScreenToWorldPoint(Input.mousePosition);
                var x = Mathf.RoundToInt((mousePos.x - _originPosition.x) / _cellSizeInUnits);
                var y = Mathf.RoundToInt((mousePos.y - _originPosition.y) / _cellSizeInUnits);
                
                _simulator.SetHeightAtPoint(x, y, 1f);
                _simulator.SetHeightAtPoint(x, y + 1, 0.25f);
                _simulator.SetHeightAtPoint(x, y - 1, 0.25f);
                _simulator.SetHeightAtPoint(x + 1, y, 0.25f);
                _simulator.SetHeightAtPoint(x - 1, y, 0.25f);
            }
        }

        public void RespondToHeightmap(ref float[] heightMap)
        {
            if (heightMap.Length != _cells.Length)
            {
                Debug.LogWarning(
                    "Number of cells in Basic Wave Simulator grid doesn't match number of quads displayed by Visual Tester.");
                return;
            }

            for (var i = 0; i < _cells.Length; ++i)
            {
                _cells[i].material.color = (heightMap[i] * 0.5f + 0.5f) * Color.cyan;
            }
        }

        void SetUpGrid()
        {
            if (_parentOfCells != null)
            {
                Destroy(_parentOfCells);
            }

            _parentOfCells = new GameObject().transform;
            
            _cellSizeInUnits = _heightInUnits / (_gridHeight + 2);
            _gridWidth = Mathf.RoundToInt(_widthInUnits / _cellSizeInUnits);
            _originPosition = _camera.transform.position - new Vector3(0.5f * _cellSizeInUnits * _gridWidth, 0.5f * _cellSizeInUnits * _gridHeight, -1f);
            var gridSize = (_gridHeight + 2) * (_gridWidth + 2);
            _cells = new MeshRenderer[gridSize];
            
            for (var i = 0; i < gridSize; ++i)
            {
                var newCell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                var cellCoordinates = GridUtilities.GridCoordinatesFromIndex(i, _gridWidth + 1);
                newCell.transform.position = _originPosition + (Vector3)((Vector2)cellCoordinates * _cellSizeInUnits);
                newCell.transform.localScale = new Vector3(_cellSizeInUnits, _cellSizeInUnits);
                newCell.transform.parent = _parentOfCells;
                var newCellRenderer = newCell.GetComponent<MeshRenderer>(); 
                newCellRenderer.material = _cellMaterial;
                _cells[i] = newCellRenderer;
            }
            
            _simulator.InitializeGrid(_gridWidth, _gridHeight);
        }
    }
}