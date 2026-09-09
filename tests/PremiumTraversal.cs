using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class PremiumTraversal : Node
{
    private int _failures;
    private bool _combat;
    private static System.Collections.Generic.IEnumerable<Node> Descendants(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            yield return child;
            foreach(Node nested in Descendants(child)) yield return nested;
        }
    }
    private static void Release()
    {
        foreach (string a in new[] { "left", "right", "up", "down", "jump", "dash", "attack", "interact" }) Input.ActionRelease(a);
    }
    private void Prepare(Node node)
    {
        if (!_combat && node is Caveira or InimigoVoador or Rato) node.ProcessMode = ProcessModeEnum.Disabled;
        if (!_combat && node is Espinho hazard) hazard.SetDeferred(Area2D.PropertyName.Monitoring, false);
        foreach (Node child in node.GetChildren()) Prepare(child);
    }
    public override async void _Ready()
    {
        _combat = Array.Exists(OS.GetCmdlineUserArgs(), s => s == "combat");
        bool secrets = Array.Exists(OS.GetCmdlineUserArgs(), s => s == "secrets");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Node bootstrap = new(); GetTree().Root.AddChild(bootstrap); GetTree().CurrentScene = bootstrap;
        var world = GetNode<Global>("/root/Global");
        foreach (string room in new[] { "topo_predio", "bury_the_chaos", "predio", "quinto_andar", "profundezas", "portao_king_rat" })
        {
            if(secrets && room=="topo_predio") continue;
            string? selected = Array.Find(OS.GetCmdlineUserArgs(), x => x.StartsWith("room="));
            if (selected is not null && selected != "room="+room) continue;
            if (room != "topo_predio") { world.SetWorldFlag("sword"); world.SetWorldFlag("dash_lesson"); }
            Release(); GetTree().ChangeSceneToFile($"res://scene/{room}.tscn");
            await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);
            Node scene = GetTree().CurrentScene;
            Prepare(scene);
            Player player = scene.GetNode<Player>("bury");
            var points = new List<Vector2>();
            foreach (Marker2D marker in scene.GetNode("ReviewRoute").GetChildren()) points.Add(marker.GlobalPosition);
            string rewardName=room=="profundezas"?"PumpFuse":room=="portao_king_rat"?"OldInsignia":"";
            int coinsBefore=world.moedas;
            if(secrets)
            {
                int row=room=="profundezas"?2:room=="portao_king_rat"?1:0;
                var step=scene.GetNode<Node2D>("Gameplay/BranchStep"+row);
                player.GlobalPosition=step.GlobalPosition+new Vector2(-96,10); player.Velocity=Vector2.Zero;
                points.Clear();
                foreach(string name in new[]{"BranchStep","BranchStepHigh","UpperGallery"})
                    points.Add(scene.GetNode<Node2D>("Gameplay/"+name+row).GlobalPosition-new Vector2(0,14));
                if(rewardName.Length>0)points.Add(scene.GetNode<Node2D>("Gameplay/"+rewardName).GlobalPosition+new Vector2(0,8));
            }
            int index = 0, stuck = 0, jumpFrame = -100, strikeFrame = -100; Vector2 last = player.GlobalPosition;
            for (int frame = 0; frame < 22000 && index < points.Count; frame++)
            {
                if (GetTree().CurrentScene != scene || !IsInstanceValid(player)) { GD.Print("TRAVERSE_RELOAD " + room); break; }
                Vector2 target = points[index];
                Node2D? opponent = null;
                if (_combat)
                    foreach (Node node in Descendants(scene.GetNode("Gameplay")))
                        if (node is Node2D enemy && node is Caveira or InimigoVoador && enemy.Modulate.A>.9f && Mathf.Abs(enemy.GlobalPosition.Y-player.GlobalPosition.Y)<80 && enemy.GlobalPosition.DistanceTo(player.GlobalPosition)<96)
                        { opponent = enemy; break; }
                bool retreating = false;
                if (_combat && player.vida_atual<=2 && opponent is null)
                {
                    WorldInteraction? nearest = null;
                    foreach(Node node in scene.GetNode("Gameplay").GetChildren())
                        if(node is WorldInteraction { RestOnInteract:true } bench && Mathf.Abs(bench.GlobalPosition.Y-player.Position.Y)<24 &&
                           (nearest is null || bench.GlobalPosition.DistanceTo(player.Position)<nearest.GlobalPosition.DistanceTo(player.Position))) nearest=bench;
                    if(nearest is not null) { target=nearest.GlobalPosition+new Vector2(0,8); retreating=true; }
                }
                float dx = target.X - player.GlobalPosition.X;
                if (opponent is not null) dx = opponent.GlobalPosition.X-player.GlobalPosition.X;
                Input.ActionRelease("left"); Input.ActionRelease("right"); Input.ActionRelease("up"); Input.ActionRelease("down");
                if (Mathf.Abs(dx) > 4) Input.ActionPress(dx > 0 ? "right" : "left");
                // Jump only at obstruction / uphill / tutorial gap, so a descent doesn't cling to walls.
                float direction = Mathf.Sign(dx);
                var ray = PhysicsRayQueryParameters2D.Create(player.GlobalPosition + new Vector2(direction*7,-7), player.GlobalPosition + new Vector2(direction*28,-7), 1);
                ray.Exclude = new Godot.Collections.Array<Rid> { player.GetRid() };
                bool obstacle = player.GetWorld2D().DirectSpaceState.IntersectRay(ray).Count > 0;
                var floorRay = PhysicsRayQueryParameters2D.Create(player.GlobalPosition + new Vector2(direction*20,0), player.GlobalPosition + new Vector2(direction*20,26), 1);
                floorRay.Exclude = ray.Exclude;
                bool gap = player.GetWorld2D().DirectSpaceState.IntersectRay(floorRay).Count == 0 && target.Y < player.Position.Y+22;
                bool needJump = (obstacle && opponent is null) || gap || target.Y < player.Position.Y - 18;
                if (_combat)
                    foreach(Node n in Descendants(scene.GetNode("Gameplay")))
                        if(n is Espinho spike && Mathf.Abs(spike.GlobalPosition.Y-player.GlobalPosition.Y)<16 &&
                           (spike.GlobalPosition.X-player.GlobalPosition.X)*direction is >0 and <36)
                            needJump=true;
                if (opponent is not null && Mathf.Abs(dx)<52) needJump = true;
                if (room == "topo_predio" && player.Position.X > 692 && player.Position.X < 746 && player.Position.Y > 285)
                {
                    needJump = false;
                    if (frame % 60 == 0) Input.ActionPress("dash");
                    if (frame % 60 == 1) Input.ActionRelease("dash");
                }
                if (needJump && player.IsOnFloor() && frame-jumpFrame>32) { Input.ActionPress("jump"); jumpFrame=frame; }
                if (frame-jumpFrame == 18) Input.ActionRelease("jump");
                if (frame-jumpFrame == 24 && target.Y < player.Position.Y-20 && !player.IsOnFloor()) Input.ActionPress("jump");
                if (frame-jumpFrame == 40) Input.ActionRelease("jump");
                if (room == "topo_predio" && frame-jumpFrame == 16 && !player.IsOnFloor() && player.Position.X>760) Input.ActionPress("dash");
                if (frame-jumpFrame == 17) Input.ActionRelease("dash");
                float enemyBelow = opponent is null ? 0 : opponent.GlobalPosition.Y-player.GlobalPosition.Y;
                bool canStrike = opponent is not null && Mathf.Abs(dx)<14 &&
                    ((player.IsOnFloor() && Mathf.Abs(enemyBelow)<18) || (!player.IsOnFloor() && player.Velocity.Y>0 && enemyBelow>12 && enemyBelow<42));
                if (opponent is not null && opponent.GlobalPosition.Y>player.GlobalPosition.Y+5 && !player.IsOnFloor()) Input.ActionPress("down");
                if (_combat && canStrike && frame-strikeFrame>18)
                {
                    Input.ActionPress("attack");
                    strikeFrame=frame;
                    if (Array.Exists(OS.GetCmdlineUserArgs(), s=>s=="trace"))
                    {
                        object? hp=opponent!.GetType().GetField("_vida",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.GetValue(opponent);
                        GD.Print($"COMBAT_SWING p={player.Position} target={opponent.GlobalPosition} enemyHP={hp} down={Input.IsActionPressed("down")} floor={player.IsOnFloor()}");
                    }
                }
                if (frame-strikeFrame == 1) Input.ActionRelease("attack");
                if (_combat)
                    foreach (Node n in scene.GetNode("Gameplay").GetChildren())
                        if (n is WorldInteraction { RestOnInteract: true } rest && rest.GlobalPosition.DistanceTo(player.GlobalPosition) < 28)
                            Input.ParseInputEvent(new InputEventAction { Action="interact", Pressed=true });
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                if (!IsInstanceValid(player) || GetTree().CurrentScene != scene) break;
                float targetTolerance = secrets && rewardName.Length>0 && index==points.Count-1 ? 8 : 22;
                if (!retreating && Mathf.Abs(target.X-player.Position.X)<targetTolerance && Mathf.Abs(target.Y-player.Position.Y)<22) index++;
                if (frame % 240 == 0)
                {
                    GD.Print($"TRAVERSE_PROGRESS {room} point={index}/{points.Count} pos={player.Position} hp={player.vida_atual}");
                    stuck = player.Position.DistanceTo(last)<4 ? stuck+1 : 0; last=player.Position;
                }
                if (stuck > 3) break;
            }
            Release();
            bool passed = index == points.Count;
            if(secrets && passed)
            {
                if(rewardName.Length>0)
                {
                    for(int f=0;f<4;f++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
                    Input.ParseInputEvent(new InputEventAction {Action="interact",Pressed=true});
                    await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
                    passed=world.HasWorldFlag(room=="profundezas"?"pump_fuse":"sigil_sewer");
                }
                else passed=world.moedas>coinsBefore;
                GD.Print($"SECRET_ROUTE {room} passed={passed} reward={rewardName}");
            }
            GD.Print($"TRAVERSE_{(passed ? "PASS" : "FAIL")} {room} point={index}/{points.Count} combat={_combat}");
            if (!passed) _failures++;
        }
        Release(); GD.Print($"TRAVERSE_RESULT failures={_failures} combat={_combat}");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }
}
