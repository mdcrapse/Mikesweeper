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

    public GameApp()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        minefield.Resize(10, 10);
    }

    private void RandomizeMinefield(int x, int y, int max_bombs = 15)
    {
        // Helps avoid a potential infinite loop
        max_bombs = Math.Min(max_bombs, minefield.TotalCells - 9);
        var rng = new Random();
        // This can potentially lead to a near-infinite loop if a particular cell is never planted with a bomb.
        while(max_bombs > 0)
        {
            var xx = rng.Next(minefield.Width);
            var yy = rng.Next(minefield.Height);
            if (xx >= x - 1 && xx <= x + 1 && yy >= y - 1 && yy <= y + 1) continue;
            minefield.PlantBomb(xx, yy);
            max_bombs--;
        }
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
        _graphics.PreferredBackBufferHeight = minefield.Height * 16 * zoom;
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
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        previousMouse = currentMouse;
        currentMouse = Mouse.GetState();

        var pos = currentMouse.Position;
        var xx = (int)(pos.X / 16f / zoom);
        var yy = (int)(pos.Y / 16f / zoom);
        if (currentMouse.LeftButton == ButtonState.Pressed
            && previousMouse.LeftButton == ButtonState.Released)
        {
            // Discover cell
            if (game_over)
            {
                has_started = false;
                game_over = false;
                minefield.Reset();
            }
            else
            {
                if (!has_started)
                {
                    has_started = true;
                    RandomizeMinefield(xx, yy);
                }
                game_over = minefield.Discover(xx, yy);
                if (!game_over) game_over = HasWon();
            }
        }

        if (has_started
            && !game_over
            && currentMouse.RightButton == ButtonState.Pressed
            && previousMouse.RightButton == ButtonState.Released)
        {
            // Flag cell
            minefield.ToggleFlag(xx, yy);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(zoom, zoom, 1f), samplerState: SamplerState.PointClamp);

        for (int yy = 0; yy < minefield.Width; yy++)
        {
            for (int xx = 0; xx < minefield.Height; xx++)
            {
                var cell = minefield.Cell(xx, yy);
                var pos = new Vector2(xx * 16, yy * 16);

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


        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
