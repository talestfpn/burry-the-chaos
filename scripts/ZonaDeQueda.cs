using Godot;

public partial class ZonaDeQueda : Area2D
{
    public void _on_body_entered(Node2D body)
    {
        if (body.Name != "bury" || body is not CharacterBody2D player)
            return;

        player.GlobalPosition = new Vector2(1429.0f, 100.0f);
        player.Velocity = new Vector2(0.0f, -100.0f);

        AnimatedSprite2D? anim = player.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        anim?.Play("jump");
    }
}
