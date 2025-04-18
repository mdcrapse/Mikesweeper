
using System;

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
    public void PlantBomb(int x, int y)
    {
        if (!IsInbounds(x, y)) return;
        ref var cell = ref grid[x, y];
        if (cell.bomb) return;
        cell.bomb = true;
        BombCount++;
        IncrementAdjacentBombCounters(x, y);
    }

    /// <summary>Toggles the flag at the specified cell. Does nothing if the cell is out of bounds or discovered.</summary>
    public void ToggleFlag(int x, int y)
    {
        if (!IsInbounds(x, y) || grid[x, y].discovered) return;
        grid[x, y].flagged = !grid[x, y].flagged;
    }

    /// <summary>
    /// Marks a cell as discovered. Does nothing if the cell is flagged.
    /// Recursively marks cells as discovered for empty cells.
    /// Should be a game over if the cell is a bomb.
    /// Returns `false` and does nothing if the cell is out of bounds, discovered, or flagged.
    /// </summary>
    /// <returns>`true` if the discovered cell is a bomb.</returns>
    public bool Discover(int x, int y)
    {
        if (!IsInbounds(x, y)) return false;

        ref var cell = ref grid[x, y];
        if (cell.discovered || cell.flagged) return false;
        cell.discovered = true;
        DiscoveredCells++;
        if (cell.nearby_bombs == 0)
        {
            // There's a lot of redundant checks here, but it's fast enough.
            ForEachAdjacentCell(x, y, (xx, yy) => Discover(xx, yy));
        }
        return cell.bomb;
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
    private void ForEachAdjacentCell(int x, int y, Action<int, int> f)
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
