using Godot;

public partial class WorldInteraction : Area2D
{
    [Export] public string RequiredFlag = "";
    [Export] public string GrantedFlag = "";
    [Export] public string Hint = "F • interagir";
    [Export] public string LockedHint = "Ainda sem energia";
    [Export] public string CompletedHint = "";
    [Export(PropertyHint.File, "*.tscn")] public string Destination = "";
    [Export] public string DestinationSpawn = "Default";
    [Export] public bool Automatic;
    [Export] public bool HideWhenComplete;
    [Export] public bool RestOnInteract;
    [Export] public NodePath BarrierPath = new("");
    [Export] public NodePath TravelTarget = new("");
    private Global _world = null!;
    private Label _label = null!;
    private Player? _player;
    private bool _busy;
    public bool Completed => !string.IsNullOrEmpty(GrantedFlag) && _world.HasWorldFlag(GrantedFlag);
    public override void _Ready()
    {
        _world = GetNode<Global>("/root/Global");
        _label = GetNode<Label>("Prompt");
        BodyEntered += Enter;
        BodyExited += body => { if (body == _player) { _player = null; _label.Hide(); } };
        _world.WorldChanged += Refresh;
        Refresh();
    }
    private void Enter(Node2D body)
    {
        if (body is not Player p) return;
        _player = p;
        Refresh();
        if (Automatic) Activate();
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if (Automatic) return;
        if (_player is not null && e.IsActionPressed("interact") && !e.IsEcho())
        {
            Activate();
            GetViewport().SetInputAsHandled();
        }
    }
    public async void Activate()
    {
        if (_busy || _player is null || !_world.HasWorldFlag(RequiredFlag)) return;
        if (RestOnInteract) _player.RestAtBench();
        _world.SetWorldFlag(GrantedFlag);
        if (!string.IsNullOrEmpty(Destination))
        {
            _busy = true;
            _world.trocar_cena(Destination, DestinationSpawn);
        }
        else if (!TravelTarget.IsEmpty && GetNodeOrNull<Marker2D>(TravelTarget) is Marker2D target)
        {
            _busy = true;
            Player p = _player;
            p.SetPhysicsProcess(false);
            p.Velocity = Vector2.Zero;
            var tween = CreateTween();
            tween.TweenProperty(p, "global_position:x", GlobalPosition.X, 0.15);
            tween.TweenProperty(p, "global_position:y", target.GlobalPosition.Y, 0.9);
            tween.TweenProperty(p, "global_position:x", target.GlobalPosition.X, 0.15);
            await ToSignal(tween, Tween.SignalName.Finished);
            if (IsInstanceValid(p)) p.SetPhysicsProcess(true);
            _busy = false;
        }
        Refresh();
    }
    private void Refresh()
    {
        bool completed = Completed;
        _label.Text = !_world.HasWorldFlag(RequiredFlag) ? LockedHint : completed ? CompletedHint : Hint;
        _label.Visible = _player is not null && !string.IsNullOrEmpty(_label.Text);
        if (HideWhenComplete && GetNodeOrNull<CanvasItem>("Visual") is CanvasItem visual) visual.Visible = !completed;
        if (!BarrierPath.IsEmpty && GetNodeOrNull<Node2D>(BarrierPath) is Node2D barrier)
        {
            barrier.Visible = !completed;
            foreach (Node child in barrier.GetChildren())
                if (child is CollisionShape2D shape) shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, completed);
        }
    }
    public override void _ExitTree()
    {
        if (IsInstanceValid(_world)) _world.WorldChanged -= Refresh;
    }
}
