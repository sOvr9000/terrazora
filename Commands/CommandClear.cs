
public class CommandClear : Command {

	public override string Name => "clear";
	public override string ExampleUsage => "clear";

	public CommandClear() { }

	public override void Execute(string[] args) {
		ConsoleController.Instance.ClearHistory();
	}

}
