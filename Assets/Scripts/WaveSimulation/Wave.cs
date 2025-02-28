using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utilities;

namespace WaveSimulation
{
    [RequireComponent(typeof(LineRenderer))]
    // Draws waves using a Catmull-Rom spline defined by points read from the wave simulation's heightmap.
    public class Wave : MonoBehaviour
    {
        [Tooltip("Number of points used between the wave spline's control points")]
        [SerializeField]
        uint _pointsPerSegment = 10;
        [Tooltip("Controls the type of Catmull-Rom spline used. 0 for uniform, 0.5 for centripetal, 1 for chordal.")]
        [SerializeField, Range(0, 1)]
        float _smoothingParameter = 0.5f;

        LineRenderer _lineRenderer;
        Water _water;
        BasicWaveSimulator _simulator;
        
        // Maximum wave height
        float _maxHeight;
        
        // Positions drawn by line renderer if the height map is 0.5 at all points
        Vector3[] _standardPositions;
        // Positions actually drawn by line renderer
        Vector3[] _positions;
        // Number of control points in the Catmull-Rom spline used to calculate _positions
        int _numberOfControlPoints;
        
        // Cached hash of shader's _Heightmap property to avoid inefficient string lookup
        // This _Heightmap property is updated every frame to reflect the updated simulation
        static readonly int Heightmap = Shader.PropertyToID("_Heightmap");

        void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _positions = Array.Empty<Vector3>();
            _water = FindObjectOfType<Water>();
            _simulator = FindObjectOfType<BasicWaveSimulator>();
        }
        
        public void Initialize(IList<Vector3> points, Material material, float height, float yScaleFactor, Color color)
        {
            _standardPositions = points.ToArray();
            _numberOfControlPoints = points.Count();

            _lineRenderer.materials = new[] {material};
            _lineRenderer.startColor = _lineRenderer.endColor = color;
            _maxHeight = height;
            
            // Initialize points drawn by line renderer
            CatmullRom.CalculateChain(_standardPositions, _pointsPerSegment, _smoothingParameter, ref _positions);
            _lineRenderer.positionCount = _positions.Length;
            _lineRenderer.SetPositions(_positions);
        }

        // Update line renderer points based on heightmap, and update shader heightmap
        public void RespondToHeightMap(ref float[] heightMap, ref WaterGrid waterGrid)
        {
            // Update points drawn by line renderer based on new simulation data
            var updatedControlPoints = new Vector3[_numberOfControlPoints];
            for (var i = 0; i < _numberOfControlPoints; ++i)
            {
                var gridPosition = waterGrid.WorldToGrid(_standardPositions[i]);
                var height = _water.HeightAt(gridPosition);
                var scaledHeight = _maxHeight * (height - 0.5f) * waterGrid.CellWidthAtWorldHeight(_standardPositions[i].y);
                updatedControlPoints[i] = _standardPositions[i] + new Vector3(0, scaledHeight, 0);
            }
            
            CatmullRom.CalculateChain(updatedControlPoints, _pointsPerSegment, _smoothingParameter, ref _positions);
            _lineRenderer.SetPositions(_positions);
            
            // Update shader's heightmap with new simulation data
            _lineRenderer.material.SetTexture(Heightmap, _simulator.HeightMap);
        }
    }
}