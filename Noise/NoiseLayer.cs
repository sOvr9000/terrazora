
using UnityEngine;

public class NoiseLayer {
	public readonly float scaleX, scaleY, offsetX, offsetY, outputScale, outputOffset, rotation;
	public readonly int octaves;

	public NoiseLayer(float scaleX, float scaleY, float offsetX, float offsetY, float outputScale = 1f, float outputOffset = 0f, float rotation = 0f, int octaves = 1) {
		this.scaleX = scaleX;
		this.scaleY = scaleY;
		this.offsetX = offsetX;
		this.offsetY = offsetY;
		this.outputScale = outputScale;
		this.outputOffset = outputOffset;
		this.rotation = rotation;
		this.octaves = octaves;
	}

	public float Noise(Vector2 point) {
		Vector2 p = point;
		if (rotation != 0f) {
			p = TZLib.RotatedPoint(point, rotation);
		}
		return TZLib.Noise(new(p.x * scaleX + offsetX, p.y * scaleY + offsetY), octaves) * outputScale + outputOffset;
	}
}
