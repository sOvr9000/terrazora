
using UnityEngine;

public static class Constants {
	public const int CHUNK_SIZE = 16;
	public const int NUM_CHUNKS_PER_AXIS = 32;
	public const int TERRAIN_SIZE = CHUNK_SIZE * NUM_CHUNKS_PER_AXIS;
	public const float PHI = 1.618033988749894848204f;
	public const float NOISE_ROTATION = Mathf.PI / PHI;
	public const float CAVE_WALL_THRESHOLD = 0.42f;

	public const int CONSOLE_MESSAGE_DISPLAY_LIFE = 300;

	public const ushort BELT_SPATIAL_RESOLUTION = 512;
	public const ushort ITEM_SIZE_ON_BELT = BELT_SPATIAL_RESOLUTION / 2;
}
