
public class BeltItem {
	public ItemType type;
	public ushort distToNext;

	public BeltItem(ItemType type, ushort distToNext) {
		this.type = type;
		this.distToNext = distToNext;
	}

	public override string ToString() {
		return $"[({distToNext}) {type}]";
	}
}
