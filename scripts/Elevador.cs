using Godot;

public partial class Elevador : StaticBody2D
{
    public AnimatedSprite2D anim { get; private set; } = null!;
    public Area2D area { get; private set; } = null!;

    public override void _Ready()
    {
        anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        area = GetNode<Area2D>("AreaDeteccao");
        anim.Play("fechado");
    }
}
