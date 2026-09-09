using Godot;

public partial class WorldRoomController : Node2D
{
    [Export] public string room_id = "Room_Unnamed";
    [Export] public string region_id = "Region_Unnamed";
    [Export] public string default_spawn = "Default";

    public string SpawnAplicado { get; private set; } = "Default";

    public override void _Ready()
    {
        AplicarSpawnDoMundo();
    }

    protected bool AplicarSpawnDoMundo(string fallback = "Default")
    {
        Global? gameManager = GetNodeOrNull<Global>("/root/Global");
        SpawnAplicado = gameManager?.consumir_spawn_pendente(room_id, fallback) ?? fallback;

        Player? player = GetTree().GetFirstNodeInGroup("Jogador") as Player;
        if (player is null)
        {
            GD.PushWarning($"{room_id}: jogador não encontrado para aplicar spawn.");
            return false;
        }

        if (SpawnAplicado == "Checkpoint" && gameManager is not null && gameManager.TryGetRest(room_id, out Vector2 rest))
        {
            player.GlobalPosition = rest;
            player.Velocity = Vector2.Zero;
            return true;
        }

        Marker2D? spawn = GetNodeOrNull<Marker2D>($"SpawnPoints/{SpawnAplicado}")
            ?? GetNodeOrNull<Marker2D>($"SpawnPoints/{default_spawn}")
            ?? GetNodeOrNull<Marker2D>($"SpawnPoints/{fallback}");
        if (spawn is null)
        {
            GD.PushWarning($"{room_id}: spawn '{SpawnAplicado}' não encontrado.");
            return false;
        }

        player.GlobalPosition = spawn.GlobalPosition;
        player.Velocity = Vector2.Zero;
        return true;
    }
}
