
using System;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator {
	public readonly NoiseLayer caveWallNoise, heightMapNoise;
	public readonly float caveWallThreshold;
	public readonly Dictionary<TileType, Tuple<NoiseLayer, float>> oreNoiseLayers;

	private System.Random rng;
	private Vector2Int caveWallNoiseShift, heightMapNoiseShift;

	public MapGenerator(NoiseLayer caveWallNoise, NoiseLayer heightMapNoise, float caveWallThreshold, Dictionary<TileType, Tuple<NoiseLayer, float>> oreNoiseLayers) {
		this.caveWallNoise = caveWallNoise;
		this.heightMapNoise = heightMapNoise;
		this.caveWallThreshold = caveWallThreshold;
		this.oreNoiseLayers = oreNoiseLayers;

		SetSeed(0);
	}

	public void SetSeed(int? seed) {
		if (seed.HasValue) {
			rng = new System.Random(seed.Value);
		} else {
			rng = new System.Random();
		}

		caveWallNoiseShift = new(rng.Next(-200000, 200000), rng.Next(-200000, 200000));
		heightMapNoiseShift = new(rng.Next(-200000, 200000), rng.Next(-200000, 200000));
	}

	public void GenerateBiome(Vector2Int tilePos) {
		// TODO
	}

	public int GenerateHeight(Vector2Int tilePos) {
		Vector2Int shiftedPos = tilePos - heightMapNoiseShift;
		return Mathf.RoundToInt(heightMapNoise.Noise(shiftedPos));
	}

	public TileType GenerateTileType(in Vector2Int tilePos) {
		Vector2Int shiftedPos = tilePos - caveWallNoiseShift;
		float tileNoise = caveWallNoise.Noise(shiftedPos);
		if (tileNoise < caveWallThreshold) {
			return GenerateWallTileType(shiftedPos);
		}
		return GenerateGroundTileType(shiftedPos);
	}

	private TileType GenerateWallTileType(Vector2Int tilePos) {
		TileType type = TileType.Wall;

		foreach (KeyValuePair<TileType, Tuple<NoiseLayer, float>> kvp in oreNoiseLayers) {
			float noise = kvp.Value.Item1.Noise(tilePos);
			if (noise > kvp.Value.Item2) {
				type = kvp.Key;
				break;
			}
		}

		return type;
	}

	private TileType GenerateGroundTileType(Vector2Int tilePos) {
		return TileType.Ground;
	}
}
