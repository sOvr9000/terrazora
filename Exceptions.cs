using System;

public class InvalidGameSaveException : Exception {
	public InvalidGameSaveException(string message) : base(message) { }
}

public class GameSaveDataTooSmallException : InvalidGameSaveException {
	public GameSaveDataTooSmallException(string message) : base("Game save binary data is too short: " + message) { }
	public GameSaveDataTooSmallException() : base("Game save binary data is too short") { }
}
