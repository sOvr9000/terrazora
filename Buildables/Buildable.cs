

public abstract class Buildable {

	public readonly BuildableType type;

	public CaveSystemLayer caveSystemLayer { get; private set; }

	public Buildable(BuildableType type) {
		this.type = type;
	}

	public void SetCaveSystemLayer(CaveSystemLayer caveSystemLayer) {
		this.caveSystemLayer = caveSystemLayer;
	}
}

