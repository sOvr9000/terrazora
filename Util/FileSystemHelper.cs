using System.Diagnostics;
using System.IO;

public static class FileSystemHelper {
	/// <summary>
	/// Opens the specified directory in the system's file explorer
	/// </summary>
	public static void OpenDirectory(string path, bool allowCreateDir = false) {
		// Make sure the path exists
		if (!Directory.Exists(path)) {
			if (allowCreateDir) {
				TZLib.TryCreateDirectory(path);
			} else {
				UnityEngine.Debug.LogWarning($"Directory does not exist: {path}");
				return;
			}
		}

		// Normalize the path for the current platform
		path = Path.GetFullPath(path);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		// Windows: Use explorer
		Process.Start("explorer.exe", path);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        // macOS: Use open command
        Process.Start("open", path);
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        // Linux: Use xdg-open
        Process.Start("xdg-open", path);
#else
        UnityEngine.Debug.LogWarning("Platform not supported for opening directories");
#endif
	}
}
