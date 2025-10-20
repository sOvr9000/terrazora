
using System.Collections.Generic;

public static class TilePrototypeStorage {
	private readonly static Dictionary<TileType, TilePrototype> prototypes = new();

	static TilePrototypeStorage() {
		LoadPrototypes();
	}

	static void LoadPrototypes() {
		prototypes.Add(TileType.Wall, new(TileType.Wall, true));
		prototypes.Add(TileType.Ground, new(TileType.Ground, false));

		prototypes.Add(TileType.HeavyMetalOre, new(TileType.HeavyMetalOre, true));
		prototypes.Add(TileType.LightMetalOre, new(TileType.LightMetalOre, true));
	}

	public static TilePrototype GetPrototype(TileType type) {
		TilePrototype prot;
		if (prototypes.TryGetValue(type, out prot)) {
			return prot;
		}
		throw new System.Exception("TilePrototype not found for " + type.ToString());
	}
}

