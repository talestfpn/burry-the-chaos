using Godot;

public partial class StaminaBar : TextureProgressBar
{
    private Player? _bury;
    private float _staminaAnterior = 100.0f;

    public override void _Ready()
    {
        MaxValue = 100.0;
        Value = 100.0;
        TextureUnder ??= TextureProgress;
        TintUnder = Colors.Black;

        _bury = GetTree().GetFirstNodeInGroup("Jogador") as Player;
        if (_bury is null)
            GD.Print("Aviso: Jogador não encontrado no grupo 'Jogador'!");
        else
        {
            MaxValue = _bury.MaxStamina;
            Value = _bury.stamina;
            _staminaAnterior = _bury.stamina;
        }
    }

    public override void _Process(double delta)
    {
        if (_bury is null)
            return;

        MaxValue = _bury.MaxStamina;
        Value = _bury.stamina;
        if (_staminaAnterior < _bury.MaxStamina && _bury.stamina >= _bury.MaxStamina)
            _piscar_brilho();

        _staminaAnterior = _bury.stamina;
    }

    private void _piscar_brilho()
    {
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(this, "modulate", new Color(2.5f, 2.5f, 2.5f, 1.0f), 0.05);
        tween.TweenProperty(this, "modulate", Colors.White, 0.2);
    }
}
