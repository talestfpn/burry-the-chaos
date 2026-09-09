using Godot;

public partial class MoedaUI : Control
{
    private Label _labelMoedas = null!;
    private AnimatedSprite2D _iconeMoeda = null!;
    private Global _global = null!;

    public override void _Ready()
    {
        _labelMoedas = GetNode<Label>("LabelMoedas");
        _iconeMoeda = GetNode<AnimatedSprite2D>("IconeMoedaAnimada");
        _global = GetNode<Global>("/root/Global");

        if (_iconeMoeda.SpriteFrames?.HasAnimation("girar") == true)
            _iconeMoeda.Play("girar");
        _global.Connect("moedas_alteradas", Callable.From<int>(_atualizar_moedas));
        _atualizar_moedas(_global.moedas);
    }

    private void _atualizar_moedas(int total)
    {
        _labelMoedas.Text = $"x {total}";
        _animar_contador();
    }

    private void _animar_contador()
    {
        Tween tween = GetTree().CreateTween();
        Scale = new Vector2(1.15f, 1.15f);
        tween.TweenProperty(this, "scale", Vector2.One, 0.12);
    }
}
