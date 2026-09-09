using Godot;

public partial class WorldElevator : Area2D
{
    private Player? _player;
    private Control _panel = null!;
    private Label _prompt = null!;
    private readonly string[] _scenes = { "topo_predio", "bury_the_chaos", "predio", "quinto_andar", "profundezas", "portao_king_rat" };
    public override void _Ready()
    {
        _panel = GetNode<Control>("UI/Panel");
        _prompt = GetNode<Label>("Prompt");
        _panel.Hide(); _prompt.Hide();
        BodyEntered += body => { if (body is Player p) { _player = p; _prompt.Show(); } };
        BodyExited += body => { if (body == _player) { Close(); _player = null; _prompt.Hide(); } };
        for (int i = 0; i < _scenes.Length; i++)
        {
            string target = _scenes[i];
            GetNode<Button>($"UI/Panel/Floors/F{i}").Pressed += () =>
            {
                var world = GetNode<Global>("/root/Global");
                if (!world.HasWorldFlag("visit_" + target)) return;
                Close();
                world.trocar_cena($"res://scene/{target}.tscn", "Lift");
            };
        }
        GetNode<Button>("UI/Panel/Floors/Close").Pressed += Close;
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if (_player is null || !e.IsActionPressed("interact") || e.IsEcho()) return;
        if (_panel.Visible) Close();
        else
        {
            _panel.Show(); _player.SetPhysicsProcess(false); _player.Velocity = Vector2.Zero;
            var world = GetNode<Global>("/root/Global");
            for (int i = 0; i < _scenes.Length; i++)
                GetNode<Button>($"UI/Panel/Floors/F{i}").Disabled = !world.HasWorldFlag("visit_" + _scenes[i]);
            GetNode<Button>("UI/Panel/Floors/Close").GrabFocus();
        }
        GetViewport().SetInputAsHandled();
    }
    private void Close()
    {
        _panel.Hide();
        if (IsInstanceValid(_player)) _player!.SetPhysicsProcess(true);
    }
}
