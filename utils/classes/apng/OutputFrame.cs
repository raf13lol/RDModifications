using UnityEngine;

namespace APNGP;

public class OutputFrame(Texture2D texture, double frameDuration)
{
    public Texture2D Texture = texture;
    public double FrameDuration = frameDuration;
}