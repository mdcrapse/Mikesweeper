using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Text.Json.Serialization;

namespace Mikesweeper;

/// <summary>The difficult setting of the minefield.</summary>
public enum Difficulty
{
    Easy,
    Normal,
    Hard,
    Custom,
}

/// <summary>The game options for the minefield, graphics, and modes.</summary>
public class Options
{
    [JsonInclude]
    public int Bombs = 15;
    [JsonInclude]
    public int Rows = 10;
    [JsonInclude]
    public int Columns = 10;
    [JsonInclude]
    public int Zoom = 4;
    [JsonInclude]
    public bool AlwaysChord = false;
    [JsonInclude]
    public bool PuppyMode = false;
    [JsonInclude]
    public Difficulty difficulty;

    public const int BombsMax = 999;
    public const int BombsMin = 1;
    public const int DimensionMax = 40;
    public const int DimensionMin = 10;
    public const int ZoomMax = 4;
    public const int ZoomMin = 1;

    /// <summary>Path to the options save data.</summary>
    private static readonly string path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Mikesweeper", "options.json"
    );

    /// <summary>Shows the options menu.</summary>
    /// <returns>`true` if the settings were modified.</returns>
    public bool ShowForm()
    {
        using var form = new OptionsForm(this);
        if (form.ShowDialog() == DialogResult.OK)
        {
            Bombs = form.Bombs;
            Columns = form.Columns;
            Rows = form.Rows;
            Zoom = form.Zoom;
            AlwaysChord = form.AlwaysChord;
            PuppyMode = form.PuppyMode;
            difficulty = form.difficulty;
            Save();
            return true;
        }
        return false;
    }

    /// <summary>Saves the options to the app data directory.</summary>
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }

    /// <returns>the options loaded from the app data directory or defaults if the directory is missing.</returns>
    public static Options Load()
    {
        if (File.Exists(path)) return JsonSerializer.Deserialize<Options>(File.ReadAllText(path));
        else return new Options();
    }
}
