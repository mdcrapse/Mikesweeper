using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Mikesweeper;

public class GameApp : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D spriteTexture;

    private Minefield minefield = new();
    private int zoom = 4;
    private bool has_started = false;
    private bool game_over = false;

    private MouseState currentMouse;
    private MouseState previousMouse;
    /// <summary>The minefield index the mouse is currently hovering.</summary>
    private Point cursor;
    /// <summary>The number of seconds the user has been minesweeping since the first discovered cell.</summary>
    private float time;

    public GameApp()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        minefield.Resize(10, 10);
        minefield.TargetBombCount = 15;
        minefield.Position.Y = 23;
    }

    private bool HasWon()
    {
        return !game_over && minefield.TotalCells - minefield.BombCount == minefield.DiscoveredCells;
    }

    public static void Play()
    {
        using var game = new GameApp();
        game.Run();
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = minefield.Width * 16 * zoom;
        _graphics.PreferredBackBufferHeight = (minefield.Height * 16 + 23) * zoom;
        _graphics.ApplyChanges();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        spriteTexture = Content.Load<Texture2D>("spritesheet");
    }

    protected override void Update(GameTime gameTime)
    {
        if (IsExiting()) Exit();

        UpdateMouse();
        UpdateDiscovery();
        UpdateFlag();
        if (!game_over && has_started) time += (float)gameTime.ElapsedGameTime.TotalSeconds;

        base.Update(gameTime);
    }

    private bool IsExiting() {
        return GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape);
    }

    private bool IsDiscoverButtonJustReleased() {
        return currentMouse.LeftButton == ButtonState.Pressed
            && previousMouse.LeftButton == ButtonState.Released;
    }

    private bool IsFlagButtonJustReleased() {
        return currentMouse.RightButton == ButtonState.Pressed
            && previousMouse.RightButton == ButtonState.Released;
    }

    private void UpdateDiscovery() {
        if (IsDiscoverButtonJustReleased())
        {
            // Discover cell
            if (game_over)
            {
                Restart();
            }
            else
            {
                if (!has_started) has_started = true;
                if (minefield.Discover(cursor.X, cursor.Y) && minefield.IsBomb(cursor.X, cursor.Y)) {
                    game_over = true;
                }
                if (!game_over) game_over = HasWon();
            }
        }
    }

    private void UpdateFlag() {
        if (has_started
            && !game_over
            && IsFlagButtonJustReleased())
        {
            minefield.ToggleFlag(cursor.X, cursor.Y);
        }
    }

    private void UpdateMouse() {
        previousMouse = currentMouse;
        currentMouse = Mouse.GetState();
        var pos = currentMouse.Position;
        cursor = minefield.PositionToIndex(pos.X / zoom, pos.Y / zoom);
    }

    private void Restart() {
        time = 0;
        has_started = false;
        game_over = false;
        minefield.Reset();
    }

    private void DrawNumber(Vector2 pos, int n, int digits) {
        var neg = n < 0;
        n = Math.Abs(n);
        pos.X += SpriteSheet.EmptyNumber.Width * digits;
        // Draws the number
        while (digits-- > 0) {
            int d = n % 10;
            n /= 10;
            pos.X -= SpriteSheet.EmptyNumber.Width;

            _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.Numbers[d], Color.White);
            if (n == 0) break;
        }
        // Draws the negative sign
        if (neg) {
            digits--;
            pos.X -= SpriteSheet.EmptyNumber.Width;
            _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.NegativeNumber, Color.White);
        }
        // Draws the empty digits
        while (digits-- > 0) {
            pos.X -= SpriteSheet.EmptyNumber.Width;
            _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.EmptyNumber, Color.White);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Gray);

        _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(zoom, zoom, 1f), samplerState: SamplerState.PointClamp);

        for (int yy = 0; yy < minefield.Width; yy++)
        {
            for (int xx = 0; xx < minefield.Height; xx++)
            {
                var cell = minefield.Cell(xx, yy);
                var pos = new Vector2(xx * 16, yy * 16) + new Vector2(minefield.Position.X, minefield.Position.Y);

                if (!cell.discovered)
                {
                    _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.CellUndiscovered, Color.White);
                    if (game_over && cell.bomb && !cell.flagged)
                        _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.Bomb, Color.White);
                    else if (game_over && cell.bomb && cell.flagged)
                        _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.BombFlagged, Color.White);
                    else if (cell.flagged)
                        _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.Flag, Color.White);
                }
                else
                {
                    _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.CellDiscovered, Color.White);
                    if (cell.bomb) _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.BombExploded, Color.White);
                    else _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.NearbyBombsNumbers[cell.nearby_bombs], Color.White);
                }
            }
        }

        DrawNumber(Vector2.Zero, minefield.BombCount - minefield.FlagCount, 3);
        DrawNumber(new Vector2(minefield.Width * 16 - 13 * 3, 0f), (int)time, 3);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
