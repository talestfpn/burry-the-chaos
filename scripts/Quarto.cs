using Godot;

public partial class Quarto : WorldRoomController
{
    public override async void _Ready()
    {
        AplicarSpawnDoMundo("Start");
        Player player = GetNode<Player>("bury");
        Camera2D? camera = GetNodeOrNull<Camera2D>("bury/Camera2D");
        AnimatedSprite2D camaSprite = GetNode<AnimatedSprite2D>("tiles/cama");
        AnimatedSprite2D spritePlayer = GetNode<AnimatedSprite2D>("bury/AnimatedSprite2D");
        Marker2D? pontoSpawn = GetNodeOrNull<Marker2D>("PontoDeSpawn");

        if (camera is not null)
        {
            camera.Enabled = true;
            camera.Zoom = new Vector2(1.5f, 1.5f);
            camera.Position = Vector2.Zero;
            camera.Offset = new Vector2(camera.Offset.X, -35.0f);
            camera.LimitBottom = 1_000_000;
            camera.LimitTop = -1_000_000;
        }

        if (SpawnAplicado == "FromRua")
        {
            player.Visible = true;
            player.Velocity = Vector2.Zero;
            player.SetPhysicsProcess(true);
            player.bloqueio_animacao = false;
            spritePlayer.Play("idle");
            return;
        }

        if (pontoSpawn is not null)
            player.GlobalPosition = pontoSpawn.GlobalPosition;

        player.Velocity = Vector2.Zero;
        player.SetPhysicsProcess(false);
        player.bloqueio_animacao = true;
        player.Visible = false;

        camaSprite.Play("levantando");
        camaSprite.Frame = 0;
        while (camaSprite.Frame < 5)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        spritePlayer.Play("idle");
        spritePlayer.Frame = 0;
        player.Visible = true;
        await ToSignal(camaSprite, AnimatedSprite2D.SignalName.AnimationFinished);

        player.Velocity = Vector2.Zero;
        player.SetPhysicsProcess(true);
        while (!player.IsOnFloor())
        {
            player.Velocity = new Vector2(player.Velocity.X, 0.0f);
            player.MoveAndSlide();
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }

        player.bloqueio_animacao = false;
    }
}
