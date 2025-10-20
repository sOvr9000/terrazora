using System;
using System.Collections;
using UnityEngine;

public class CaveSystemLayer {
	public int depth;
	private int[,] heightMap;
	private TileType[,] tileTypes;

	public readonly CaveSystem caveSystem;
	public readonly MapGenerator mapGenerator;

	public CaveSystemLayer(CaveSystem caveSystem, int depth, MapGenerator mapGenerator) {
		this.caveSystem = caveSystem;
		this.depth = depth;
		this.mapGenerator = mapGenerator;

		InitArrays();
	}

	public CaveSystemLayer(CaveSystem caveSystem) {
		this.caveSystem = caveSystem;

		InitArrays();
	}

	private void InitArrays() {
		heightMap = new int[Constants.TERRAIN_SIZE, Constants.TERRAIN_SIZE];
		tileTypes = new TileType[Constants.TERRAIN_SIZE, Constants.TERRAIN_SIZE];
	}

	public IEnumerator Generate(int seed) {
		mapGenerator.SetSeed(seed);

		for (int x = 0; x < Constants.TERRAIN_SIZE; x++) {
			for (int y = 0; y < Constants.TERRAIN_SIZE; y++) {
				Vector2Int pos = new(x, y);

				int height = mapGenerator.GenerateHeight(pos);
				heightMap[x, y] = height;
				TileType tile = mapGenerator.GenerateTileType(pos);
				tileTypes[x, y] = tile;
			}

			if (x % 100 == 0) {
				float progress = x / (float) Constants.TERRAIN_SIZE;
				Events.TriggerCaveSystemLayerGenerationProgress(this, progress);
				yield return null;
			}
		}

		Events.TriggerCaveSystemLayerGenerated(this);
	}

	public IEnumerator LoadBitArray(BitArray bitArray) {
		if (!bitArray.HasNextByte())
			throw new GameSaveDataTooSmallException("No depth");
		depth = bitArray.GetNextByte();

		if (!bitArray.HasNextShort())
			throw new GameSaveDataTooSmallException("No sizeX");
		short sizeX = bitArray.GetNextShort();

		if (!bitArray.HasNextShort())
			throw new GameSaveDataTooSmallException("No sizeY");
		short sizeY = bitArray.GetNextShort();

		heightMap = new int[sizeX, sizeY];
		tileTypes = new TileType[sizeX, sizeY];

		if (!bitArray.HasNextBits(sizeX * sizeY * 2, 0, 0, 0))
			throw new GameSaveDataTooSmallException("Not enough terrain data for layer at depth " + depth);

		for (int x = 0; x < sizeX; x++) {
			for (int y = 0; y < sizeY; y++) {
				int height = bitArray.GetNextByte();
				TileType tile = (TileType) bitArray.GetNextByte();

				//if (x == 0 && y == 0) {
				//	Debug.Log(height);
				//	Debug.Log((byte) height);
				//	Debug.Log(tile);
				//	Debug.Log((byte) tile);
				//}

				heightMap[x, y] = height;
				tileTypes[x, y] = tile;
			}

			if(x % 100 == 0) {
				float progress = x / (float) Constants.TERRAIN_SIZE;
				Events.TriggerCaveSystemLayerLoadProgress(this, progress);
				yield return null;
			}
		}

		Events.TriggerCaveSystemLayerLoaded(this);
	}

	public TileType GetTileType(Vector2Int pos) {
		if (!TZLib.PointIsInside(new(0, 0), new(Constants.TERRAIN_SIZE, Constants.TERRAIN_SIZE), pos)) return TileType.Wall;
		return tileTypes[pos.x, pos.y];
	}

	public int GetTotalWidth() {
		return heightMap.GetLength(0);
	}

	public int GetTotalHeight() {
		return heightMap.GetLength(1);
	}

	public bool SetTileType(Vector2Int pos, TileType type) {
		if (!TZLib.PointIsInside(new(0, 0), new(Constants.TERRAIN_SIZE, Constants.TERRAIN_SIZE), pos)) return false;
		TileType prev = tileTypes[pos.x, pos.y];
		tileTypes[pos.x, pos.y] = type;
		return prev != type;
	}

	public int GetTileHeight(Vector2Int pos) {
		if (!TZLib.PointIsInside(new(0, 0), new(Constants.TERRAIN_SIZE, Constants.TERRAIN_SIZE), pos)) return 0;
		return heightMap[pos.x, pos.y];
	}

	public bool SetTileHeight(Vector2Int pos, int height) {
		if (!TZLib.PointIsInside(new(0, 0), new(Constants.TERRAIN_SIZE, Constants.TERRAIN_SIZE), pos)) return false;
		int prev = heightMap[pos.x, pos.y];
		heightMap[pos.x, pos.y] = height;
		return prev != height;
	}

	public Vector2Int FindGroundTilePosition(Vector2Int startPos, int maxRange = 32) {
		Vector2Int p;
		int x, y;

		for (int n = 1; n < maxRange; n++) {
			y = startPos.y + n;
			int maxX = startPos.x + n;
			for (int i = 0; i < 2; i++) {
				for (x = startPos.x - n; x <= maxX; x++) {
					p = new(x, y);
					TileType tileType = GetTileType(p);
					TilePrototype prot = TilePrototypeStorage.GetPrototype(tileType);
					if (!prot.IsSolid) {
						return p;
					}
				}
				y -= n + n;
			}

			int maxY = startPos.y + n - 1;
			x = startPos.x + n;
			for (int i = 0; i < 2; i++) {
				for (y = startPos.y - n + 1; y <= maxY; y++) {
					p = new(x, y);
					TileType tileType = GetTileType(p);
					TilePrototype prot = TilePrototypeStorage.GetPrototype(tileType);
					if (!prot.IsSolid) {
						return p;
					}
				}
				x -= n + n;
			}
		}

		Debug.LogError("Could not find a ground tile position");
		return startPos;
	}

	public void TrySetPositionOutsideOfWall(ref Vector2 pos, int maxRange = 32) {
		Vector2Int tilePos = TZLib.FloorVector2(pos);
		TileType tile = GetTileType(tilePos);
		if (tile == TileType.Wall) {
			pos = FindGroundTilePosition(tilePos, maxRange);
			pos += Vector2.one * 0.5f;
		}
	}
}
