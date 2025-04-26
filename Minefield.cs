
using System;
using Microsoft.Xna.Framework;

namespace Mikesweeper;

/// <summary>Information about a single cell in a minefield.</summary>
public struct Cell
{
    /// <summary>Whether or not this cell has a bomb.</summary>
    public bool bomb;
    /// <summary>Whether or not this cell has been flagged by the user.</summary>
    public bool flagged;
    /// <summary>The number of adjacent bombs to this cell.</summary>
    public int nearby_bombs;
    /// <summary>
    /// Whether or not this cell has been discovered by the user.
    /// If this cell is a bomb and discovered by the user, it is a game over.
    /// </summary>
    public bool discovered;
}

/// <summary>Utilities for a grid of cells that may have bombs.</summary>
public class Minefield
{
    /// <summary>The grid of cells in the minefield.</summary>
    private Cell[,] grid = new Cell[0, 0];
    /// <summary>The pixel position of the top left of the minefield.</summary>
    public Point Position;
    /// <summary>The width and height of each cell in pixels.</summary>
    public int TileSize = 16;
    /// <summary>The number of columns in the minefield grid.</summary>
    public int Width { get => grid.GetLength(0); }
    /// <summary>The number of rows in the minefield grid.</summary>
    public int Height { get => grid.GetLength(1); }
    /// <summary>The number of cells discovered so far.</summary>
    public int DiscoveredCells { get; private set; } = 0;
    /// <summary>The total number of cells in the minefield (width * height).</summary>
    public int TotalCells { get => Width * Height; }
    /// <summary>The number of bombs in the minefield.</summary>
    public int BombCount { get; private set; }
    /// <summary>The number of flags in the minefield.</summary>
    public int FlagCount { get; private set; }
    /// <summary>The number of bombs to generate when randomly planting bombs.</summary>
    public int TargetBombCount;

    /// <summary>Converts position to an index in the minefield.</summary>
    /// <returns>the index at the position. The returned index may be out of bounds.</returns>
    public Point PositionToIndex(int x, int y) {
        int xx = (int)Math.Floor(((float)x - Position.X) / TileSize);
        int yy = (int)Math.Floor(((float)y - Position.Y) / TileSize);
        return new Point(xx, yy);
    }

    /// <summary>Resets all cells to default values.</summary>
    public void Reset()
    {
        Resize(Width, Height);
    }

    /// <summary>Resizes the minefield grid to the specified dimensions. Resets the state of all cells.</summary>
    public void Resize(int width, int height)
    {
        grid = new Cell[width, height];
        DiscoveredCells = 0;
        BombCount = 0;
        FlagCount = 0;
    }

    /// <summary>Checks if the specified position is within the bounds of the minefield grid.</summary>
    /// <returns>`true` if the position is within the bounds of the minefield grid.</returns>
    public bool IsInbounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    /// <summary>
    /// For retrieving cell information in the minefield grid.
    /// The information is returned by value instead of reference, so it may not be used to modify the minefield state.
    /// </summary>
    /// <returns>information about the cell at the location in the minefield grid.</returns>
    public Cell Cell(int x, int y) => grid[x, y];

    /// <summary>
    /// Places a bomb at the location specified and updates adjacent cell nearby bomb numbers.
    /// Does nothing if the cell is out of bounds.
    /// </summary>
    /// <returns>`true` if the bomb was successfully placed at the location.</returns>
    public bool PlantBomb(int x, int y)
    {
        if (!IsInbounds(x, y)) return false;
        ref var cell = ref grid[x, y];
        if (cell.bomb) return false;
        cell.bomb = true;
        BombCount++;
        IncrementAdjacentBombCounters(x, y);
        return true;
    }

    /// <summary>Toggles the flag at the specified cell. Does nothing if the cell is out of bounds or discovered.</summary>
    /// <returns>`true` if the flag was toggled.<\returns>
    public bool ToggleFlag(int x, int y)
    {
        if (!CanFlag(x, y)) return false;
        ref var cell = ref grid[x, y]; 
        cell.flagged = !cell.flagged;
        if (cell.flagged) FlagCount++;
        else FlagCount--;
        return true;
    }

    /// <returns>`true` if the cell flag may be toggled.</returns>
    public bool CanFlag(int x, int y) {
        return IsInbounds(x, y) && !grid[x, y].discovered;
    }

    /// <summary>
    /// Marks a cell as discovered. Does nothing if the cell is flagged.
    /// Recursively marks cells as discovered for empty cells.
    /// Should be a game over if the cell is a bomb.
    /// </summary>
    /// <returns>`true` if the cell was discovered.</returns>
    public bool Discover(int x, int y)
    {
        if (!CanDiscover(x, y)) return false;

        if (DiscoveredCells == 0) Randomize(x, y);

        ref var cell = ref grid[x, y];
        cell.discovered = true;
        DiscoveredCells++;
        if (cell.nearby_bombs == 0)
        {
            // There's a lot of redundant checks here, but it's fast enough.
            ForEachAdjacentCell(x, y, (xx, yy) => Discover(xx, yy));
        }
        return true;
    }

    /// <returns>`true` if the cell may be discovered.</returns>
    public bool CanDiscover(int x, int y) {
        return IsInbounds(x, y) && !grid[x, y].discovered && !grid[x, y].flagged;
    }

    /// <returns>`true` if the position is a bomb cell.</returns>
    public bool IsBomb(int x, int y) {
        return IsInbounds(x, y) && grid[x, y].bomb;
    }

    /// <returns>`true` if the position is a discovered cell.</returns>
    public bool IsDiscovered(int x, int y) {
        return IsInbounds(x, y) && grid[x, y].discovered;
    }

    /// <summary>Randomly places bombs around the minefield while avoiding the specified location.</summary>
    public void Randomize(int x, int y) {
        // Helps avoid a potential infinite loop
        var num_bombs = Math.Min(TargetBombCount, TotalCells - 9);
        var rng = new Random();
        // This can potentially lead to a near-infinite loop if a particular cell is never planted with a bomb.
        while(num_bombs > 0)
        {
            var xx = rng.Next(Width);
            var yy = rng.Next(Height);
            if (xx >= x - 1 && xx <= x + 1 && yy >= y - 1 && yy <= y + 1) continue;
            if (PlantBomb(xx, yy)) num_bombs--;
        }
    }

    /// <returns>the number of flags in adjacent cells.</returns>
    public int FlagsNearby(int x, int y) {
        int num = 0;
        ForEachAdjacentCell(x, y, (xx, yy) => {if (grid[xx, yy].flagged) num++;});
        return num;
    }

    /// <returns>the number of bombs in adjacent cells.</returns>
    public int BombsNearby(int x, int y) {
        return IsInbounds(x, y) ? grid[x, y].nearby_bombs : 0;
    }

    /// <summary>Increments the bombs counter around adjacent tiles.</summary>
    private void IncrementAdjacentBombCounters(int x, int y)
    {
        grid[x, y].nearby_bombs++;
        ForEachAdjacentCell(x, y, (xx, yy) => grid[xx, yy].nearby_bombs++);
    }

    /// <summary>
    /// Applies the function to the adjacent cells of the specified position.
    /// Does not apply the function to cells that are out of bounds.
    /// </summary>
    public void ForEachAdjacentCell(int x, int y, Action<int, int> f)
    {
        for (int yy = Math.Max(0, y - 1); yy < Math.Min(Height, y + 2); yy++)
        {
            for (int xx = Math.Max(0, x - 1); xx < Math.Min(Width, x + 2); xx++)
            {
                if (xx != x || yy != y) f(xx, yy);
            }
        }
    }
}
