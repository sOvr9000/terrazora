using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Static utility class for writing custom log files with timestamps.
/// Thread-safe for multiplayer environments.
/// </summary>
public static class Logger {
	private static string logFilePath;
	private static readonly object fileLock = new object();
	private static bool isInitialized = false;
	private static bool alsoLogToConsole = true;

	/// <summary>
	/// Initialize the logger with a custom log file name.
	/// Call this once at the start of your application.
	/// </summary>
	/// <param name="fileName">Name of the log file (default: "custom_log.txt")</param>
	/// <param name="logToConsole">Whether to also output to Unity console (default: true)</param>
	public static void Initialize(string fileName = "current.log", bool logToConsole = false) {
		if (isInitialized) return;

		alsoLogToConsole = logToConsole;

		// Store logs in the persistent data path (safe across platforms)
		string directory = Application.persistentDataPath;
		logFilePath = Path.Combine(directory, fileName);

		// Create or clear the log file
		try {
			lock (fileLock) {
				File.WriteAllText(logFilePath, $"=== Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n");
				isInitialized = true;
			}
			//Debug.Log($"CustomLogger initialized. Log file: {logFilePath}");
		} catch (Exception e) {
			Debug.LogError($"Failed to initialize CustomLogger: {e.Message}");
		}
	}

	/// <summary>
	/// Log a standard info message.
	/// </summary>
	public static void Log(string message,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "") {
		WriteToFile("INFO", message, filePath, lineNumber, memberName);
		if (alsoLogToConsole) {
			Debug.Log(message);
		}
	}

	/// <summary>
	/// Log a warning message.
	/// </summary>
	public static void LogWarning(string message,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "") {
		WriteToFile("WARNING", message, filePath, lineNumber, memberName);
		if (alsoLogToConsole) {
			Debug.LogWarning(message);
		}
	}

	/// <summary>
	/// Log an error message.
	/// </summary>
	public static void LogError(string message,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "") {
		WriteToFile("ERROR", message, filePath, lineNumber, memberName);
		if (alsoLogToConsole) {
			Debug.LogError(message);
		}
	}

	/// <summary>
	/// Open the log file in the default text editor.
	/// </summary>
	public static void OpenLogFile() {
		if (!isInitialized) {
			Debug.LogWarning("CustomLogger not initialized!");
			return;
		}

		try {
			System.Diagnostics.Process.Start(logFilePath);
		} catch (Exception e) {
			Debug.LogError($"Failed to open log file: {e.Message}");
		}
	}

	/// <summary>
	/// Get the full path to the log file.
	/// </summary>
	public static string GetLogFilePath() {
		return logFilePath;
	}

	// Internal method for thread-safe file writing
	private static void WriteToFile(string level, string message, string filePath, int lineNumber, string memberName) {
		if (!isInitialized) {
			Debug.LogWarning("CustomLogger not initialized! Call CustomLogger.Initialize() first.");
			return;
		}

		// Convert absolute path to relative path (remove everything before and including "Assets/")
		string relativePath = filePath;
		int assetsIndex = filePath.IndexOf("Assets" + Path.DirectorySeparatorChar);
		if (assetsIndex >= 0) {
			relativePath = filePath.Substring(assetsIndex + 7); // 7 = "Assets/".Length
		}

		string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
		string callerInfo = $"{relativePath}:{lineNumber}";
		string logEntry = $"[{timestamp}] [{level}] [{callerInfo}] {message}\n";

		try {
			lock (fileLock) {
				File.AppendAllText(logFilePath, logEntry);
			}
		} catch (Exception e) {
			Debug.LogError($"Failed to write to log file: {e.Message}");
		}
	}
}