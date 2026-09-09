using Godot;

public partial class Sprite2DFade : Sprite2D
{
    public override void _Ready()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.5);
        tween.Finished += QueueFree;
    }
}
