using System;
using UnityEngine;

namespace ArmyVArmy.Sim
{
    // Uniform grid over a fixed world area. Rebuilt once per tick via counting sort into
    // pre-allocated arrays, so neighbor queries during the tick never allocate.
    public class SpatialGrid
    {
        readonly float cellSize;
        readonly float originX;
        readonly float originY;
        readonly int columns;
        readonly int rows;

        readonly int[] cellOfUnit;
        readonly int[] cellCount;
        readonly int[] cellStart;
        readonly int[] cellCursor;
        readonly int[] bucket;

        public SpatialGrid(float worldWidth, float worldHeight, float cellSize, float originX, float originY, int unitCapacity)
        {
            this.cellSize = cellSize;
            this.originX = originX;
            this.originY = originY;
            columns = Mathf.Max(1, Mathf.CeilToInt(worldWidth / cellSize));
            rows = Mathf.Max(1, Mathf.CeilToInt(worldHeight / cellSize));

            int totalCells = columns * rows;
            cellOfUnit = new int[unitCapacity];
            cellCount = new int[totalCells];
            cellStart = new int[totalCells + 1];
            cellCursor = new int[totalCells];
            bucket = new int[unitCapacity];
        }

        public void Rebuild(Vector2[] positions, bool[] alive, int count)
        {
            Array.Clear(cellCount, 0, cellCount.Length);

            for (int i = 0; i < count; i++)
            {
                if (!alive[i])
                {
                    cellOfUnit[i] = -1;
                    continue;
                }

                int cell = CellIndex(positions[i]);
                cellOfUnit[i] = cell;
                cellCount[cell]++;
            }

            int running = 0;
            for (int c = 0; c < cellCount.Length; c++)
            {
                cellStart[c] = running;
                cellCursor[c] = running;
                running += cellCount[c];
            }
            cellStart[cellCount.Length] = running;

            for (int i = 0; i < count; i++)
            {
                int cell = cellOfUnit[i];
                if (cell < 0)
                    continue;

                bucket[cellCursor[cell]] = i;
                cellCursor[cell]++;
            }
        }

        public int CellIndex(Vector2 position)
        {
            int cx = Mathf.Clamp(Mathf.FloorToInt((position.x - originX) / cellSize), 0, columns - 1);
            int cy = Mathf.Clamp(Mathf.FloorToInt((position.y - originY) / cellSize), 0, rows - 1);
            return cy * columns + cx;
        }

        public void GetNeighborCellRange(Vector2 position, out int cxMin, out int cxMax, out int cyMin, out int cyMax)
        {
            int cx = Mathf.Clamp(Mathf.FloorToInt((position.x - originX) / cellSize), 0, columns - 1);
            int cy = Mathf.Clamp(Mathf.FloorToInt((position.y - originY) / cellSize), 0, rows - 1);
            cxMin = Mathf.Max(0, cx - 1);
            cxMax = Mathf.Min(columns - 1, cx + 1);
            cyMin = Mathf.Max(0, cy - 1);
            cyMax = Mathf.Min(rows - 1, cy + 1);
        }

        public int CellAt(int cx, int cy) => cy * columns + cx;
        public int BucketStart(int cell) => cellStart[cell];
        public int BucketCount(int cell) => cellCount[cell];
        public int UnitAt(int bucketSlot) => bucket[bucketSlot];
    }
}
