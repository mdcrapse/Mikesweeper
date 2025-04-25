using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Text.Json.Serialization;

namespace Mikesweeper;

public enum Difficulty
{
    Easy,
    Normal,
    Hard,
    Custom,
}

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

    private static readonly string path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Mikesweeper", "options.json"
    );

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

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }

    public static Options Load()
    {
        if (File.Exists(path)) return JsonSerializer.Deserialize<Options>(File.ReadAllText(path));
        else return new Options();
    }
}
