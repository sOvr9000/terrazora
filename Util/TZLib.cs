
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class TZLib {
	public static Vector3 FlatVector2(Vector2 vec) {
		return new Vector3(vec.x, 0f, vec.y);
	}

	public static Vector2Int WorldPosToChunkPos(Vector2 pos) {
		return FloorVector2(pos / Constants.CHUNK_SIZE);
	}

	public static Vector2 ChunkPosToWorldPos(Vector2Int chunkPos) {
		return new(chunkPos.x * Constants.CHUNK_SIZE, chunkPos.y * Constants.CHUNK_SIZE);
	}

	public static void FixCornersMinMax(ref Vector2Int min, ref Vector2Int max) {
		if (min.x > max.x) {
			(max.x, min.x) = (min.x, max.x);
		}
		if (min.y > max.y) {
			(max.y, min.y) = (min.y, max.y);
		}
	}

	public static bool PointIsInside(Vector2Int min, Vector2Int max, Vector2Int point) {
		return point.x < max.x && point.y < max.y && point.x >= min.x && point.y >= min.y;
	}

	/// <summary>
	/// Given two rects, "main" and "overlapping", return all positions that are inside "overlapping" but not inside "main".
	/// </summary>
	/// <param name="minMain"></param>
	/// <param name="maxMain"></param>
	/// <param name="minOverlapping"></param>
	/// <param name="maxOverlapping"></param>
	/// <returns></returns>
	public static Vector2Int[] GetNonOverlappingPositions(Vector2Int minMain, Vector2Int maxMain, Vector2Int minOverlapping, Vector2Int maxOverlapping) {
		FixCornersMinMax(ref minMain, ref maxMain);
		FixCornersMinMax(ref minOverlapping, ref maxOverlapping);

		List<Vector2Int> points = new();
		for (int y = minOverlapping.y; y < maxOverlapping.y; y++) {
			for (int x = minOverlapping.x; x < maxOverlapping.x; x++) {
				Vector2Int point = new(x, y);
				if (!PointIsInside(minMain, maxMain, point)) {
					points.Add(point);
				}
			}
		}

		return points.ToArray();
	}

	public static Vector2 RotatedPoint(Vector2 pos, float angle) {
		float cos = Mathf.Cos(angle);
		float sin = Mathf.Sin(angle);
		return new(pos.x * cos - pos.y * sin, pos.x * sin + pos.y * cos);
	}

	public static float RotatedNoise(Vector2 pos, int octaves = 1) {
		return Noise(RotatedPoint(pos, Constants.NOISE_ROTATION), octaves);
	}

	public static float Noise(Vector2 pos, int octaves = 1) {
		const float outputScale = 0.7838819448706803f;
		const float outputShift = 0.1361203f;

		float noise = 0f;
		if (octaves == 1) {
			noise = (Mathf.PerlinNoise(pos.x, pos.y) + outputShift) * outputScale;
			return Mathf.Max(0f, Mathf.Min(1f, noise));
		}

		float scale = outputScale;
		for (int i = 0; i < octaves; i++) {
			noise += (Mathf.PerlinNoise(pos.x, pos.y) + outputShift) * scale;
			scale *= 0.5f;
		}

		// Remap theoretical max of noise composition to 0-1
		int pow2 = 1 << octaves;
		noise /= 1f + (pow2 - 1f) / pow2;

		return Mathf.Max(0f, Mathf.Min(1f, noise));
	}

	public static Vector2Int FloorVector2(Vector2 pos) {
		return new(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
	}

	public static int L1Distance(Vector2Int p1, Vector2Int p2) {
		return Mathf.Abs(p1.x - p2.x) + Mathf.Abs(p1.y - p2.y);
	}

	/// <summary>
	/// Return whether the directory was newly created by this method.
	/// </summary>
	/// <param name="dirName"></param>
	/// <returns></returns>
	public static bool TryCreateDirectory(string dirName) {
		if (!Directory.Exists(dirName)) {
			Directory.CreateDirectory(dirName);
			return true;
		}
		return false;
	}

	public static string GetFormattedDateTime() {
		return DateTime.Now.ToString("yyyyMMddHHmmss");
	}

	/// <summary>
	/// Return the given number as a string, but pad it on the left and right such that
	/// one space is put on the left for positive values (assuming the possibility of negatives)
	/// and the rest of the spaces are put on the right side.
	/// 
	/// The total length of the returned string is never smaller than <paramref name="width"/>.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="width"></param>
	/// <returns></returns>
	public static string FormatWithSign(int value, int width) {
		if (value < 0) {
			return $"-{Math.Abs(value).ToString().PadRight(width - 1)}";
		} else {
			return $" {value.ToString().PadRight(width - 1)}";
		}
	}
}
