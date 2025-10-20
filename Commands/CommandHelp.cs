
public class CommandHelp : Command {

	public override string Name => "help";
	public override string ExampleUsage => "help";

	public CommandHelp() { }

	public override void Execute(string[] args) {
		string commandsStr = "";

		foreach (Command c in ConsoleCommandProcessor.Instance.GetRegisteredCommands()) {
			commandsStr += " /" + c.Name;
		}

		ConsoleController.Instance.AddMessage("Available commands:" + commandsStr);
	}

}
