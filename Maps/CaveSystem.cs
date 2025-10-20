
using System.Collections;
using System.Collections.Generic;

public class CaveSystem {
	public Dictionary<int, CaveSystemLayer> layers;

	public CaveSystem() {
		layers = new();
	}

	public CaveSystemLayer CreateLayer(int depth) {
		CaveSystemLayer layer = new(this, depth, MapGeneratorStorage.GetMapGenerator("basic"));
		return layer;
	}

	public void AddLayer(CaveSystemLayer layer) {

		layers.Add(layer.depth, layer);

		Events.TriggerCaveSystemLayerAdding(layer);
		Events.TriggerCaveSystemLayerAdded(layer);
	}

	public CaveSystemLayer GetLayer(int depth) {
		CaveSystemLayer layer;
		if (layers.TryGetValue(depth, out layer)) {
			return layer;
		}
		throw new System.Exception("Could not find layer at depth " + depth);
	}

	public IEnumerator LoadBitArray(BitArray bitArray) {
		// Read header
		if (!bitArray.HasNextByte())
			throw new GameSaveDataTooSmallException("No numLayers");
		byte numLayers = bitArray.GetNextByte();
		
		for (int i = 0; i < numLayers; i++) {
			yield return LoadCaveSystemLayer(bitArray);
		}
	}

	private IEnumerator LoadCaveSystemLayer(BitArray bitArray) {
		CaveSystemLayer layer = new(this);
		yield return layer.LoadBitArray(bitArray);
		AddLayer(layer);
	}
}
