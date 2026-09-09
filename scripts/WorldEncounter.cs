using Godot;

public partial class WorldEncounter : Node2D
{
    [Export] public string CompletionFlag = "";
    private Global _world = null!;
    private bool _finished;
    public override void _Ready()
    {
        _world = GetNode<Global>("/root/Global");
        _finished = _world.HasWorldFlag(CompletionFlag);
        if (_finished) foreach (Node enemy in GetChildren()) enemy.QueueFree();
    }
    public override void _Process(double delta)
    {
        if (_finished || GetChildCount() > 0) return;
        _finished = true;
        _world.SetWorldFlag(CompletionFlag);
    }
}
