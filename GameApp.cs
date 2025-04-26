using System;
using Microsoft.Xna.Framework;
// using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Mikesweeper;

public class GameApp : Game
{
    // ============================== ASSETS ==============================

    #region Assets
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    /// <summary>The texture for the current mode.</summary>
    private Texture2D spriteTexture;
    /// <summary>The texture to use for the default mode.</summary>
    private Texture2D normalTexture;
    /// <summary>The texture to use for puppy mode.</summary>
    private Texture2D puppyTexture;

    // private Soundscape sounds = new();

    #endregion

    // ============================== MINEFIELD ==============================

    #region Minefield
    private Options options;
    private Minefield minefield = new();

    /// <summary>Updates the user discovering cells.</summary>
    private void UpdateDiscover()
    {
        if (DidDiscover()) Discover(cursor.X, cursor.Y);
        wasDiscovering = IsDiscovering();
    }

    /// <summary>Updates the user chording cells.</summary>
    private void UpdateChord()
    {
        if (DidChord()) Chord(cursor.X, cursor.Y);
        wasChording = IsChording();
    }

    /// <summary>Updates the user toggling flags.</summary>
    private void UpdateFlag()
    {
        if (DidFlag() && minefield.ToggleFlag(cursor.X, cursor.Y))
        {
            // sounds.Flag.Play();
        }
        wasFlagging = IsFlagging();
    }

    /// <returns>`true` if the user may chord at the location.</returns>
    private bool CanChord(int x, int y)
    {
        return minefield.FlagsNearby(x, y) == minefield.BombsNearby(x, y);
    }

    /// <summary>Chords the cells at the location.</summary>
    private void Chord(int x, int y)
    {
        if (CanChord(x, y))
        {
            minefield.ForEachAdjacentCell(x, y, Discover);
        }
    }

    /// <summary>Discovers the cell at the location and updates the game over and winnning status.</summary>
    private void Discover(int x, int y)
    {
        if (!game_over && minefield.Discover(x, y))
        {
            // sounds.Discover.Play();
            if (minefield.IsBomb(x, y))
            {
                // sounds.Explode.Play();
                game_over = true;
            }
            won = HasWon();
            if (!game_over) game_over = won;
        }
    }

    /// <summary>Draws the minefield.</summary>
    private void DrawMinefield()
    {
        for (int yy = 0; yy < minefield.Height; yy++)
        {
            for (int xx = 0; xx < minefield.Width; xx++)
            {
                var cell = minefield.Cell(xx, yy);
                var pos = new Vector2(xx * 16, yy * 16) + new Vector2(minefield.Position.X, minefield.Position.Y);
                var isDiscovering = !game_over
                    && ((IsChording() && xx >= cursor.X - 1 && xx <= cursor.X + 1 && yy >= cursor.Y - 1 && yy <= cursor.Y + 1)
                        || (IsDiscovering() && xx == cursor.X && yy == cursor.Y));
                var isFlagging = IsFlagging() && xx == cursor.X && yy == cursor.Y;

                if (!cell.discovered)
                {
                    if (isDiscovering && !cell.flagged)
                        _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.CellDiscovered, Color.White);
                    else _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.CellUndiscovered, Color.White);

                    if (game_over && cell.bomb && !cell.flagged)
                        _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.Bomb, Color.White);
                    else if (game_over && !cell.bomb && cell.flagged)
                        _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.BombFlagged, Color.White);
                    else if (cell.flagged || (!isDiscovering && isFlagging))
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
    }

    #endregion

    // ============================== GAME ==============================

    #region Game
    /// <summary>`true` if the first cell has been discovered.</summary>
    private bool has_started { get => minefield.DiscoveredCells != 0; }
    /// <summary>`true` if the game has been won or lost.</summary>
    private bool game_over = false;
    /// <summary>Whether the user has won the game or not.</summary>
    private bool won = false;
    /// <summary>The number of seconds the user has been minesweeping since the first discovered cell.</summary>
    private float time;

    /// <summary>Plays the game.</summary>
    public static void Play()
    {
        using var game = new GameApp();
        game.Run();
    }

    public GameApp()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        minefield.Position.Y = 24;
        // Modernizes the settings menu appearance.
        System.Windows.Forms.Application.EnableVisualStyles();
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        normalTexture = Content.Load<Texture2D>("spritesheet");
        puppyTexture = Content.Load<Texture2D>("puppy_spritesheet");
        // sounds.LoadSounds(Content);

        options = Options.Load();
        ApplyOptions();
    }

    /// <summary>Resets the game variables to the default while maintaining the minefield settings.</summary>
    private void Restart()
    {
        time = 0;
        won = false;
        game_over = false;
        minefield.Reset();
    }

    /// <summary>Updates the game.</summary>
    protected override void Update(GameTime gameTime)
    {
        if (IsExiting()) Exit();

        UpdateMouse();

        if (UpdateButtons()) ResetInputs();

        UpdateChord();
        UpdateDiscover();
        UpdateFlag();
        if (!game_over && has_started) time += (float)gameTime.ElapsedGameTime.TotalSeconds;

        base.Update(gameTime);
    }

    /// <summary>Applies the game options to the game.</summary>
    private void ApplyOptions()
    {
        minefield.TargetBombCount = options.Bombs;
        minefield.Resize(options.Columns, options.Rows);
        _graphics.PreferredBackBufferWidth = minefield.Width * 16 * options.Zoom;
        _graphics.PreferredBackBufferHeight = (minefield.Height * 16 + minefield.Position.Y) * options.Zoom;
        _graphics.ApplyChanges();
        spriteTexture = options.PuppyMode ? puppyTexture : normalTexture;
        Restart();
    }

    /// <summary>Draws the game.</summary>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(options.Zoom, options.Zoom, 1f), samplerState: SamplerState.PointClamp);

        DrawMinefield();
        var bombs = has_started ? minefield.BombCount - minefield.FlagCount : Math.Min(minefield.TotalCells - 9, minefield.TargetBombCount);
        DrawNumber(new Vector2(24, 0), bombs, 3);
        DrawNumber(new Vector2(minefield.Width * 16 - 13 * 3 - 24, 0), Math.Min((int)time, 999), 3);
        DrawButton(optionsBtn);
        DrawButton(faceBtn);
        DrawButton(infoBtn);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    /// <summary>Draws an integer number at the position with the specified number of digits.</summary>
    private void DrawNumber(Vector2 pos, int n, int digits)
    {
        var neg = n < 0;
        n = Math.Abs(n);
        pos.X += SpriteSheet.EmptyNumber.Width * digits;
        // Draws the number
        while (digits-- > 0)
        {
            int d = n % 10;
            n /= 10;
            pos.X -= SpriteSheet.EmptyNumber.Width;

            _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.Numbers[d], Color.White);
            if (n == 0) break;
        }
        // Draws the negative sign
        if (neg)
        {
            digits--;
            pos.X -= SpriteSheet.EmptyNumber.Width;
            _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.NegativeNumber, Color.White);
        }
        // Draws the empty digits
        while (digits-- > 0)
        {
            pos.X -= SpriteSheet.EmptyNumber.Width;
            _spriteBatch.Draw(spriteTexture, pos, SpriteSheet.EmptyNumber, Color.White);
        }
    }

    /// <returns>`true` if the user has met the winning condition of the game.</returns>
    private bool HasWon()
    {
        return !game_over && minefield.TotalCells - minefield.BombCount == minefield.DiscoveredCells;
    }

    #endregion

    // ============================== INPUT ==============================

    #region Input
    private bool wasChording = false;
    private bool wasDiscovering = false;
    private bool wasFlagging = false;
    private MouseState mouse;
    /// <summary>The minefield index the mouse is currently hovering.</summary>
    private Point cursor;

    /// <summary>Resets all inputs for helping to avoid an extra click when switching menus.</summary>
    private void ResetInputs()
    {
        wasDiscovering = false;
        wasChording = false;
        wasFlagging = false;
    }

    /// <returns>`true` if the user is attempting to exit the application.</returns>
    private bool IsExiting()
    {
        return GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape);
    }

    /// <returns>`true` if the user is holding the discover cell input.</returns>
    private bool IsDiscovering()
    {
        return mouse.LeftButton == ButtonState.Pressed;
    }

    /// <returns>`true` if the user attempted to discover a cell.</returns>
    private bool DidDiscover()
    {
        return !IsDiscovering() && wasDiscovering;
    }

    /// <returns>`true` if the user is holding the chord cell input.</returns>
    private bool IsChording()
    {
        return minefield.IsDiscovered(cursor.X, cursor.Y)
            && (mouse.MiddleButton == ButtonState.Pressed
                || (mouse.LeftButton == ButtonState.Pressed && mouse.RightButton == ButtonState.Pressed)
                || (options.AlwaysChord && mouse.LeftButton == ButtonState.Pressed));
    }

    /// <returns>`true` if the user attempted to chord a cell.</returns>
    private bool DidChord()
    {
        return !game_over && minefield.IsDiscovered(cursor.X, cursor.Y) && !IsChording() && wasChording;
    }

    /// <returns>`true` if the user is holding the toggle flag input.</returns>
    private bool IsFlagging()
    {
        return !game_over && mouse.RightButton == ButtonState.Pressed;
    }

    /// <returns>`true` if the user attempted to toggle a flag.</returns>
    private bool DidFlag()
    {
        return !game_over && !IsFlagging() && wasFlagging;
    }

    /// <summary>Updates the mouse and cursor state from the user.</summary>
    private void UpdateMouse()
    {
        mouse = Mouse.GetState();
        var pos = mouse.Position;
        cursor = minefield.PositionToIndex(pos.X / options.Zoom, pos.Y / options.Zoom);
    }

    #endregion

    // ============================== BUTTONS ==============================

    #region Buttons
    private GameButton optionsBtn = new(SpriteSheet.Options);
    private GameButton infoBtn = new(SpriteSheet.Info);
    private GameButton faceBtn = new(SpriteSheet.FaceHappy);

    /// <summary>Handles button presses and switching menus.</summary>
    /// <returns>`true` if a button was pressed.</returns>
    private bool UpdateButtons()
    {
        optionsBtn.Position.X = 0;
        UpdateButton(optionsBtn);
        infoBtn.Position.X = minefield.Width * 16 - 24;
        UpdateButton(infoBtn);
        faceBtn.Position.X = minefield.Width * 16 / 2 - 12;
        UpdateButton(faceBtn);

        faceBtn.Sprite = SpriteSheet.FaceHappy;
        if (won)
        {
            faceBtn.Sprite = SpriteSheet.FaceWon;
        }
        else if (game_over)
        {
            faceBtn.Sprite = SpriteSheet.FaceLost;
        }
        else if (IsDiscovering() || IsChording())
        {
            faceBtn.Sprite = SpriteSheet.FaceDiscovering;
        }

        if (optionsBtn.WasReleased)
        {
            if (options.ShowForm()) ApplyOptions();
            return true;
        }
        if (infoBtn.WasReleased)
        {
            System.Windows.Forms.MessageBox.Show("Mikesweeper.\n\nCreated by Michael Crapse\nCreated with MonoGame\n\nControls:\n\tMouse Left Click: Discover\n\tMouse Right Click: Flag\n\tMouse Middle Click: Chord", "Mikesweeper");
            return true;
        }
        if (faceBtn.WasReleased)
        {
            Restart();
            return true;
        }
        return false;
    }

    /// <summary>Updates the state of a button.</summary>
    private void UpdateButton(GameButton btn)
    {
        var wasPressed = btn.IsPressed;
        var isHovering = mouse.Position.X > btn.Position.X * options.Zoom
            && mouse.Position.X < (btn.Position.X + btn.Sprite.Width) * options.Zoom
            && mouse.Position.Y > btn.Position.Y * options.Zoom
            && mouse.Position.Y < (btn.Position.Y + btn.Sprite.Height) * options.Zoom;
        btn.IsPressed = isHovering && mouse.LeftButton == ButtonState.Pressed;

        btn.WasReleased = wasPressed && !btn.IsPressed && isHovering;
        // if (btn.WasReleased) {
        //     sounds.Button.Play();
        // }
    }

    /// <summary>A button with a sprite and pressed state.</summary>
    public class GameButton
    {
        public Vector2 Position;
        public Rectangle Sprite;
        public bool IsPressed;
        public bool WasReleased;

        public GameButton(Rectangle sprite)
        {
            Sprite = sprite;
        }
    }

    /// <summary>Draws a button with a visual queue of its pressed state with its texture.</summary>
    private void DrawButton(GameButton btn)
    {
        if (btn.IsPressed)
        {
            _spriteBatch.Draw(spriteTexture, btn.Position, SpriteSheet.ButtonPressed, Color.White);
            _spriteBatch.Draw(spriteTexture, btn.Position + new Vector2(1f, 1f), btn.Sprite, Color.White);
        }
        else
        {
            _spriteBatch.Draw(spriteTexture, btn.Position, SpriteSheet.ButtonUnpressed, Color.White);
            _spriteBatch.Draw(spriteTexture, btn.Position, btn.Sprite, Color.White);
        }
    }

    #endregion
}
