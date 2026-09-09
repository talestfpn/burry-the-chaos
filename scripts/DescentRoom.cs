using Godot;

// Scene-authored geometry and encounters; this controller only applies progression.
public partial class DescentRoom : WorldRoomController
{
    [Export] public string DiscoveryFlag = "";
    [Export] public bool Rooftop;
    [Export] public float RescueDepth = 2000;
    private Player? _player;
    private Global _world = null!;
    public override void _Ready()
    {
        base._Ready();
        _world = GetNode<Global>("/root/Global");
        _world.SetWorldFlag(DiscoveryFlag);
        _player = GetNode<Player>("bury");
        _world.WorldChanged += ApplyAbilities;
        ApplyAbilities();
        _player.GetNode<Camera2D>("Camera2D").ResetSmoothing();
    }
    private void ApplyAbilities()
    {
        if (_player is null) return;
        _player.pode_pular = true;
        _player.pode_dash = !Rooftop || _world.HasWorldFlag("dash_lesson");
        _player.pode_atacar = _world.HasWorldFlag("sword");
        _player.pode_pulo_duplo = _world.HasWorldFlag("sword");
    }
    public override void _PhysicsProcess(double delta)
    {
        if (_player is null || _player.GlobalPosition.Y < RescueDepth) return;
        // Last-resort recovery, not part of any intended route.
        _player.GlobalPosition = GetNode<Marker2D>("SpawnPoints/Default").GlobalPosition;
        _player.Velocity = Vector2.Zero;
        _player.GetNode<Camera2D>("Camera2D").ResetSmoothing();
    }
    public override void _ExitTree()
    {
        if (IsInstanceValid(_world)) _world.WorldChanged -= ApplyAbilities;
    }
}
