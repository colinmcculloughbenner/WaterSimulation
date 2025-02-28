using UnityEngine;
using Utilities;

namespace WaveSimulation
{
    public class WaterGrid : MonoBehaviour
    {
        [SerializeField] int _gridWidth = 20;
        [SerializeField] int _gridHeight = 10;
        
        [Tooltip("Render the grid in Scene view while playing.")]
        [SerializeField] bool _renderGrid;
        
        [Tooltip("Vertical position of the top of the grid")]
        [SerializeField] float _topY;
        [Tooltip("Vertical position of the bottom of the grid")]
        [SerializeField] float _bottomY;
        [Tooltip("Horizontal position of the center of the grid")]
        [SerializeField] float _centerX;
        [Tooltip("Total width of the grid in world units")]
        [SerializeField] float _widthInUnits;
        
        [Tooltip("Cell width at the bottom of the grid is cell width at the top of the grid plus _perspectiveFactor.")]
        [SerializeField] float _perspectiveFactor;

        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;

        public float BottomToTopScaleFactor => CellWidthAtWorldHeight(_topY) / CellWidthAtWorldHeight(_bottomY);

        void Update()
        {
            if (_renderGrid) RenderGrid();
        }

        public Vector2 WorldToGrid(Vector2 worldPosition)
        {
            var gridX = 0.5f * (_gridWidth + 1) + (worldPosition.x - _centerX) / CellWidthAtWorldHeight(worldPosition.y);
            var gridY = Mathf.Lerp(_gridHeight, 0,
                Mathf.Sqrt(NormalizedHorizonDistance(worldPosition.y)));
            return new Vector2(gridX, gridY);
        }

        public Vector2 GridToWorld(Vector2 gridPosition)
        {
            var worldY = _topY - (_topY - _bottomY) * MathUtilities.Square(1 - gridPosition.y / _gridHeight);
            var worldX = CellWidthAtWorldHeight(worldY) * (gridPosition.x - 0.5f * (_gridWidth + 1));
            return new Vector2(worldX, worldY);
        }

        public Vector2 GridToWorld(float x, float y) => GridToWorld(new Vector2(x, y));

        public float WidthFromWorldToGridUnitsBottom(float width) => width / CellWidthAtWorldHeight(_bottomY);

        public float CellWidthAtWorldHeight(float y)
        {
            var cellWidthTop = _widthInUnits / _gridWidth;
            return cellWidthTop + _perspectiveFactor * NormalizedHorizonDistance(y);
        }

        public float NormalizedHorizonDistance(float y) => (_topY - y) / (_topY - _bottomY);

        void RenderGrid()
        {
            // Ensures visibility of grid
            var offset = new Vector3(0, 0, -1);
            
            for (var i = 1; i < _gridWidth + 1; ++i)
            {
                Vector3 start = GridToWorld(i, 0);
                Vector3 end = GridToWorld(i, _gridHeight);
                Debug.DrawLine(start + offset, end + offset, Color.white);
            }

            for (var j = 1; j < _gridHeight + 1; ++j)
            {
                Vector3 start = GridToWorld(0, j);
                Vector3 end = GridToWorld(_gridWidth, j);
                Debug.DrawLine(start + offset, end + offset, Color.white);
            }
        }
    }
}