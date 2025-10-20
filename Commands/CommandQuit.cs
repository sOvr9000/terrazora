
public class CommandQuit : Command {

	public override string Name => "quit";
	public override string ExampleUsage => "quit";

	public CommandQuit() { }

	public override void Execute(string[] args) {
		GameController.Instance.Quit();
	}

}
