using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Belt {
	public BeltSystem system;
	public int id, nextBeltId, groupId;
	public ushort nextBeltInsertionPoint;
	public ushort reservedFrontSpace;
	private ushort frontItemIndex;
	public List<BeltItem> allItems;
	public List<int> inputBeltIds;  // Multiple belts can feed into this one
	public ushort length;
	public short rearSpace;

	public readonly ushort speed;

	public Belt(ushort speed) {
		if (speed >= Constants.ITEM_SIZE_ON_BELT) {
			Debug.LogWarning($"Belt speed of {speed} is illegal. Capping at {Constants.ITEM_SIZE_ON_BELT} (item size on belt).");
			speed = Constants.ITEM_SIZE_ON_BELT;
		}
		this.speed = speed;

		nextBeltInsertionPoint = 0;
		reservedFrontSpace = 0;
		frontItemIndex = 0;
		length = 0;
		rearSpace = 0;

		nextBeltId = -1;
		inputBeltIds = new List<int>();

		allItems = new List<BeltItem>();
	}

	public void SetLength(ushort length) {
		this.length = length;
		RecalculateRearSpace();
	}

	public void SetReservedFrontSpace(ushort s) {
		reservedFrontSpace = s;
		RecalculateFrontItem();
	}

	public void RecalculateRearSpace() {
		short space = (short) length;

		foreach (BeltItem item in allItems) {
			int add = item.distToNext + Constants.ITEM_SIZE_ON_BELT;
			space -= (short) add;
		}

		if (space < -Constants.ITEM_SIZE_ON_BELT) {
			throw new System.Exception($"Rear space calculated to be too negative: {space} (limit is {-Constants.ITEM_SIZE_ON_BELT})");
		}

		rearSpace = space;
	}

	#region Single Segment
	public void AppendItem(BeltItem item, ushort? distFromEnd = null) {
		if (distFromEnd.HasValue) {
			ushort d = distFromEnd.Value;

			int totalDist = d + rearSpace;
			if (totalDist < length) {
				throw new System.Exception($"Cannot append item at distFromEnd = {distFromEnd} in belt:\n{ToString()}");
			}

			item.distToNext = (ushort) (totalDist - length);
		} else {
			int distToNext = rearSpace - Constants.ITEM_SIZE_ON_BELT;
			if (distToNext < 0) {
				throw new System.Exception($"Not enough rear space to append item. rearSpace = {rearSpace}");
			}
			item.distToNext = (ushort) distToNext;
		}

		allItems.Add(item);

		int add = item.distToNext + Constants.ITEM_SIZE_ON_BELT;
		rearSpace -= (short) add;

		if (rearSpace < -Constants.ITEM_SIZE_ON_BELT) {
			throw new System.Exception($"Rear space became too negative after appending: {rearSpace}");
		}
	}

	public void Update() {
		if (allItems.Count == 0) {
			return;
		}

		ChainUpdate(speed);
	}

	private void ChainUpdate(ushort remainingToMove) {
		if (frontItemIndex >= allItems.Count) {
			return;
		}

		BeltItem frontItem = allItems[frontItemIndex];
		ushort moved;
		ushort originalDistToNext = frontItem.distToNext;
		ushort dtn = frontItem.distToNext;
		if (frontItemIndex == 0) {
			dtn -= reservedFrontSpace;
		}

		if (dtn < remainingToMove) {
			moved = dtn;
			if (reservedFrontSpace == 0 && frontItemIndex == 0 && originalDistToNext < remainingToMove) {
				// This is the front item being able to go to the next belt
				ushort carryOverDist = SendCarryOver();
				rearSpace += (short) (originalDistToNext + Constants.ITEM_SIZE_ON_BELT);
			} else {
				frontItem.distToNext -= dtn;
				rearSpace += (short) moved;
			}
		} else {
			moved = remainingToMove;
			frontItem.distToNext -= remainingToMove;
			rearSpace += (short) moved;
		}

		if (moved < remainingToMove) {
			if (moved == 0) {
				frontItemIndex++;
			}

			DetermineNextFrontItem();

			remainingToMove -= moved;
			ChainUpdate(remainingToMove);
		}
	}

	private void DetermineNextFrontItem() {
		// Select the next BeltItem in allItems which has nonzero distToNext.
		for (; frontItemIndex < allItems.Count; frontItemIndex++) {
			BeltItem item = allItems[frontItemIndex];
			ushort compare = frontItemIndex == 0 ? reservedFrontSpace : (ushort) 0;
			if (item.distToNext > compare) {
				break;
			}
		}
	}

	private void RecalculateFrontItem() {
		frontItemIndex = 0;
		DetermineNextFrontItem();
	}
	#endregion

	#region Carry Over
	private ushort SendCarryOver() {
		if (allItems.Count == 0) {
			throw new System.Exception("Cannot send an item to the next belt when this belt has no items");
		}

		if (nextBeltId < 0 || !system.belts.ContainsKey(nextBeltId)) {
			throw new System.Exception("There is no next belt to send the front item to");
		}

		Belt nextBelt;
		if (!system.belts.TryGetValue(nextBeltId, out nextBelt)) {
			throw new System.Exception($"Could not obtain nextBelt from nextBeltId");
		}

		if (nextBelt.rearSpace < -Constants.ITEM_SIZE_ON_BELT) {
			throw new System.Exception($"Not enough space on the next belt to carry over the front item. nextBelt.rearSpace = {nextBelt.rearSpace}");
		}

		BeltItem item = allItems[0];
		if (item.distToNext >= speed) {
			throw new System.Exception("Front item is not close enough to be sent to the next belt");
		}

		ushort oldDistToNext = item.distToNext;
		ushort carryOverDist = (ushort) (speed - item.distToNext);

		int maxCarryOver = nextBelt.rearSpace + Constants.ITEM_SIZE_ON_BELT;
		if (maxCarryOver < 0) {
			maxCarryOver = 0;
		}
		carryOverDist = System.Math.Min(carryOverDist, (ushort) maxCarryOver);

		int newDistToNext = nextBelt.rearSpace - carryOverDist;
		if (newDistToNext < 0) {
			item.distToNext = 0;
		} else {
			item.distToNext = (ushort) newDistToNext;
		}

		nextBelt.allItems.Add(item);
		nextBelt.rearSpace -= (short) (item.distToNext + Constants.ITEM_SIZE_ON_BELT);

		allItems.RemoveAt(0);

		reservedFrontSpace = (ushort) (Constants.ITEM_SIZE_ON_BELT - carryOverDist);

		if (allItems.Count > 0) {
			allItems[0].distToNext += reservedFrontSpace;
		}

		return carryOverDist;
	}

	public (short spaceToFront, short spaceToBack, int indexBefore) GetAvailableSpaceAt(ushort distToEnd) {
		if (distToEnd > length) {
			throw new System.Exception($"distToEnd ({distToEnd}) exceeds belt length ({length})");
		}

		ushort front = 0;
		ushort back = length;
		short distToFront, distToBack;

		if (distToEnd < reservedFrontSpace) {
			front = reservedFrontSpace;
		}

		ushort d = 0;
		int i = 0;
		bool found = false;
		for (; i < allItems.Count; i++) {
			ushort prevD = d;
			d += allItems[i].distToNext;
			if (d > distToEnd) {
				back = d;
				if (i > 0) {
					front = prevD;
				} else {
					front = reservedFrontSpace;
				}
				i--;
				found = true;
				break;
			}
			d += Constants.ITEM_SIZE_ON_BELT;
			if (d > distToEnd) {  // CHANGED: from >= to >
								  // We're inside this item (not at its front edge)
				front = d;
				if (i + 1 < allItems.Count) {
					back = front;
					back += allItems[i + 1].distToNext;
				}
				found = true;
				break;
			}
		}

		if (!found) {
			front = d;
			i--;
		}

		distToFront = (short) distToEnd;
		distToFront -= (short) front;

		distToBack = (short) back;
		distToBack -= (short) distToEnd;
		distToBack -= (short) Constants.ITEM_SIZE_ON_BELT;

		return (distToFront, distToBack, i);
	}
	#endregion

	#region Split
	/// <summary>
	/// Split this belt at a specific point into two connected belts: A -> B
	/// This belt becomes B (the downstream/receiving belt).
	/// Returns the newly created belt A (the upstream/feeding belt).
	/// </summary>
	public Belt Split(ushort distToEnd) {
		if (distToEnd == 0 || distToEnd >= length) {
			throw new System.Exception($"Invalid split point: {distToEnd}");
		}

		Belt newBelt = new Belt(speed);

		// Get info about split point
		// indexBefore: index of the item that is IN FRONT OF (toward the front/exit) the split position
		// Items at indices 0 through indexBefore are in front of the split
		// Items at indices indexBefore+1 onwards are behind the split (toward rear/entrance)
		(short distToFront, short distToBack, int indexBefore) = GetAvailableSpaceAt(distToEnd);

		// Disconnect from previous belts (system will reconnect properly)
		if (system != null && inputBeltIds != null && inputBeltIds.Count > 0) {
			List<int> oldInputs = new List<int>(inputBeltIds);
			foreach (int inputId in oldInputs) {
				if (system.belts.ContainsKey(inputId)) {
					system.DisconnectBelts(system.belts[inputId], this);
				}
			}
			newBelt.inputBeltIds = oldInputs;  // newBelt (A, upstream) inherits the inputs
		}

		// Move items behind the split point to newBelt (A, the rear/feeding portion)
		// Items in front of the split (0 through indexBefore) stay on this belt (B, the front/receiving portion)
		int startIndex = indexBefore + 1;
		int itemsToMove = allItems.Count - startIndex;
		if (itemsToMove > 0) {
			newBelt.allItems.AddRange(allItems.GetRange(startIndex, itemsToMove));
			allItems.RemoveRange(startIndex, itemsToMove);
		}

		// Set lengths
		// This belt (B) gets the front portion: from split point to original front
		// newBelt (A) gets the rear portion: from original rear to split point
		ushort originalLength = length;
		length = distToEnd;  // Front portion (B, this belt)
		newBelt.length = (ushort) (originalLength - distToEnd);  // Rear portion (A, newBelt)

		// Adjust last item's distToNext on this belt (B)
		// The last item on this belt now terminates at the split point
		if (allItems.Count > 0) {
			BeltItem lastOnThisBelt = allItems[allItems.Count - 1];
			// Calculate distance from this item's back edge to the new end of belt
			short newDistToNext = (short) (distToFront + Constants.ITEM_SIZE_ON_BELT);
			lastOnThisBelt.distToNext = (ushort) System.Math.Max((ushort) 0, newDistToNext);
		}

		// Adjust first item's distToNext in newBelt (A)
		// The first item in newBelt is now at the front of its belt
		if (newBelt.allItems.Count > 0) {
			BeltItem firstInNewBelt = newBelt.allItems[0];
			// Calculate distance from start of newBelt to this item's back edge
			short newDistToNext = (short) (distToBack + Constants.ITEM_SIZE_ON_BELT);
			firstInNewBelt.distToNext = (ushort) System.Math.Max((ushort) 0, newDistToNext);
		}

		// Recalculate rear spaces
		newBelt.RecalculateRearSpace();
		RecalculateRearSpace();

		// Handle items extending across the split
		//if (rearSpace < 0) {
		//	// Item from newBelt (A) extends into current belt (B)
		//	reservedFrontSpace = (ushort) (-rearSpace);
		//	rearSpace = 0;
		//}

		// Connect newBelt (A, upstream) -> this belt (B, downstream)
		newBelt.nextBeltId = this.id;
		inputBeltIds = new List<int> { newBelt.id };

		// Recalculate front item indices
		newBelt.RecalculateFrontItem();
		RecalculateFrontItem();

		// Add newBelt to system if current belt is in a system
		if (system != null) {
			system.AddBelt(newBelt);
			system.ConnectBelts(newBelt, this);
		}

		return newBelt;
	}
	#endregion

	public override string ToString() {
		string s = "[BELT] | ";

		s += $"[L={length}\tS={speed}\tF={frontItemIndex}\tR={rearSpace}\tV={reservedFrontSpace}]\tItems: ";

		ushort itemsUsedSpace = 0;
		foreach (BeltItem item in allItems) {
			s += item.ToString();
			itemsUsedSpace += item.distToNext;
		}
		itemsUsedSpace += (ushort) (Constants.ITEM_SIZE_ON_BELT * allItems.Count);

		short oldRearSpace = rearSpace;
		RecalculateRearSpace();

		//s += "\n";
		//s += $"Rear space calculation is correct: {rearSpace == oldRearSpace}\n";
		//s += $"Space calculations are contiguous: {oldRearSpace + itemsUsedSpace == length || (oldRearSpace < 0 && oldRearSpace + itemsUsedSpace <= length)}\n";

		//s += "^^ BELT ^^";

		rearSpace = oldRearSpace;

		return s;
	}
}