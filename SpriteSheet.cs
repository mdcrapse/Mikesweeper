
using System.Linq;
using Microsoft.Xna.Framework;

namespace Mikesweeper;

/// <summary>Contains coordinate information for each sprite in the game.</summary>
public static class SpriteSheet {
    public static readonly Rectangle CellUndiscovered = new(1, 50, 16, 16);
    public static readonly Rectangle CellDiscovered = new(18, 50, 16, 16);
    public static readonly Rectangle Bomb = new(86, 50, 16, 16);
    public static readonly Rectangle BombExploded = new(103, 50, 16, 16);
    public static readonly Rectangle BombFlagged = new(120, 50, 16, 16);
    public static readonly Rectangle Flag = new(35, 50, 16, 16);
    public static readonly Rectangle[] NearbyBombsNumbers = [.. Enumerable.Range(0, 10).Select(x => new Rectangle(1 + x * 17, 67, 16, 16))];
    public static readonly Rectangle[] Numbers = [.. Enumerable.Range(0, 10).Select(x => new Rectangle(1 + x * 14, 1, 13, 23))];
    public static readonly Rectangle EmptyNumber = new(141, 1, 13, 23);
    public static readonly Rectangle NegativeNumber = new(155, 1, 13, 23);
    public static readonly Rectangle Options = new(1, 84, 24, 24);
    public static readonly Rectangle Info = new(26, 84, 24, 24);
    public static readonly Rectangle FaceHappy = new(1, 25, 24, 24);
    public static readonly Rectangle ButtonUnpressed = new(51, 84, 24, 24);
    public static readonly Rectangle ButtonPressed = new(76, 84, 24, 24);
}
