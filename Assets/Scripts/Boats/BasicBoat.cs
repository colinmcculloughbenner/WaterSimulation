using UnityEngine;
using WaveSimulation;

namespace Boats
{
    // Updates how the boat is rendered, including its shader properties, based on simulation data and its position in
    // the grid.
    public class BasicBoat : MonoBehaviour
    {
        [SerializeField] Vector2 _gridPosition = Vector2.zero;
        [SerializeField] float _baseScale = 1f;
        [SerializeField] float _baseWidth = 4f;
        [SerializeField] float _rockingDampFactor = 0.5f;
        [SerializeField] float _xSpeed = 5f;
        [SerializeField] float _ySpeed = 1f;

        [SerializeField] Vector2 _spriteOffset;
        [SerializeField] SpriteRenderer _heightmapTester;

        WaterGrid _grid;
        Transform _transform;
        BuoyancyPhysics _buoyancyPhysics;
        SpriteRenderer _renderer;
        BasicWaveSimulator _simulator;
        
        // Cached shader property hashes to avoid inefficient string lookup
        static readonly int GridXHash = Shader.PropertyToID("_GridX");
        static readonly int GridYHash = Shader.PropertyToID("_GridY");
        static readonly int HeightmapHash = Shader.PropertyToID("_Heightmap");

        public Vector2 GridPosition => _gridPosition;

        void Awake()
        {
            _grid = FindObjectOfType<WaterGrid>();
            FindObjectOfType<Water>();
            _transform = transform;
            _grid.WidthFromWorldToGridUnitsBottom(_baseWidth);
            _buoyancyPhysics = transform.parent.GetComponentInChildren<BuoyancyPhysics>();
            _renderer = GetComponent<SpriteRenderer>();
            _simulator = FindObjectOfType<BasicWaveSimulator>();
        }

        void Start()
        {
            _renderer.material.SetFloat("_MaxX", _grid.GridWidth + 1);
            _renderer.material.SetFloat("_MaxY", _grid.GridHeight + 1);
        }

        void Update()
        {
            // Move the boat with WASD or arrow keys
            _gridPosition.x += Input.GetAxis("Horizontal") * Time.deltaTime * _xSpeed;
            _gridPosition.y += Input.GetAxis("Vertical") * Time.deltaTime * _ySpeed;

            // Scale the boat based on its position
            var initialWorldPosition = _grid.GridToWorld(_gridPosition);
            var scaleFactor = Mathf.Lerp(_grid.BottomToTopScaleFactor, 1f,
                _grid.NormalizedHorizonDistance(initialWorldPosition.y));
            var scale = _baseScale * scaleFactor;
            _transform.localScale = new Vector3(scale, scale, scale);

            // Rotate the sprite to match its Rigidbody
            _transform.rotation = _buoyancyPhysics.Rotation;
            
            // Move the sprite based on movement of its Rigidbody, taking into account the scaling calculated above.
            var buoyancyEffect = scaleFactor * _buoyancyPhysics.Position;
            _transform.position = (Vector3)(initialWorldPosition + buoyancyEffect + _spriteOffset * scaleFactor) - Vector3.forward;

            // Update the sprite's shader properties.
            // The shader needs these to render water over the underwater parts of the boat.
            _renderer.material.SetFloat(GridXHash, _gridPosition.x);
            _renderer.material.SetFloat(GridYHash, _gridPosition.y);
            _renderer.material.SetTexture(HeightmapHash, _simulator.HeightMap);
            _heightmapTester.material.SetTexture(HeightmapHash, _simulator.HeightMap);
        }
    }
}