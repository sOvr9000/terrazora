
public class MappedNoiseLayer : NoiseLayer {
	public MappedNoiseLayer(
		float scaleX,
		float scaleY,
		float offsetX,
		float offsetY,
		float outputMax = 1f,
		float outputMin = 0f,
		float rotation = 0f,
		int octaves = 1
	) : base(
		scaleX,
		scaleY,
		offsetX,
		offsetY,
		outputScale: outputMax - outputMin,
		outputOffset: outputMin,
		rotation: rotation,
		octaves: octaves
	) {}
}
