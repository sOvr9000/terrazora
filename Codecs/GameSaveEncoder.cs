
using System;
using System.Collections;
using System.IO;
using UnityEngine;


public class GameSaveEncoder {
	private FileStream stream;
	private BinaryWriter writer;
	private BitArray bitArray;

	public GameSaveEncoder(string gameSaveName) {
		string saveDir = Path.Join(Application.persistentDataPath, "saves");
		TZLib.TryCreateDirectory(saveDir);

		string filePath = Path.Join(saveDir, gameSaveName + ".dat");

		stream = new FileStream(filePath, FileMode.Create);
		writer = new BinaryWriter(stream);
	}

	public void InitBitArray(CaveSystem caveSystem) {
		int totalBits = 0;

		// Number of cave system layers
		totalBits += 8;

		// Info for each cave system layer
		foreach (CaveSystemLayer layer in caveSystem.layers.Values) {
			// Header (depth)
			totalBits += 8;

			// Header (dimensions)
			totalBits += 32;

			// Header (depth)
			totalBits += 8;

			// Body
			totalBits += layer.GetTotalWidth() * layer.GetTotalHeight() * 16;
		}

		bitArray = new BitArray(totalBits);
	}

	public void Serialize() {
		byte[] compressed = bitArray.ToCompressed();
		writer.Write(compressed);
	}

	public void Close() {
		stream.Close();
	}

	public IEnumerator Encode(CaveSystem caveSystem) {
		yield return WriteCaveSystem(caveSystem);
	}

	public IEnumerator WriteCaveSystem(CaveSystem caveSystem) {
		// Write the number of layers
		bitArray.SetByte((byte) caveSystem.layers.Count);

		// Write each layer sequentially
		foreach (CaveSystemLayer layer in caveSystem.layers.Values) {
			yield return WriteCaveSystemLayer(layer);
		}
	}

	public IEnumerator WriteCaveSystemLayer(CaveSystemLayer layer) {
		// Write the dimensions of the layer
		bitArray.SetByte((byte) layer.depth);
		bitArray.SetShort((short) layer.GetTotalWidth());
		bitArray.SetShort((short) layer.GetTotalHeight());

		// Write each tile height and type sequentially
		for (int x = 0; x < layer.GetTotalWidth(); x++) {
			for (int y = 0; y < layer.GetTotalHeight(); y++) {
				Vector2Int pos = new(x, y);
				byte bHeight = (byte) layer.GetTileHeight(pos);
				TileType tileType = layer.GetTileType(pos);

				//if (x == 0 && y == 0) {
				//	Debug.Log(bHeight);
				//	Debug.Log(tileType);
				//	Debug.Log((byte) tileType);
				//}

				byte bTileType = (byte) tileType;

				bitArray.SetByte(bHeight);
				bitArray.SetByte(bTileType);
			}

			if (x % 100 == 0) yield return null;
		}
	}
}
