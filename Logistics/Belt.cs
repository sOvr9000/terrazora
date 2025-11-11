using System;
using System.Collections.Generic;

public class Belt {
	public BeltSystem system;
	public int id, nextBeltId, groupId;
	public bool balanceInputs;
	public int currentInputBelt;
	public ushort reservedFrontSpace;
	private ushort frontItemIndex;
	public List<BeltItem> allItems;
	public List<int> inputBeltIds;  // Multiple belts can feed into this one
	public ushort length;
	public short rearSpace; // Space between the rear side of the last item on this belt (or the downstream end of the belt, accounting for reservedFrontSpace) and the upstream end of the belt.  Can be negative if the last item on this belt extends beyond the upstream end and onto a preceding belt.

	public readonly ushort speed;

	public Belt(ushort speed) {
		if (speed >= Constants.ITEM_SIZE_ON_BELT) {
			Logger.LogWarning($"Belt speed of {speed} is illegal. Capping at {Constants.ITEM_SIZE_ON_BELT} (item size on belt).");
			speed = Constants.ITEM_SIZE_ON_BELT;
		}
		this.speed = speed;

		reservedFrontSpace = 0;
		frontItemIndex = 0;
		length = 0;
		rearSpace = 0;

		balanceInputs = true;
		currentInputBelt = -1;

		nextBeltId = -1;
		inputBeltIds = new List<int>();

		allItems = new List<BeltItem>();
	}

	public void SetLength(ushort length) {
		this.length = length;
		RecalculateRearSpace();
	}

	/// <summary>
	/// Set the new reserved front space of the belt.
	/// If decreasing the value or no items exist on the belt, return 0.
	/// If incrementing the value and at least one item exists on the belt, return how much the reserved front space was able to be moved, pushing the front item and preceding items backward as needed until jamming.
	/// </summary>
	/// <param name="s"></param>
	/// <returns></returns>
	public ushort SetReservedFrontSpace(ushort s) {
		ushort prev = reservedFrontSpace;
		reservedFrontSpace = s;

		if (s < prev) {
			frontItemIndex = 0;
			if (allItems.Count == 0) {
				rearSpace += (short) (prev - s);
			}
			return 0;
		}

		if (s == prev) {
			return 0;
		}

		if (allItems.Count == 0) {
			rearSpace -= (short) (s - prev);
			return 0;
		}

		if (reservedFrontSpace <= allItems[0].distToNext) {
			return 0;
		}

		ushort toMove = (ushort) (reservedFrontSpace - allItems[0].distToNext);

		ushort actualMoved = InverseChainUpdate(0, 1, toMove);
		RecalculateFrontItem();

		if (actualMoved < toMove) {
			reservedFrontSpace -= (ushort) (toMove - actualMoved);
		}

		return (ushort) (reservedFrontSpace - prev);
	}

	/// <summary>
	/// Push items on the belt backward, squeezing earlier gaps first.
	/// Return how much the front item (at index <paramref name="frontIdx"/>) has been pushed backward before hitting maximum compression.
	/// </summary>
	/// <param name="frontIdx"></param>
	/// <param name="curIdx"></param>
	/// <param name="remainingToMove"></param>
	public ushort InverseChainUpdate(int frontIdx, int curIdx, ushort remainingToMove) {
		if (remainingToMove == 0) {
			return 0;
		}

#if UNITY_EDITOR
		if (remainingToMove >= Constants.ITEM_SIZE_ON_BELT) {
			throw new Exception($"remainingToMove = {remainingToMove} is larger than or equal to the maximum of {Constants.ITEM_SIZE_ON_BELT} (item size on belt)");
		}
#endif

		if (curIdx >= allItems.Count) {
			return 0;
		}

		BeltItem curItem = allItems[curIdx];
		ushort toMove = Math.Min(curItem.distToNext, remainingToMove);
		curItem.distToNext -= toMove;
		allItems[frontIdx].distToNext += toMove;

		ushort actualMoved = toMove;
		remainingToMove -= toMove;

		if (curIdx < allItems.Count - 1) {
			actualMoved += InverseChainUpdate(frontIdx, curIdx + 1, remainingToMove);
			return actualMoved;
		}

		short targetRearSpace = (short) Math.Max(rearSpace - remainingToMove, 1 - Constants.ITEM_SIZE_ON_BELT);

		if (targetRearSpace >= 0) {
			rearSpace = targetRearSpace;
			allItems[frontIdx].distToNext += remainingToMove;
			return remainingToMove;
		}

		short originalRearSpace = rearSpace;
		rearSpace = targetRearSpace;

		ushort actualRearSpaceAdjustment = remainingToMove;
		if (inputBeltIds.Count > 0) {
			actualRearSpaceAdjustment = SetPreviousBeltReservedFrontSpace();

#if UNITY_EDITOR
			if (actualRearSpaceAdjustment + originalRearSpace < 0) {
				throw new Exception($"(WARNING: belt is likely in an invalid state now) SetPreviousBeltReservedFrontSpace() returned a value too large ({actualRearSpaceAdjustment}) to add to originalRearSpace = {originalRearSpace} to calculate actualRearSpaceAdjustment.  This might be because the front item on the previous belt is \"pushing forward\" too hard in adjustment from the attempt to push it backward.");
			}
#endif

			actualRearSpaceAdjustment = (ushort) (actualRearSpaceAdjustment + originalRearSpace);
		}

		if (actualRearSpaceAdjustment == remainingToMove) {
			allItems[frontIdx].distToNext += remainingToMove;
			return (ushort) (actualMoved + remainingToMove);
		}

		ushort invalidToMove = (ushort) (remainingToMove - actualRearSpaceAdjustment);
		allItems[frontIdx].distToNext -= invalidToMove;
		rearSpace = (short) (originalRearSpace - actualRearSpaceAdjustment);

		actualMoved = (ushort) (toMove + actualRearSpaceAdjustment);
		return actualMoved;
	}

	public void RecalculateRearSpace() {
		int space = length;

		if (allItems.Count == 0) {
			space -= reservedFrontSpace;
		}

		foreach (BeltItem item in allItems) {
			space -= item.distToNext;
			space -= Constants.ITEM_SIZE_ON_BELT;
		}

#if UNITY_EDITOR
		if (space <= -Constants.ITEM_SIZE_ON_BELT) {
			throw new Exception($"Rear space calculated to be too negative: {space} (cannot be at or below {-Constants.ITEM_SIZE_ON_BELT})");
		}

		if (space > short.MaxValue) {
			throw new Exception($"Rear space calculated to be too large: {space} (cannot exceed {short.MaxValue})");
		}
#endif

		rearSpace = (short) space;
	}

	#region Single Segment
	public void AddInputBeltID(int beltId) {
#if UNITY_EDITOR
		if (inputBeltIds.Contains(beltId)) {
			throw new Exception($"Belt ID = {beltId} is already an input to this belt:\n{ToString()}");
		}
#endif

		inputBeltIds.Add(beltId);
	}

	public void RemoveInputBeltID(int beltId) {
#if UNITY_EDITOR
		if (!inputBeltIds.Contains(beltId)) {
			throw new Exception($"Belt ID = {beltId} is not an input to this belt:\n{ToString()}");
		}
#endif

		inputBeltIds.Remove(beltId);
		
		if (currentInputBelt >= inputBeltIds.Count) {
			SetCurrentInputBelt(-1);
			//currentInputBelt = -1;
		}
	}

	/// <summary>
	/// Place an item on this belt at the exact total distance from the downstream end of the belt.
	/// </summary>
	/// <param name="item"></param>
	/// <param name="distFromEnd"></param>
	/// <exception cref="Exception"></exception>
	public void InsertItem(BeltItem item, ushort distFromEnd) {
		(short spaceToFront, short spaceToBack, int indexBefore) = GetAvailableSpaceAt(distFromEnd);

#if UNITY_EDITOR
		if (distFromEnd >= length) {
			throw new Exception($"Item placed too far back at distFromEnd={distFromEnd} on the belt of length {length}");
		}

		if (spaceToFront < 0) {
			throw new Exception("Not enough space in front of the item");
		}

		if (spaceToBack < 0 && indexBefore < allItems.Count - 1) {
			throw new Exception("Not enough space behind the item");
		}
#endif

		// TODO: refactor to use less indentation
		if (indexBefore == allItems.Count - 1) {
			rearSpace -= spaceToFront;
			rearSpace -= (short) Constants.ITEM_SIZE_ON_BELT;
			if (spaceToBack < 0) {
				short actualSpaceToBack = AdjustedSpaceToBackFromPrecedingBelt(spaceToBack);
				if (actualSpaceToBack >= 0) {
					// Extending over to the previous belt.
#if UNITY_EDITOR
					if (inputBeltIds.Count != 1) {
						rearSpace += spaceToFront;
						rearSpace += (short) Constants.ITEM_SIZE_ON_BELT;
						throw new Exception("GetAvailableSpaceAt() returned an invalid value for this operation");
					}
#endif
					Belt prev = system.GetBelt(inputBeltIds[0]);
					prev.SetReservedFrontSpace((ushort) -spaceToBack); // Should not be actualSpaceToBack; using spaceToBack results in the correct calculation.
				} else {
					rearSpace += spaceToFront;
					rearSpace += (short) Constants.ITEM_SIZE_ON_BELT;
					throw new Exception("Not enough space behind the item (previous belt's front item is too close, or there is no input belt and extending is prohibited)");
				}
			}
		} else {
			allItems[indexBefore + 1].distToNext -= (ushort) (spaceToFront + Constants.ITEM_SIZE_ON_BELT);
		}

		item.distToNext = (ushort) spaceToFront;
		allItems.Insert(indexBefore + 1, item);
	}

	/// <summary>
	/// Automatically creates the BeltItem needed for insertion.
	/// </summary>
	/// <param name="itemType"></param>
	/// <param name="distFromEnd"></param>
	public void InsertItem(ItemType itemType, ushort distFromEnd) {
		InsertItem(new BeltItem(itemType, 0), distFromEnd);
	}

	/// <summary>
	/// Add an item directly at the upstream end of the belt.
	/// </summary>
	/// <param name="item"></param>
	public void AppendItem(BeltItem item) {
		AppendItemAt(item, 1);
	}

	/// <summary>
	/// Add an item near the upstream end of the belt, but at <paramref name="distFromStart"/> units of distance from it.
	/// Assumes that the appended item is going the be the new last item on the belt, saving compute time.
	/// </summary>
	/// <param name="item"></param>
	/// <param name="distFromStart"></param>
	/// <exception cref="Exception"></exception>
	public void AppendItemAt(BeltItem item, ushort distFromStart) {
#if UNITY_EDITOR
		if (distFromStart == 0) {
			throw new Exception("Cannot add an item exactly at the upstream end");
		}

		if (rearSpace < distFromStart) {
			throw new Exception("Not enough space on belt");
		}
#endif

		ushort dist = (ushort) rearSpace;
		dist -= distFromStart;
		if (allItems.Count == 0) {
			dist += reservedFrontSpace;
		}
		item.distToNext = dist;

		rearSpace = (short) (distFromStart - Constants.ITEM_SIZE_ON_BELT);
		SetPreviousBeltReservedFrontSpace();

		if (item.distToNext == 0 && frontItemIndex == allItems.Count) {
			frontItemIndex++;
		}

		allItems.Add(item);
	}

	public bool HasActiveInputBelt() {
		return currentInputBelt != -1;
	}

	public Belt GetActiveInputBelt() {
		int activeBeltId = GetCurrentActiveInputBeltID();

#if UNITY_EDITOR
		if (activeBeltId == -1) {
			throw new Exception("GetActiveInputBelt() was called when the belt is in the state to accept any input belt. Check for Belt.HasActiveInputBelt() before calling this.");
		}
#endif

		return system.GetBelt(activeBeltId);
	}

	/// <summary>
	/// Set the reserved front space of the currently active input belt preceding this one.
	/// Return the total space moved backward, or zero if no input belt exists or the space was moved forward instead.
	/// </summary>
	/// <returns></returns>
	public ushort SetPreviousBeltReservedFrontSpace() {
		if (!HasActiveInputBelt()) {
			if (inputBeltIds.Count > 0) {
				// Just pick one of the input belts to push back onto.
				SetCurrentInputBelt(0);
				// currentInputBelt = 0;
			} else {
				return 0;
			}
		}

		Belt prevBelt = GetActiveInputBelt();
		ushort spaceToRequest = (ushort) Math.Max(0, -rearSpace);
		ushort result = prevBelt.SetReservedFrontSpace(spaceToRequest);

		return result;
	}

	public void RemoveItem(ushort index) {
#if UNITY_EDITOR
		if (index >= allItems.Count) {
			throw new Exception($"Item index {index} is invalid for belt with {allItems.Count} items on it");
		}
#endif

		BeltItem item = allItems[index];

		allItems.RemoveAt(index);
		if (frontItemIndex > index) {
			frontItemIndex = index;
		}

		if (index < allItems.Count) {
			BeltItem prev = allItems[index];
			prev.distToNext += (ushort) (Constants.ITEM_SIZE_ON_BELT + item.distToNext);
		} else if (allItems.Count == 0) {
			rearSpace = (short) (length - reservedFrontSpace);
		}
	}

	public void Update() {
#if UNITY_EDITOR
		VerifySpaces();
#endif

		if (allItems.Count == 0) {
			return;
		}

		ushort moved = 0;
		ushort carryOverDist = 0;
		ChainUpdate(speed, ref moved, ref carryOverDist);
		PostProcessMovedItems(moved, carryOverDist);
		UpdateCurrentInputBelt();
	}

	private void PostProcessMovedItems(ushort moved, ushort carryOverDist) {
		if (moved == 0) {
			return;
		}

		bool wasNegative = rearSpace < 0;

		// When the last item is transferred to the next belt, RemoveItem + SetReservedFrontSpace
		// already set rearSpace correctly. Don't add moved in this case.
		if (!(carryOverDist > 0 && allItems.Count == 0)) {
			rearSpace += (short) moved;

			if (rearSpace > length) {
				// This should only possibly happen when a single item on a belt is sent to the next belt, and it's a cheap calculation in that case.
				RecalculateRearSpace();
			}
		}

		if (wasNegative) {
			SetPreviousBeltReservedFrontSpace();
		}
	}

	private void ChainUpdate(ushort remainingToMove, ref ushort moved, ref ushort carryOverDist) {
		if (frontItemIndex >= allItems.Count) {
			return;
		}

		BeltItem frontItem = allItems[frontItemIndex];

		ushort actualDistToNext = frontItem.distToNext;
		bool isFrontItem = frontItemIndex == 0;
		if (isFrontItem) {
#if UNITY_EDITOR
			if (actualDistToNext < reservedFrontSpace) {
				throw new Exception($"Item is too close to reserved front space!  Data is mishandled somewhere.  Tried to subtract reservedFrontSpace={reservedFrontSpace} from actualDistToNext={actualDistToNext}.  Current belt:\n{ToString()}");
			}
#endif
			actualDistToNext -= reservedFrontSpace;
		}

		if (actualDistToNext >= remainingToMove) {
			frontItem.distToNext -= remainingToMove;
			moved += remainingToMove;
			return;
		}

		ushort curMoved;
		ushort curCarryOverDist = 0;

		curMoved = actualDistToNext;
		if (isFrontItem && CanSendCarryOver()) {
			// This is the front item being able to go to the next belt
			curCarryOverDist = SendCarryOver();
			curMoved += curCarryOverDist;
		} else {
			frontItem.distToNext -= actualDistToNext;
		}

		if (curMoved < remainingToMove) {
			if (curMoved == 0 && curCarryOverDist == 0) {
				frontItemIndex++;
			}

			DetermineNextFrontItem();

			remainingToMove -= moved;

			ChainUpdate(remainingToMove, ref moved, ref carryOverDist);
		}

		moved += curMoved;
		carryOverDist += curCarryOverDist;
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

	public (short spaceToFront, short spaceToBack, int indexBefore) GetAvailableSpaceAt(ushort distToEnd) {
#if UNITY_EDITOR
		if (distToEnd > length) {
			throw new Exception($"distToEnd ({distToEnd}) exceeds belt length ({length})");
		}
#endif

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
			if (d >= distToEnd) {
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
			if (d > distToEnd) {
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

	#region Multi-Segment
	public void SetBalanceInputs(bool flag) {
		balanceInputs = flag;

		// Shouldn't need anything here.
	}

	public void SetCurrentInputBelt(int n) {
		currentInputBelt = n;

		if (n >= 0) {
			Belt belt = GetActiveInputBelt();
			belt.frontItemIndex = 0;
		}
	}

	private bool CanSendCarryOver() {
		if (reservedFrontSpace > 0 || nextBeltId == -1) {
			return false;
		}

		if (allItems.Count == 0 || allItems[0].distToNext >= speed) {
			return false;
		}

		Belt nextBelt = system.GetBelt(nextBeltId);
		if (!nextBelt.CanReceiveCarryOver(id)) {
			return false;
		}

		return true;
	}

	private bool CanReceiveCarryOver(int fromBeltId) {
		if (rearSpace <= 0) {
			return false;
		}

		int activeId = GetCurrentActiveInputBeltID();
		if (activeId == -1) {
			// Take the first belt to send items to this one.
			return true;
		}

		if (activeId != fromBeltId) {
			return false;
		}

		return true;
	}

	public int GetCurrentActiveInputBeltID() {
#if UNITY_EDITOR
		if (currentInputBelt >= inputBeltIds.Count) {
			throw new Exception($"currentInputBelt = {currentInputBelt} out of range for the list of input belt ids of length {inputBeltIds.Count}");
		}
#endif

		if (currentInputBelt == -1) {
			// This is currently in the state of no suitable carry-over items on any input belt.
			return -1;
		}

		return inputBeltIds[currentInputBelt];
	}

	private void UpdateCurrentInputBelt() {
		if (rearSpace <= 0 || currentInputBelt == -1) {
			// The current input belt is not ready to be changed.
			return;
		}

		if (inputBeltIds.Count == 0) {
			SetCurrentInputBelt(-1);
			//currentInputBelt = -1;
			return;
		}

		int offset = balanceInputs ? currentInputBelt + 1 : 0;

		// Find a belt with a suitable carry-over item.
		for (int i = 0; i < inputBeltIds.Count; i++) {
			int n = (offset + i) % inputBeltIds.Count;
			int nextId = inputBeltIds[n];

			Belt belt = system.GetBelt(nextId);
			if (belt.allItems.Count > 0 && belt.allItems[0].distToNext < belt.speed) {
				SetCurrentInputBelt(n);
				//currentInputBelt = n;
				return;
			}
		}

		// No belt was found with suitable carry-over items.
		SetCurrentInputBelt(-1);
		//currentInputBelt = -1;
	}

	private ushort SendCarryOver() {
#if UNITY_EDITOR
		if (allItems.Count == 0) {
			throw new Exception("Cannot send an item to the next belt when this belt has no items");
		}

		if (nextBeltId < 0 || !system.belts.ContainsKey(nextBeltId)) {
			throw new Exception("There is no next belt to send the front item to");
		}

		Belt nextBelt = system.GetBelt(nextBeltId);
		if (nextBelt.rearSpace <= 0) {
			throw new Exception($"Not enough space on the next belt to carry over the front item. nextBelt.rearSpace = {nextBelt.rearSpace}");
		}

		BeltItem item = allItems[0];
		if (item.distToNext >= speed) {
			throw new Exception("Front item is not close enough to be sent to the next belt");
		}
#endif

		RemoveItem(0);

		ushort carryOverDist = (ushort) (speed - item.distToNext);
		ushort error = 0;
		if (carryOverDist > nextBelt.rearSpace) {
			error = carryOverDist;
			error -= (ushort) nextBelt.rearSpace;
			carryOverDist -= error;
		}

		nextBelt.AppendItemAt(item, carryOverDist);
		if (nextBelt.rearSpace < 0) {
			// Set the next belt's currently active input belt to this one.
			nextBelt.SetCurrentInputBelt(nextBelt.inputBeltIds.IndexOf(id));
			//nextBelt.currentInputBelt = nextBelt.inputBeltIds.IndexOf(id);
		}

		if (allItems.Count > 0) {
			allItems[0].distToNext -= (ushort) (speed - error);
		}

		return carryOverDist;
	}

	/// <summary>
	/// Convert a single-belt measurement of rear-facing available space to a two-belt measurement, which can make the measurement positive.
	/// </summary>
	/// <param name="spaceToBack"></param>
	/// <returns></returns>
	public short AdjustedSpaceToBackFromPrecedingBelt(short spaceToBack) {
		if (inputBeltIds.Count != 1) {
			// Not in a two-belt connection point.  For convenience, this returns the value back instead of throwing an error.
			return spaceToBack;
		}

		if (spaceToBack >= 0) {
			// No need to adjust; the spaceToBack does not represent extending over the upstream end.
			return spaceToBack;
		}

		Belt prev = system.GetBelt(inputBeltIds[0]);
		if (prev.allItems.Count == 0) {
			return (short) (prev.length + spaceToBack);
		}

		return (short) (prev.allItems[0].distToNext + spaceToBack);
	}

	/// <summary>
	/// Return whether this belt is connected to and feeding into <paramref name="other"/>.
	/// </summary>
	/// <param name="other">The downstream belt.</param>
	/// <returns></returns>
	public bool IsConnectedTo(Belt other) {
		return nextBeltId == other.id && other.inputBeltIds.Contains(id) && system == other.system;
	}
	#endregion

	#region Split
	/// <summary>
	/// Split this belt at a specific point into two connected belts: A -> B
	/// This belt becomes B (the downstream/receiving belt).
	/// Returns the newly created belt A (the upstream/feeding belt).
	/// </summary>
	public Belt Split(ushort distToEnd) {
#if UNITY_EDITOR
		if (distToEnd == 0 || distToEnd >= length) {
			throw new Exception($"Invalid split point: {distToEnd}");
		}
#endif

		Belt newBelt = new Belt(speed);
		(short distToFront, short distToBack, int indexBefore) = GetAvailableSpaceAt(distToEnd);

		if (system != null) {
			system.AddBelt(newBelt);
			if (inputBeltIds != null) {
				List<int> oldInputs = new List<int>(inputBeltIds);
				foreach (int inputId in oldInputs) {
					if (system.belts.ContainsKey(inputId)) {
						system.DisconnectBelts(system.belts[inputId], this);
						system.ConnectBelts(system.belts[inputId], newBelt);
						// TODO: Optimization: do not check for group splitting or merging here; theoretically nothing should ever change when splitting a belt
					}
				}
			}
			system.ConnectBelts(newBelt, this);
		}

		int startIndex = indexBefore + 1;
		int itemsToMove = allItems.Count - startIndex;
		if (itemsToMove > 0) {
			newBelt.allItems.AddRange(allItems.GetRange(startIndex, itemsToMove));
			allItems.RemoveRange(startIndex, itemsToMove);
		}

		ushort originalLength = length;
		length = distToEnd;
		newBelt.length = (ushort) (originalLength - distToEnd);

		if (newBelt.allItems.Count > 0) {
			BeltItem firstInNewBelt = newBelt.allItems[0];
			short newDistToNext = (short) (distToBack + Constants.ITEM_SIZE_ON_BELT);
			firstInNewBelt.distToNext = (ushort) System.Math.Max((ushort) 0, newDistToNext);
		}

		newBelt.rearSpace = rearSpace;
		rearSpace = distToFront;

		if (rearSpace < 0) {
			newBelt.reservedFrontSpace = (ushort) -rearSpace;
		}

		newBelt.RecalculateFrontItem();
		RecalculateFrontItem();

		return newBelt;
	}

	public void Join(Belt other) {
		if (nextBeltId == other.id) {
#if UNITY_EDITOR
			if (other.nextBeltId == id) {
				throw new Exception("Cannot join a circular belt connection.");
			}
#endif
			other.Join(this);
			return;
		}

#if UNITY_EDITOR
		if (inputBeltIds.Count != 1 || inputBeltIds[0] != other.id) {
			throw new Exception("Given belt is not the only belt feeding into this belt.");
		}
#endif

		allItems.AddRange(other.allItems);

		if (other.allItems.Count > 0) {
			BeltItem inter = other.allItems[0];

#if UNITY_EDITOR
			if (rearSpace < 0 && -rearSpace > inter.distToNext) {
				throw new Exception("Negative value encountered in distToNext of given belt. This indicates mishandling of the data elsewhere.");
			}
#endif
			inter.distToNext = (ushort) (inter.distToNext + rearSpace);
			rearSpace = other.rearSpace;
		} else {
			rearSpace += other.rearSpace;
		}

		length += other.length;

		RecalculateFrontItem();

		if (system != null) {
			for (int i = other.inputBeltIds.Count - 1; i >= 0; i--) {
				system.ConnectBelts(system.GetBelt(other.inputBeltIds[i]), this);
			}

			// Reverse order of connections just in case the order matters (when balanceInputs is true), since they were added in reverse order above
			inputBeltIds.Reverse();

			system.RemoveBelt(other);
		}
	}
	#endregion

	public void VerifySpaces() {
		if (allItems.Count > 0 && reservedFrontSpace > allItems[0].distToNext) {
			throw new Exception($"reservedFrontSpace={reservedFrontSpace} is larger than the front item's distToNext={allItems[0].distToNext}.  Current belt:\n{ToString()}");
		}

		foreach (BeltItem item in allItems) {
			if (item.distToNext >= length) {
				throw new Exception($"Belt item ({item}) has distToNext larger than or equal to length={length}");
			}
		}

		short rs = rearSpace;
		RecalculateRearSpace();
		if (rs != rearSpace) {
			short correct = rearSpace;
			rearSpace = rs;
			throw new Exception($"Rear space is not being handled correctly!  Expected rearSpace={correct} but currently rearSpace={rearSpace}.  Current belt:\n{ToString()}");
		}
	}

	public override string ToString() {
		string s = $"[BELT (ID={id, -4})] | ";

		s += $"[L={length,-5} S={speed,-3} F={frontItemIndex,-3} R={TZLib.FormatWithSign(rearSpace, 6)} V={reservedFrontSpace,-3} I={(currentInputBelt == -1 ? " " : currentInputBelt)}] | ";
		s += $"Output Belt ID={(nextBeltId >= 0 ? nextBeltId : ""), -4} | Input Belt IDs: [";

		for (int i = 0; i < inputBeltIds.Count; i++) {
			int inputId = inputBeltIds[i];
			if (i > 0) {
				s += ", ";
			}
			s += inputId;
		}
		s += "] | Items: ";

		foreach (BeltItem item in allItems) {
			s += item.ToString();
		}

		return s;
	}

	public string DumpCriticalPoints() {
		string s = "Belt Critical Points | 0";

		ushort total = 0;
		ushort nextTotal;
		foreach (BeltItem item in allItems) {
			nextTotal = item.distToNext;
			nextTotal += total;
			if (nextTotal >= reservedFrontSpace && total < reservedFrontSpace) {
				s += $" V={reservedFrontSpace}";
			}
			if (nextTotal >= length && total < reservedFrontSpace) {
				s += $" L={length}";
			}
			total = nextTotal;
			s += $" ({item.type})[{total}"; // Item space occupation starts

			nextTotal = Constants.ITEM_SIZE_ON_BELT;
			nextTotal += total;
			if (nextTotal >= reservedFrontSpace && total < reservedFrontSpace) {
				s += $" V={reservedFrontSpace}";
			}
			if (nextTotal >= length && total < reservedFrontSpace) {
				s += $" L={length}";
			}
			total = nextTotal;
			s += $" {total}]"; // Item space occupation ends
		}

		if (total < length) {
			s += $" R={length-rearSpace} L={length}";
		}

		return s;
	}
}