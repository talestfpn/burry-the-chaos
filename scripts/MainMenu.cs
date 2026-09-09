using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("%StartButton").GrabFocus();
    }

    public void _on_start_game_pressed()
    {
        Global? gameManager = GetNodeOrNull<Global>("/root/Global");
        if (gameManager is not null)
            gameManager.iniciar_novo_jogo();
        else
            GetTree().ChangeSceneToFile(Global.CenaInicial);
    }

    public void _on_quit_pressed()
    {
        GetTree().Quit();
    }
}
