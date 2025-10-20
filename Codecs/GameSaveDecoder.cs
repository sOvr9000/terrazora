
using System.IO;
using UnityEngine;

public class GameSaveDecoder {

	private FileStream stream;
	private BinaryReader reader;

	public GameSaveDecoder(string gameSaveName) {
		string saveDir = Path.Join(Application.persistentDataPath, "saves");
		TZLib.TryCreateDirectory(saveDir);

		string filePath = Path.Join(saveDir, gameSaveName + ".dat");

		stream = new FileStream(filePath, FileMode.Open);
		reader = new BinaryReader(stream);
	}

	public BitArray Deserialize() {
		byte[] compressed = reader.ReadBytes((int) stream.Length);
		return BitArray.FromCompressed(compressed);
	}

	public void Close() {
		stream.Close();
	}

}