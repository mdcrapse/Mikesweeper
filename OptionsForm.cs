using System;
using System.Windows.Forms;

namespace Mikesweeper;

public class OptionsForm : Form
{
    public int Bombs => (int)bombs.Value;
    public int Rows => (int)rows.Value;
    public int Columns => (int)columns.Value;
    public int Zoom => (int)zoom.Value;
    public bool AlwaysChord => alwaysChord.Checked;
    public bool PuppyMode => puppyMode.Checked;
    public Difficulty difficulty;

    private NumericUpDown bombs;
    private NumericUpDown rows;
    private NumericUpDown columns;
    private NumericUpDown zoom;
    private CheckBox alwaysChord;

    private Button okButton;
    private Button cancelButton;
    private FlowLayoutPanel flow;
    private CheckBox puppyMode;

    private static (FlowLayoutPanel, NumericUpDown) NewNumericLabelled(string text, int min, int max, int value)
    {
        var num_flow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Padding = Padding.Empty,
        };

        num_flow.Controls.Add(new Label { Text = text, Width = 100, Margin = Padding.Empty });

        var num = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Min(max, Math.Max(min, value)), // avoids exception
            Width = 100,
            Margin = Padding.Empty,
        };
        num_flow.Controls.Add(num);

        return (num_flow, num);
    }

    private FlowLayoutPanel AddGroupFlow(string group_name)
    {
        var group = new GroupBox
        {
            Text = group_name,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
        };

        var grp_flow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 15, 3, 3)  // Add top margin to avoid title overlap
        };

        group.Controls.Add(grp_flow);
        flow.Controls.Add(group);

        return grp_flow;
    }

    public OptionsForm(Options options)
    {
        Text = "Mikesweeper Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        difficulty = options.difficulty;

        flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(10),
        };

        var level = AddGroupFlow("Difficulty");
        var easy = new RadioButton { Text = "Easy", Width = 200, Checked = options.difficulty == Difficulty.Easy };
        level.Controls.Add(easy);
        easy.CheckedChanged += (sender, args) =>
        {
            if (easy.Checked) {
                difficulty = Difficulty.Easy;
                bombs.Value = 15;
                rows.Value = 10;
                columns.Value = 10;
            }
        };

        var normal = new RadioButton { Text = "Normal", Width = 200, Checked = options.difficulty == Difficulty.Normal };
        level.Controls.Add(normal);
        normal.CheckedChanged += (sender, args) =>
        {
            if (normal.Checked) {
                difficulty = Difficulty.Normal;
                bombs.Value = 25;
                rows.Value = 15;
                columns.Value = 10;
            }
        };

        var hard = new RadioButton { Text = "Hard", Width = 200, Checked = options.difficulty == Difficulty.Hard };
        level.Controls.Add(hard);
        hard.CheckedChanged += (sender, args) =>
        {
            if (hard.Checked) {
                difficulty = Difficulty.Hard;
                bombs.Value = 99;
                rows.Value = 20;
                columns.Value = 20;
            }
        };

        var custom = new RadioButton { Text = "Custom", Width = 200, Checked = options.difficulty == Difficulty.Custom };
        level.Controls.Add(custom);
        custom.CheckedChanged += (sender, args) =>
        {
            if (custom.Checked) {
                difficulty = Difficulty.Custom;
            }
        };

        var settings = AddGroupFlow("Minefield");

        (var flow_bombs, bombs) = NewNumericLabelled("Bombs", Options.BombsMin, Options.BombsMax, options.Bombs);
        bombs.ValueChanged += (_, _) => { if (bombs.Focused) custom.Checked = true; };
        settings.Controls.Add(flow_bombs);

        (var flow_rows, rows) = NewNumericLabelled("Rows", Options.DimensionMin, Options.DimensionMax, options.Rows);
        rows.ValueChanged += (_, _) => { if (rows.Focused) custom.Checked = true; };
        settings.Controls.Add(flow_rows);

        (var flow_columns, columns) = NewNumericLabelled("Columns", Options.DimensionMin, Options.DimensionMax, options.Columns);
        columns.ValueChanged += (_, _) => { if (columns.Focused) custom.Checked = true; };
        settings.Controls.Add(flow_columns);

        alwaysChord = new CheckBox { Text = "Always Chord", Width = 200, Checked = options.AlwaysChord };
        settings.Controls.Add(alwaysChord);

        var graphics = AddGroupFlow("Graphics");
        (var flow_zoom, zoom) = NewNumericLabelled("Zoom", Options.ZoomMin, Options.ZoomMax, options.Zoom);
        graphics.Controls.Add(flow_zoom);
        puppyMode = new CheckBox { Text = "Puppy Mode", Width = 200, Checked = options.PuppyMode };
        graphics.Controls.Add(puppyMode);

        // Label

        // OK Button
        okButton = new Button()
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Dock = DockStyle.Fill
        };
        AcceptButton = okButton;
        flow.Controls.Add(okButton);

        // Cancel Button
        cancelButton = new Button()
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Dock = DockStyle.Fill
        };
        CancelButton = cancelButton;
        flow.Controls.Add(cancelButton);

        Controls.Add(flow);

        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }
}
