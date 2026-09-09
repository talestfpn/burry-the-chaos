using Godot;

public partial class PremiumPrologue : Node
{
    public override async void _Ready()
    {
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Node boot=new();GetTree().Root.AddChild(boot);GetTree().CurrentScene=boot;
        GetTree().ChangeSceneToFile("res://scene/quarto.tscn");
        await ToSignal(GetTree(),SceneTree.SignalName.SceneChanged);
        await ToSignal(GetTree().CreateTimer(4),SceneTreeTimer.SignalName.Timeout);
        bool bedroom=false,street=false,passed=false;
        for(int frame=0;frame<6000;frame++)
        {
            Node room=GetTree().CurrentScene;
            Player? player=room?.GetNodeOrNull<Player>("bury");
            if(player is null){await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);continue;}
            if(room!.Name=="topo_predio") {passed=bedroom&&street;break;}
            if(player.pode_atacar||player.pode_pular||player.pode_dash){GD.PushError("PROLOGUE_ABILITY_LEAK");break;}
            bool upstairs=room.Name=="Quarto" && player.Position.Y<140;
            bedroom |= room.Name=="Quarto";street |= room.Name=="Ruacasa";
            Input.ActionRelease("left");Input.ActionRelease("right");
            Input.ActionPress(upstairs?"left":"right");
            if(upstairs && player.Position.X<36 && frame%90==0)
                Input.ParseInputEvent(new InputEventAction {Action="interact",Pressed=true});
            if(frame%300==0)GD.Print($"PROLOGUE_PROGRESS {room.Name} {player.Position}");
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        }
        Input.ActionRelease("left");Input.ActionRelease("right");
        GD.Print($"PROLOGUE_RESULT pass={passed} bedroom={bedroom} street={street}");
        GetTree().Quit(passed?0:1);
    }
}
