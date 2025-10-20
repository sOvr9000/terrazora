using System;
using System.Collections.Generic;
using System.Linq;

public class ConsoleCommandProcessor {
	// Dictionary mapping command names to Command objects
	private Dictionary<string, Command> commands;

	public static ConsoleCommandProcessor Instance { get; private set; }

	public ConsoleCommandProcessor() {
		Instance = this;
		commands = new Dictionary<string, Command>();
	}

	public bool TryProcessCommand(string text) {
		if (text.StartsWith('/')) {
			try {
				string commandText = text.Substring(1);
				ProcessCommandText(commandText);
				return true;
			} catch (Exception) {
				return false;
			}
		}
		return false;
	}

	/// <summary>
	/// Processes a raw command text string from the console
	/// </summary>
	/// <param name="text">The full command text including command name and arguments</param>
	public void ProcessCommandText(string text) {
		// Handle empty input
		if (string.IsNullOrWhiteSpace(text)) {
			return;
		}

		// Parse the command and arguments
		string[] parts = ParseCommandText(text);
		string commandName = parts[0].ToLower();
		string[] args = parts.Skip(1).ToArray();

		// Check if command exists
		if (!commands.ContainsKey(commandName)) {
			UnityEngine.Debug.LogWarning($"Unknown command: '{commandName}'");
			return;
		}

		// Execute the command
		try {
			commands[commandName].Execute(args);
		} catch (Exception ex) {
			UnityEngine.Debug.LogError($"Error executing command '{commandName}': {ex.Message}");
		}
	}

	/// <summary>
	/// Parses command text into individual parts, respecting quoted strings
	/// </summary>
	/// <param name="text">The raw command text</param>
	/// <returns>Array of command parts (command name + arguments)</returns>
	private string[] ParseCommandText(string text) {
		List<string> parts = new List<string>();
		bool inQuotes = false;
		string currentPart = "";

		foreach (char c in text) {
			if (c == '"') {
				inQuotes = !inQuotes;
			} else if (c == ' ' && !inQuotes) {
				if (!string.IsNullOrEmpty(currentPart)) {
					parts.Add(currentPart);
					currentPart = "";
				}
			} else {
				currentPart += c;
			}
		}

		// Add the last part if any
		if (!string.IsNullOrEmpty(currentPart)) {
			parts.Add(currentPart);
		}

		return parts.ToArray();
	}

	/// <summary>
	/// Registers a command object with the processor
	/// </summary>
	/// <param name="command">The command object to register</param>
	public void RegisterCommand(Command command) {
		string lowerCommandName = command.Name.ToLower();

		if (commands.ContainsKey(lowerCommandName)) {
			UnityEngine.Debug.LogWarning($"Command '{command.Name}' is already registered. Overwriting.");
		}

		commands[lowerCommandName] = command;
	}

	/// <summary>
	/// Unregisters a command from the processor
	/// </summary>
	/// <param name="commandName">The name of the command to remove</param>
	public void UnregisterCommand(string commandName) {
		commands.Remove(commandName.ToLower());
	}

	/// <summary>
	/// Gets a list of all registered commands
	/// </summary>
	/// <returns>List of command objects</returns>
	public List<Command> GetRegisteredCommands() {
		return new List<Command>(commands.Values);
	}

	/// <summary>
	/// Gets a specific command by name
	/// </summary>
	/// <param name="commandName">The name of the command</param>
	/// <returns>The command object, or null if not found</returns>
	public Command GetCommand(string commandName) {
		commands.TryGetValue(commandName.ToLower(), out Command command);
		return command;
	}
}
