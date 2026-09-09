using Godot;

public partial class WorldShortcut : Node2D
{
    [Export] public string shortcut_id = "shortcut_unset";

    private StaticBody2D _barrier = null!;
    private CollisionShape2D _barrierShape = null!;
    private Area2D _activationArea = null!;
    private Node2D _doorVisual = null!;
    private Label _prompt = null!;
    private bool _playerPerto;
    private bool _aberto;
    private Vector2 _visualClosedPosition;

    public override void _Ready()
    {
        _barrier = GetNode<StaticBody2D>("Barrier");
        _barrierShape = GetNode<CollisionShape2D>("Barrier/CollisionShape2D");
        _activationArea = GetNode<Area2D>("ActivationArea");
        _doorVisual = GetNode<Node2D>("DoorVisual");
        _prompt = GetNode<Label>("ActivationArea/Prompt");
        _visualClosedPosition = _doorVisual.Position;

        _activationArea.BodyEntered += _on_body_entered;
        _activationArea.BodyExited += _on_body_exited;
        Global? gameManager = GetNodeOrNull<Global>("/root/Global");
        if (gameManager?.atalho_esta_aberto(shortcut_id) == true)
            _aplicar_aberto(false);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_playerPerto && !_aberto && @event.IsActionPressed("interact"))
            abrir();
    }

    public void abrir()
    {
        if (_aberto)
            return;
        GetNodeOrNull<Global>("/root/Global")?.abrir_atalho(shortcut_id);
        _aplicar_aberto(true);
    }

    private void _aplicar_aberto(bool animar)
    {
        _aberto = true;
        _prompt.Visible = false;
        _barrierShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        Vector2 destino = _visualClosedPosition + new Vector2(0.0f, -72.0f);
        if (animar)
        {
            Tween tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
            tween.TweenProperty(_doorVisual, "position", destino, 0.45);
        }
        else
        {
            _doorVisual.Position = destino;
        }
    }

    private void _on_body_entered(Node2D body)
    {
        if (!body.IsInGroup("Jogador"))
            return;
        _playerPerto = true;
        _prompt.Visible = !_aberto;
    }

    private void _on_body_exited(Node2D body)
    {
        if (!body.IsInGroup("Jogador"))
            return;
        _playerPerto = false;
        _prompt.Visible = false;
    }
}
