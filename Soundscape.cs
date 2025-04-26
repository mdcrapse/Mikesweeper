
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace Mikesweeper;

/// <summary>Contains all the sounds in the game.</summary>
public class Soundscape
{
    public SoundEffect Explode;
    public SoundEffect Discover;
    public SoundEffect Flag;
    public SoundEffect Button;

    public void LoadSounds(ContentManager content)
    {
        foreach (var field in typeof(Soundscape).GetFields())
        {
            field.SetValue(this, content.Load<SoundEffect>(field.Name.ToLower()));
        }
    }
}
