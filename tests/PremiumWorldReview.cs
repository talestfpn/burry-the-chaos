using Godot;
using System;
using System.Collections.Generic;

// Runs the real scenes. Structural checks never stand in for traversal or visual review.
public partial class PremiumWorldReview : Node
{
    [Export] public bool CaptureViews;
    private int _checks, _failures;
    private readonly string[] _rooms = { "quarto", "ruacasa", "topo_predio", "bury_the_chaos", "predio", "quinto_andar", "profundezas", "portao_king_rat", "arena_rei_ratos", "arquivo_sete", "abrigo_silencioso", "sala_inundada", "segredo_telhado" };
    private void Check(bool valid, string label)
    {
        _checks++;
        if (!valid) { _failures++; GD.PushError("PREMIUM_FAIL " + label); }
        else GD.Print("PREMIUM_PASS " + label);
    }
    private IEnumerable<Node> Walk(Node n)
    {
        yield return n;
        foreach (Node child in n.GetChildren()) foreach (Node item in Walk(child)) yield return item;
    }
    public override async void _Ready()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Node bootstrap = new(); GetTree().Root.AddChild(bootstrap); GetTree().CurrentScene = bootstrap;
        var world = GetNode<Global>("/root/Global");
        bool visual = CaptureViews || Array.Exists(OS.GetCmdlineUserArgs(), x => x == "visual");
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://artifacts/premium-review"));
        var positions = new Dictionary<string, Vector2[]>
        {
            ["quarto"] = new[] { new Vector2(196,88) }, ["ruacasa"] = new[] { new Vector2(680,150) },
            ["topo_predio"] = new[] { new Vector2(128,294), new Vector2(760,294), new Vector2(1448,294) },
            ["bury_the_chaos"] = new[] { new Vector2(536,190),new Vector2(1200,430) },
            ["predio"] = new[] { new Vector2(1184,470), new Vector2(544,190) },
            ["quinto_andar"] = new[] { new Vector2(1500,390),new Vector2(2384,850) },
            ["profundezas"] = new[] { new Vector2(2160,800),new Vector2(1200,1230) },
            ["portao_king_rat"] = new[] { new Vector2(1240,710),new Vector2(180,990) },
            ["arena_rei_ratos"] = new[] { new Vector2(768,294) },
            ["arquivo_sete"] = new[] { new Vector2(520,190) },
            ["abrigo_silencioso"] = new[] { new Vector2(240,190) },
            ["sala_inundada"] = new[] { new Vector2(620,190) },
            ["segredo_telhado"] = new[] { new Vector2(330,120) }
        };
        int shot = 0;
        foreach (string room in _rooms)
        {
            string path = $"res://scene/{room}.tscn";
            var packed = ResourceLoader.Load<PackedScene>(path);
            Check(packed is not null, "load " + room);
            if (packed is null) continue;
            if (!visual) using (Node detached = packed.Instantiate())
            {
                Check(detached.GetNodeOrNull<Player>("bury") is not null, "player " + room);
                foreach (Node node in Walk(detached))
                {
                    string dest = node is WorldInteraction a ? a.Destination : node is TransicaoCena b ? b.proxima_cena : "";
                    string spawn = node is WorldInteraction c ? c.DestinationSpawn : node is TransicaoCena d ? d.spawn_destino : "";
                    if (string.IsNullOrEmpty(dest)) continue;
                    var target = ResourceLoader.Load<PackedScene>(dest);
                    Check(target is not null, "edge " + room + "/" + node.Name);
                    if (target is null) continue;
                    using Node targetRoot = target.Instantiate();
                    Check(targetRoot.GetNodeOrNull<Marker2D>("SpawnPoints/" + spawn) is not null, "spawn " + dest + "/" + spawn);
                }
            }
            GetTree().ChangeSceneToFile(path);
            await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);
            await ToSignal(GetTree().CreateTimer(room == "quarto" ? 4 : .3), SceneTreeTimer.SignalName.Timeout);
            var current = GetTree().CurrentScene;
            var player = current.GetNode<Player>("bury");
            if (room is "quarto" or "ruacasa")
                Check(!player.pode_atacar && !player.pode_dash && !player.pode_pular, "prologue restrictions " + room);
            if (room == "topo_predio")
            {
                Check(player.pode_pular && !player.pode_atacar && !player.pode_dash, "rooftop progression");
                world.SetWorldFlag("dash_lesson"); Check(player.pode_dash, "dash unlock");
                if (!visual) { world.SetWorldFlag("sword"); Check(player.pode_atacar, "sword unlock"); }
            }
            if (!visual) continue;
            foreach (Vector2 position in positions[room])
            {
                player.GlobalPosition = position; player.Velocity = Vector2.Zero;
                player.SetPhysicsProcess(false); player.Visible = true;
                player.GetNode<AnimatedSprite2D>("AnimatedSprite2D").Play("idle");
                GetViewport().GetCamera2D().ResetSmoothing();
                await ToSignal(GetTree().CreateTimer(.25), SceneTreeTimer.SignalName.Timeout);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using Image capture = GetViewport().GetTexture().GetImage();
                if (GetViewport().UseHdr2D) { capture.Convert(Image.Format.Rgba8); capture.LinearToSrgb(); }
                Check(capture.SavePng($"res://artifacts/premium-review/{shot++:00}-{room}.png") == Error.Ok, "capture " + room);
            }
            if (room == "topo_predio") { world.SetWorldFlag("sword"); Check(player.pode_atacar, "sword unlock"); }
        }
        GD.Print($"PREMIUM_RESULT checks={_checks} failures={_failures}");
        // Let the rendering server release the final scene before stopping the D3D12 device.
        GetTree().CurrentScene.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }
}
