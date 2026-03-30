using UnityEngine;

public class BeltVertex {
	public readonly int layerDepth;
	public readonly Vector3Int position;
	public readonly Direction direction;
	public int beltId;
	public ushort distFromEnd;

	public BeltVertex(int layerDepth, Vector3Int position, Direction direction) {
		this.layerDepth = layerDepth;
		this.position = position;
		this.direction = direction;
		beltId = -1;
		distFromEnd = 0;
	}
}
