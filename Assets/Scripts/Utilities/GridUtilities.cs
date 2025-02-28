using UnityEngine;

namespace Utilities
{
    // Utility functions for representing grids as 1-dimensional arrays
    public static class GridUtilities
    {
        // Returns index for the grid cell at (x,y) stored in 1-dimensional arrays
        public static int GetIndex(int x, int y, int maxX) => x + (maxX + 1) * y;

        // Returns grid coordinates for the cell stored at index as a Vector2Int
        public static Vector2Int GridCoordinatesFromIndex(int index, int maxX)
        {
            var x = index % (maxX + 1);
            var y = index / (maxX + 1);
            return new Vector2Int(x, y);
        }
    }
}