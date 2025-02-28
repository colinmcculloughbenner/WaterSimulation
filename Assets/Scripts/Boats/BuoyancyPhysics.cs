using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;
using WaveSimulation;

namespace Boats
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    // Applies buoyancy forces and adjusts Rigidbody2D drag and angular drag parameters based on how much of the
    // BoxCollider2D is underwater. 
    public class BuoyancyPhysics : MonoBehaviour
    {
        [SerializeField] float _buoyancy;
        [FormerlySerializedAs("_drag")] [SerializeField]
        float _linearDrag;
        [SerializeField] float _linearDragAir;
        [SerializeField] float _angularDragAir;
        [SerializeField] float _angularDrag;
        [SerializeField] float _lateralForceFactor = 1f;

        Rigidbody2D _rigidbody;
        BoxCollider2D _collider;
        Water _water;
        BasicBoat _boat;

        Vector2 _gridPosition;

        public Vector2 Position => _rigidbody.position;
        public Quaternion Rotation => Quaternion.Euler(0,0,_rigidbody.rotation);

        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();
            _water = FindObjectOfType<Water>();
            _boat = transform.parent.GetComponentInChildren<BasicBoat>();
        }

        void Start()
        {
            _rigidbody.position = Vector2.zero;
        }

        void FixedUpdate()
        {
            ApplyBuoyancy();
        }
        
        void ApplyBuoyancy()
        {
            // We must first calculate:
            // (1) underwater area
            // (2) the slope of the points on the collider perimeter that intersect the water surface
            // We divide this into cases based on the number of corners underwater.
            // This allows us to calculate:
            // * Buoyancy force: upward force proportional to underwater area
            // * Lateral force: horizontal force proportional to underwater area and slope of surface points
            // * Rigidbody drag and angular drag: lerped between air and underwater values based on underwater area
            
            _gridPosition = _boat.GridPosition;

            var underwaterCorners = GetUnderwaterCorners();
            var numberOfUnderwaterCorners = underwaterCorners.Count;

            // Easy cases first:

            if (numberOfUnderwaterCorners == 0)
            {
                _rigidbody.angularDrag = _angularDragAir;
                _rigidbody.drag = _linearDragAir;
                return;
            }
            
            if (numberOfUnderwaterCorners == 4)
            {
                _rigidbody.AddForceAtPosition(_buoyancy * Vector2.up, _collider.bounds.center);
                _rigidbody.angularDrag = _angularDragAir + _angularDrag;
                _rigidbody.drag = _linearDragAir + _linearDrag;
                return;
            }
            
            // Harder cases:

            var rotationAngleRad = MathUtilities.NormalizedAngleDegrees(_collider.attachedRigidbody.rotation) *
                                   Mathf.Deg2Rad;
            var colliderSize = _collider.size;
            var colliderArea = colliderSize.x * colliderSize.y;

            var underwaterArea = 0f;
            Vector2 underwaterCenter = default;
            
            float slopeAtSurfacePoints = 0; // Used to calculate horizontal force
            
            if (numberOfUnderwaterCorners == 1)
            {
                var corner = underwaterCorners[0];
                var cornerDepth = corner.WaterHeight - corner.Position.y;
                
                var offset1 = -1f * cornerDepth * Mathf.Tan(rotationAngleRad);
                var offset2 = cornerDepth / Mathf.Tan(rotationAngleRad);

                var surfacePoint1 = new Vector2(corner.Position.x + offset1, corner.WaterHeight);
                var surfacePoint2 = new Vector2(corner.Position.x + offset2, corner.WaterHeight);

                underwaterArea = MathUtilities.TriangleArea(corner.Position, surfacePoint1, surfacePoint2);
                underwaterCenter = (corner.Position + surfacePoint1 + surfacePoint2) / 3f;
                
                slopeAtSurfacePoints = (surfacePoint1.y - surfacePoint2.y) / (surfacePoint1.x - surfacePoint2.x);
            }

            if (numberOfUnderwaterCorners == 2)
            {
                var corner1 = underwaterCorners[0];
                var corner2 = underwaterCorners[1];
                
                var corner1Depth = corner1.WaterHeight - corner1.Position.y;
                var corner2Depth = corner2.WaterHeight - corner2.Position.y;

                var corner1Offset = corner1Depth * Mathf.Tan(rotationAngleRad);
                var corner2Offset = corner2Depth * Mathf.Tan(rotationAngleRad);

                var surfacePoint1 =
                    new Vector2(corner1.Position.x - corner1Offset, corner1.WaterHeight);
                var surfacePoint2 =
                    new Vector2(corner2.Position.x - corner2Offset, corner2.WaterHeight);

                var triangle1Area =
                    MathUtilities.TriangleArea(surfacePoint1, surfacePoint2, corner1.Position);
                var triangle1Center = (surfacePoint1 + surfacePoint2 + corner1.Position) / 3f;
                
                var triangle2Area = MathUtilities.TriangleArea(corner2.Position, surfacePoint2, corner1.Position);
                var triangle2Center = (corner2.Position + surfacePoint2 + corner1.Position) / 3f;

                underwaterArea = triangle1Area + triangle2Area;
                underwaterCenter = (triangle1Center * triangle1Area + triangle2Center * triangle2Area) / underwaterArea;
                
                slopeAtSurfacePoints = (surfacePoint1.y - surfacePoint2.y) / (surfacePoint1.x - surfacePoint2.x);
            }

            if (numberOfUnderwaterCorners == 3)
            {
                var bottomIndex = (int)((rotationAngleRad + 180f) / 90f);

                var bottomCorner = underwaterCorners[0];
                var leftCorner = underwaterCorners[1];
                var rightCorner = underwaterCorners[2];
                
                for (var i = 0; i < 3; ++i)
                {
                    if (underwaterCorners[i].Index == bottomIndex)
                    {
                        bottomCorner = underwaterCorners[i];
                        leftCorner = underwaterCorners[(i + 1) % 3];
                        rightCorner = underwaterCorners[(i + 2) % 3];
                    }
                }

                var leftCornerDepth = leftCorner.WaterHeight - leftCorner.Position.y;
                var rightCornerDepth = rightCorner.WaterHeight - rightCorner.Position.y;

                float leftSurfacePointOffset, rightSurfacePointOffset;
                if (rotationAngleRad > 0)
                {
                    leftSurfacePointOffset =
                        Mathf.Clamp(leftCornerDepth / Mathf.Tan(rotationAngleRad), 0f, colliderSize.x);
                    rightSurfacePointOffset =
                        -1f * Mathf.Clamp(Mathf.Tan(rotationAngleRad) / rightCornerDepth, 0f, colliderSize.x);
                }
                else
                {
                    leftSurfacePointOffset =
                        -1f * Mathf.Clamp(Mathf.Tan(rotationAngleRad) / leftCornerDepth, 0f, colliderSize.x);
                    rightSurfacePointOffset = Mathf.Clamp(rightCornerDepth /  Mathf.Tan(rotationAngleRad), 0f, colliderSize.x);
                }

                var leftSurfacePoint =
                    new Vector2(leftCorner.Position.x + leftSurfacePointOffset, leftCorner.WaterHeight);
                var rightSurfacePoint = 
                    new Vector2(rightCorner.Position.x + rightSurfacePointOffset, rightCorner.WaterHeight);

                var leftTriangleArea =
                    MathUtilities.TriangleArea(bottomCorner.Position, leftCorner.Position, leftSurfacePoint);
                var leftTriangleCenter = (bottomCorner.Position + leftCorner.Position + leftSurfacePoint) / 3f;

                var middleTriangleArea =
                    MathUtilities.TriangleArea(bottomCorner.Position, leftSurfacePoint, rightSurfacePoint);
                var middleTriangleCenter = (bottomCorner.Position + leftSurfacePoint + rightSurfacePoint) / 3f;

                var rightTriangleArea =
                    MathUtilities.TriangleArea(bottomCorner.Position, rightSurfacePoint, rightCorner.Position);
                var rightTriangleCenter = (bottomCorner.Position + rightSurfacePoint + rightCorner.Position) / 3f;

                underwaterArea = leftTriangleArea + middleTriangleArea + rightTriangleArea;
                underwaterCenter =
                    (leftTriangleCenter * leftTriangleArea + middleTriangleCenter * middleTriangleArea +
                     rightTriangleCenter * rightTriangleArea) / underwaterArea;
                
                slopeAtSurfacePoints = (leftSurfacePoint.y - rightSurfacePoint.y) / (leftSurfacePoint.x - rightSurfacePoint.x);
            }
            
            // Finally calculate forces and drag values
            var buoyancyForce = Mathf.Clamp01(underwaterArea / colliderArea) * _buoyancy * Vector2.up;
            var lateralForce = -1f * Mathf.Clamp01(underwaterArea / colliderArea) * slopeAtSurfacePoints * _lateralForceFactor * Vector2.right;
            _rigidbody.AddForceAtPosition(buoyancyForce + lateralForce, underwaterCenter);
            _rigidbody.drag = _linearDragAir + _linearDrag * underwaterArea / colliderArea;
            _rigidbody.angularDrag = _angularDragAir + _angularDrag * underwaterArea / colliderArea;
        }

        Vector2[] GetColliderCornerPositions()
        {
            var bounds = _collider.bounds;
            var center = bounds.center;
            var extents = bounds.extents;
            var corners = new Vector2[4];

            corners[0] = new Vector2(center.x - extents.x, center.y + extents.y);
            corners[1] = new Vector2(center.x + extents.x, center.y + extents.y);
            corners[2] = new Vector2(center.x + extents.x, center.y - extents.y);
            corners[3] = new Vector2(center.x - extents.x, center.y - extents.y);

            return corners;
        }

        List<UnderwaterCorner> GetUnderwaterCorners()
        {
            var corners = GetColliderCornerPositions();
            var underwaterCorners = new List<UnderwaterCorner>();

            for (var i = 0; i < 4; ++i)
            {
                var corner = corners[i];
                var gridPosition = corner.x * Vector2.right + _gridPosition;
                var waterHeight = _water.HeightAt(gridPosition);
                if (corner.y <= waterHeight)
                {
                    underwaterCorners.Add(
                        new UnderwaterCorner {Position = corner, WaterHeight = waterHeight, Index = i});
                }
            }

            return underwaterCorners;
        }

        class UnderwaterCorner
        {
            public Vector2 Position;
            public float WaterHeight;
            public int Index;
        }
    }
}