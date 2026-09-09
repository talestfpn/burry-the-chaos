using Godot;

public partial class WorldFlagBarrier : Node2D
{
    [Export] public string OpenFlag = "";
    private Global _world = null!;
    public override void _Ready()
    {
        _world = GetNode<Global>("/root/Global");
        _world.WorldChanged += Refresh;
        Refresh();
    }
    private void Refresh()
    {
        bool open = _world.HasWorldFlag(OpenFlag);
        Visible = !open;
        SetShapes(this, open);
    }
    private static void SetShapes(Node n, bool disabled)
    {
        if (n is CollisionShape2D shape) shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, disabled);
        foreach (Node child in n.GetChildren()) SetShapes(child, disabled);
    }
    public override void _ExitTree()
    {
        if (IsInstanceValid(_world)) _world.WorldChanged -= Refresh;
    }
}
