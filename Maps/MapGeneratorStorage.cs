
using System;
using System.Collections.Generic;

public static class MapGeneratorStorage {
	private readonly static Dictionary<string, MapGenerator> mapGenerators = new();

	static MapGeneratorStorage() {
		Load();
	}

	static void Load() {
		Dictionary<TileType, Tuple<NoiseLayer, float>> oreNoiseLayers = new();

		oreNoiseLayers.Add(TileType.HeavyMetalOre, new Tuple<NoiseLayer, float>
			(
				new MappedNoiseLayer(0.04546f, 0.04546f, -618f, -618f, outputMax: 1f, outputMin: 0f, rotation: Constants.NOISE_ROTATION, octaves: 1),
				0.85f
			)
		);
		oreNoiseLayers.Add(TileType.LightMetalOre, new Tuple<NoiseLayer, float>
			(
				new MappedNoiseLayer(0.04546f, 0.04546f, 1618f, 1618f, outputMax: 1f, outputMin: 0f, rotation: Constants.NOISE_ROTATION, octaves: 1),
				0.85f
			)
		);

		mapGenerators.Add("basic", new(
			new MappedNoiseLayer(0.0314159625358979f * 1.5f, 0.0314159625358979f * 1.5f, 618f, 618f, outputMax: 1f, outputMin: 0f, rotation: Constants.NOISE_ROTATION, octaves: 2),
			new MappedNoiseLayer(0.0114159625358979f * 1.5f, 0.0114159625358979f * 1.5f, -618f, 618f, outputMax: 16f, outputMin: 0f, rotation: -Constants.NOISE_ROTATION, octaves: 5),
			0.38f,
			oreNoiseLayers
		));
	}

	public static MapGenerator GetMapGenerator(string name) {
		MapGenerator mg;
		if (mapGenerators.TryGetValue(name, out mg)) {
			return mg;
		}
		throw new Exception("MapGenerator not found for " + name);
	}
}
