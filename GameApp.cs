using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Mikesweeper;

public class GameApp : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D spriteTexture;

    private Minefield minefield = new();
    private bool has_started { get => minefield.DiscoveredCells != 0; }
    private bool game_over = false;
    private bool wasChording = false;
    private bool wasDiscovering = false;
    private bool wasFlagging = false;

    private Options options;

    private MouseState mouse;
    /// <summary>The minefield index the mouse is currently hovering.</summary>
    private Point cursor;
    /// <summary>The number of seconds the user has been minesweeping since the first discovered cell.</summary>
    private float time;

    private GameButton optionsBtn = new(SpriteSheet.Options);
    private GameButton infoBtn = new(SpriteSheet.Info);
    private GameButton faceBtn = new(SpriteSheet.FaceHappy);

    public GameApp()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        minefield.Position.Y = 24;
        // Modernizes the settings menu appearance.
        System.Windows.Forms.Application.EnableVisualStyles();
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
        options = Options.Load();
        ApplyOptions();
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

        if (!UpdateButtons()) {
            UpdateChord();
            UpdateDiscover();
            UpdateFlag();
            if (!game_over && has_started) time += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        base.Update(gameTime);
    }

    private bool UpdateButtons() {
        optionsBtn.Position.X = 0;
        UpdateButton(optionsBtn);
        infoBtn.Position.X = minefield.Width * 16 - 24;
        UpdateButton(infoBtn);
        faceBtn.Position.X = minefield.Width * 16 / 2 - 12;
        UpdateButton(faceBtn);

        if (optionsBtn.WasReleased) {
            if (options.ShowForm()) ApplyOptions();
            return true;
        }
        if (infoBtn.WasReleased) {
            if (options.ShowForm()) ApplyOptions();
            return true;
        }
        if (faceBtn.WasReleased) {
            Restart();
            return true;
        }
        return false;
    }

    private void UpdateButton(GameButton btn) {
        var wasPressed = btn.IsPressed;
        var isHovering = mouse.Position.X > btn.Position.X * options.Zoom
            && mouse.Position.X < (btn.Position.X + btn.Sprite.Width) * options.Zoom
            && mouse.Position.Y > btn.Position.Y * options.Zoom
            && mouse.Position.Y < (btn.Position.Y + btn.Sprite.Height) * options.Zoom;
        btn.IsPressed = isHovering && mouse.LeftButton == ButtonState.Pressed;
        
        btn.WasReleased = wasPressed && !btn.IsPressed && isHovering;
    }
 
    private bool IsExiting() {
        return GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape);
    }

    private bool IsDiscovering() {
        return mouse.LeftButton == ButtonState.Pressed;
    }

    private bool DidDiscover() {
        return !IsDiscovering() && wasDiscovering;
    }

    private bool IsChording() {
        return minefield.IsDiscovered(cursor.X, cursor.Y)
            && (mouse.MiddleButton == ButtonState.Pressed
                || (mouse.LeftButton == ButtonState.Pressed && mouse.RightButton == ButtonState.Pressed)
                || (options.AlwaysChord && mouse.LeftButton == ButtonState.Pressed));
    }

    private bool DidChord() {
        return !game_over && minefield.IsDiscovered(cursor.X, cursor.Y) && !IsChording() && wasChording;
    }

    private bool IsFlagging() {
        return !game_over && mouse.RightButton == ButtonState.Pressed;
    }

    private bool DidFlag() {
        return !game_over && !IsFlagging() && wasFlagging;
    }

    private void UpdateDiscover() {
        if (DidDiscover()) Discover(cursor.X, cursor.Y);
        wasDiscovering = IsDiscovering();
    }

    private void UpdateChord() {
        if (DidChord()) Chord(cursor.X, cursor.Y);
        wasChording = IsChording();
    }

    private void UpdateFlag() {
        if (DidFlag()) minefield.ToggleFlag(cursor.X, cursor.Y);
        wasFlagging = IsFlagging();
    }

    private bool CanChord(int x, int y) {
        return minefield.FlagsNearby(x, y) == minefield.BombsNearby(x, y);
    }

    private void Chord(int x, int y) {
        if (CanChord(x, y)) {
            minefield.ForEachAdjacentCell(x, y, Discover);
        }
    }

    private void Discover(int x, int y) {
        if (!game_over && minefield.Discover(x, y) && minefield.IsBomb(x, y)) {
            game_over = true;
        }
        if (!game_over) game_over = HasWon();
    }

    private void ApplyOptions() {
        minefield.TargetBombCount = options.Bombs;
        minefield.Resize(options.Columns, options.Rows);
        _graphics.PreferredBackBufferWidth = minefield.Width * 16 * options.Zoom;
        _graphics.PreferredBackBufferHeight = (minefield.Height * 16 + minefield.Position.Y) * options.Zoom;
        _graphics.ApplyChanges();
        Restart();
    }

    private void UpdateMouse() {
        mouse = Mouse.GetState();
        var pos = mouse.Position;
        cursor = minefield.PositionToIndex(pos.X / options.Zoom, pos.Y / options.Zoom);
    }

    private void Restart() {
        time = 0;
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
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(options.Zoom, options.Zoom, 1f), samplerState: SamplerState.PointClamp);

        DrawMinefield();
        DrawNumber(new Vector2(24, 0), minefield.BombCount - minefield.FlagCount, 3);
        DrawNumber(new Vector2(minefield.Width * 16 - 13 * 3 - 24, 0), Math.Min((int)time, 999), 3);
        DrawButton(optionsBtn);
        DrawButton(faceBtn);
        DrawButton(infoBtn);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawButton(GameButton btn) {
        if (btn.IsPressed) {
            _spriteBatch.Draw(spriteTexture, btn.Position, SpriteSheet.ButtonPressed, Color.White);
            _spriteBatch.Draw(spriteTexture, btn.Position + new Vector2(1f, 1f), btn.Sprite, Color.White);
        } else {
            _spriteBatch.Draw(spriteTexture, btn.Position, SpriteSheet.ButtonUnpressed, Color.White);
            _spriteBatch.Draw(spriteTexture, btn.Position, btn.Sprite, Color.White);
        }
    }

    private void DrawMinefield() {
        for (int yy = 0; yy < minefield.Height; yy++)
        {
            for (int xx = 0; xx < minefield.Width; xx++)
            {
                var cell = minefield.Cell(xx, yy);
                var pos = new Vector2(xx * 16, yy * 16) + new Vector2(minefield.Position.X, minefield.Position.Y);
                var isDiscovering = (IsChording() && xx >= cursor.X - 1 && xx <= cursor.X + 1 && yy >= cursor.Y - 1 && yy <= cursor.Y + 1)
                    || (IsDiscovering() && xx == cursor.X && yy == cursor.Y);
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
}

public class GameButton {
    public Vector2 Position;
    public Rectangle Sprite;
    public bool IsPressed;
    public bool WasReleased;

    public GameButton(Rectangle sprite) {
        Sprite = sprite;
    }
}
