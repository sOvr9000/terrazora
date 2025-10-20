
using System;
using System.IO;
using System.Collections;
using UnityEngine;

public class SaveDecoder {
	private const int MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

	private FileStream stream;
	private BinaryReader reader;
	private BitArray bitArray;

	public SaveDecoder(string fpath) {
		string saveDir = Path.Join(Application.persistentDataPath, "saves");
		TZLib.TryCreateDirectory(saveDir);

		// Prevent directory traversal attempts
		fpath = Path.GetFileName(fpath);

		string filePath = Path.Join(saveDir, fpath);

		stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
		reader = new BinaryReader(stream);
	}

	public void Deserialize() {
		if (reader.BaseStream.Length > MAX_FILE_SIZE)
			throw new InvalidOperationException("Save file is too large");

		byte[] bytes = reader.ReadBytes((int) reader.BaseStream.Length);
		bitArray = BitArray.FromCompressed(bytes);
	}

	public IEnumerator Decode(CaveSystem caveSystem) {
		yield return caveSystem.LoadBitArray(bitArray);
	}

	public void Close() {
		stream.Close();
	}
}
