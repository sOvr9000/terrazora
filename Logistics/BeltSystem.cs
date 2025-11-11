using System.Collections.Generic;

public class BeltSystem {

	public Dictionary<int, Belt> belts;
	public List<int> updateOrder;

	// Group management
	public Dictionary<int, BeltGroup> groups;  // groupId -> BeltGroup
	private int nextGroupId = 0;

	private int nextBeltId = 0;

	public BeltSystem() {
		belts = new Dictionary<int, Belt>();
		updateOrder = new List<int>();
		groups = new Dictionary<int, BeltGroup>();
	}

	public void Update() {
		foreach (int beltId in updateOrder) {
			if (belts.ContainsKey(beltId)) {
				belts[beltId].Update();
			}
		}
	}

	public Belt GetBelt(int id) {
		Belt belt;
		if (belts.TryGetValue(id, out belt)) {
			return belt;
		}
		throw new System.Exception($"Belt not found with ID = {id}");
	}

	/// <summary>
	/// Add a belt with an auto-generated ID
	/// </summary>
	public void AddBelt(Belt belt) {
		AddBelt(belt, nextBeltId++);
	}

	/// <summary>
	/// Add a belt with a specific ID (for multiplayer)
	/// </summary>
	public void AddBelt(Belt belt, int assignedId) {
		if (belts.ContainsKey(assignedId)) {
			return;
		}

		belt.system = this;
		belt.id = assignedId;

		if (assignedId >= nextBeltId) {
			nextBeltId = assignedId + 1;
		}

		belts.Add(belt.id, belt);

		// Create a new group for this belt (will merge later if connected)
		int groupId = nextGroupId++;
		belt.groupId = groupId;

		BeltGroup group = new BeltGroup(groupId);
		group.beltIds.Add(belt.id);
		groups.Add(groupId, group);

		// Add to update order
		AddToUpdateOrderIncremental(belt);
	}

	/// <summary>
	/// Remove a belt and potentially split its group
	/// </summary>
	public void RemoveBelt(Belt belt) {
		if (!belts.ContainsKey(belt.id)) {
			return;
		}

		int groupId = belt.groupId;
		List<int> inputIds = new List<int>(belt.inputBeltIds);
		int nextId = belt.nextBeltId;

		// Disconnect from input belts
		foreach (int inputId in inputIds) {
			if (belts.ContainsKey(inputId)) {
				belts[inputId].nextBeltId = -1;
			}
		}

		// Disconnect from next belt
		if (belt.nextBeltId != -1 && belts.ContainsKey(belt.nextBeltId)) {
			belts[belt.nextBeltId].inputBeltIds.Remove(belt.id);
		}

		// Remove from group
		if (groups.ContainsKey(groupId)) {
			groups[groupId].beltIds.Remove(belt.id);

			// If group is now empty, delete it
			if (groups[groupId].beltIds.Count == 0) {
				groups.Remove(groupId);
			} else if (inputIds.Count > 0 && nextId != -1) {
				// Belt was in the middle of a chain - might need to split group
				CheckAndSplitGroup(groupId);
			}
		}

		// Remove from system
		belts.Remove(belt.id);
		updateOrder.Remove(belt.id);
	}

	/// <summary>
	/// Connect two belts (belt1 outputs to belt2), merging groups if necessary
	/// </summary>
	public void ConnectBelts(Belt belt1, Belt belt2) {
		// Validation
#if UNITY_EDITOR
		if (belt1.id == belt2.id) {
			throw new System.Exception("Cannot connect a belt to itself");
		}
#endif

		if (!belts.ContainsKey(belt1.id) || !belts.ContainsKey(belt2.id)) {
			return;
		}

		if (belt1.nextBeltId != -1 && belts.ContainsKey(belt1.nextBeltId)) {
			DisconnectBelts(belt1, GetBelt(belt1.nextBeltId));
		}

		belt1.nextBeltId = belt2.id;
		belt2.AddInputBeltID(belt1.id);

		// Merge groups if different
		if (belt1.groupId != belt2.groupId) {
			MergeGroups(belt1.groupId, belt2.groupId);
		} else {
			// Same group - just rebuild this group's update order
			RebuildGroupUpdateOrder(belt1.groupId);
		}
	}

	/// <summary>
	/// Disconnect two belts and potentially split the group
	/// </summary>
	public void DisconnectBelts(Belt belt1, Belt belt2) {
		if (!belts.ContainsKey(belt1.id) || !belts.ContainsKey(belt2.id) || belt1.nextBeltId != belt2.id) {
			return;
		}

		belt1.nextBeltId = -1;
		belt2.RemoveInputBeltID(belt1.id);

		// Check if this split the group into separate components
		CheckAndSplitGroup(belt1.groupId);
	}

	/// <summary>
	/// Return whether <paramref name="belt1"/> is connected to and feeding into <paramref name="belt2"/>.
	/// </summary>
	/// <param name="belt1">The upstream belt.</param>
	/// <param name="belt2">The downstream belt.</param>
	/// <returns></returns>
	public bool BeltsAreConnected(Belt belt1, Belt belt2) {
		return belt1.IsConnectedTo(belt2); // Can possibly change this to check bidirectionally, but that's less consistent with game logic, so that may be more suitable as a different method.
	}

	/// <summary>
	/// Merge two groups into one and rebuild update order
	/// </summary>
	private void MergeGroups(int groupId1, int groupId2) {
		if (!groups.ContainsKey(groupId1) || !groups.ContainsKey(groupId2)) {
			return;
		}

		// Merge smaller group into larger one for efficiency
		int keepGroupId, mergeGroupId;
		if (groups[groupId1].beltIds.Count >= groups[groupId2].beltIds.Count) {
			keepGroupId = groupId1;
			mergeGroupId = groupId2;
		} else {
			keepGroupId = groupId2;
			mergeGroupId = groupId1;
		}

		// Update all belts in the merged group
		foreach (int beltId in groups[mergeGroupId].beltIds) {
			if (belts.ContainsKey(beltId)) {
				belts[beltId].groupId = keepGroupId;
			}
		}

		// Merge the belt ID lists
		groups[keepGroupId].beltIds.UnionWith(groups[mergeGroupId].beltIds);

		// Remove the old group
		groups.Remove(mergeGroupId);

		// Rebuild update order for the merged group
		RebuildGroupUpdateOrder(keepGroupId);
	}

	/// <summary>
	/// Check if a group should be split into multiple connected components
	/// </summary>
	private void CheckAndSplitGroup(int groupId) {
		if (!groups.ContainsKey(groupId) || groups[groupId].beltIds.Count <= 1) {
			return;
		}

		// Find all connected components using BFS
		HashSet<int> unvisited = new HashSet<int>(groups[groupId].beltIds);
		List<HashSet<int>> components = new List<HashSet<int>>();

		while (unvisited.Count > 0) {
			// Start a new component from any unvisited belt
			int startId = -1;
			foreach (int id in unvisited) {
				startId = id;
				break;
			}

			HashSet<int> component = new HashSet<int>();
			Queue<int> queue = new Queue<int>();
			queue.Enqueue(startId);

			// BFS to find all connected belts
			while (queue.Count > 0) {
				int currentId = queue.Dequeue();

				if (!unvisited.Contains(currentId)) {
					continue;
				}

				component.Add(currentId);
				unvisited.Remove(currentId);

				if (belts.ContainsKey(currentId)) {
					Belt current = belts[currentId];

					// Check next belt
					if (current.nextBeltId != -1 && unvisited.Contains(current.nextBeltId)) {
						queue.Enqueue(current.nextBeltId);
					}

					// Check all input belts
					foreach (int inputId in current.inputBeltIds) {
						if (unvisited.Contains(inputId)) {
							queue.Enqueue(inputId);
						}
					}
				}
			}

			components.Add(component);
		}

		// If we found multiple components, split the group
		if (components.Count > 1) {
			// Keep the first component in the original group
			groups[groupId].beltIds = components[0];

			// Create new groups for the rest
			for (int i = 1; i < components.Count; i++) {
				int newGroupId = nextGroupId++;
				BeltGroup newGroup = new BeltGroup(newGroupId);
				newGroup.beltIds = components[i];
				groups.Add(newGroupId, newGroup);

				// Update all belts in this component
				foreach (int beltId in components[i]) {
					if (belts.ContainsKey(beltId)) {
						belts[beltId].groupId = newGroupId;
					}
				}

				// Rebuild update order for the new group
				RebuildGroupUpdateOrder(newGroupId);
			}

			// Rebuild update order for the original group
			RebuildGroupUpdateOrder(groupId);
		}
	}

	/// <summary>
	/// Rebuild update order for a specific group only (O(group_size))
	/// </summary>
	private void RebuildGroupUpdateOrder(int groupId) {
		if (!groups.ContainsKey(groupId)) {
			return;
		}

		// Remove all belts in this group from update order
		HashSet<int> groupBelts = groups[groupId].beltIds;
		updateOrder.RemoveAll(id => groupBelts.Contains(id));

		// Rebuild order for just this group
		HashSet<int> visited = new HashSet<int>();

		// Start with belts that have no next belt (end of chains)
		foreach (int beltId in groupBelts) {
			if (belts.ContainsKey(beltId) && belts[beltId].nextBeltId == -1) {
				AddChainToUpdateOrder(beltId, visited, groupBelts);
			}
		}

		// Add any remaining belts in this group (cycles or disconnected)
		foreach (int beltId in groupBelts) {
			if (!visited.Contains(beltId)) {
				AddChainToUpdateOrder(beltId, visited, groupBelts);
			}
		}
	}

	/// <summary>
	/// Add a belt to update order based on its connections (incremental)
	/// </summary>
	private void AddToUpdateOrderIncremental(Belt belt) {
		if (belt.nextBeltId == -1) {
			updateOrder.Insert(0, belt.id);
			return;
		}

		int nextBeltPosition = updateOrder.IndexOf(belt.nextBeltId);

		if (nextBeltPosition == -1) {
			updateOrder.Add(belt.id);
		} else {
			updateOrder.Insert(nextBeltPosition + 1, belt.id);
		}
	}

	/// <summary>
	/// Recursively add a belt chain to update order (only within specified group)
	/// </summary>
	private void AddChainToUpdateOrder(int beltId, HashSet<int> visited, HashSet<int> groupBelts) {
		if (visited.Contains(beltId) || !belts.ContainsKey(beltId) || !groupBelts.Contains(beltId)) {
			return;
		}

		visited.Add(beltId);
		updateOrder.Add(beltId);

		Belt belt = belts[beltId];

		// Recursively add all input belts
		foreach (int inputId in belt.inputBeltIds) {
			if (groupBelts.Contains(inputId)) {
				AddChainToUpdateOrder(inputId, visited, groupBelts);
			}
		}
	}

	/// <summary>
	/// For bulk operations - rebuild everything from scratch
	/// </summary>
	public void RebuildAllGroups() {
		updateOrder.Clear();

		foreach (var group in groups.Values) {
			RebuildGroupUpdateOrder(group.id);
		}
	}

	public string ToStringDetailed() {
		string s = $"vv BeltSystem vv\nTotal belts: {belts.Count}\nAll belts:";
		
		foreach (Belt belt in belts.Values) {
			s += $"\n\n{belt.ToString()}\n{belt.DumpCriticalPoints()}";
		}

		s += "\n^^ BeltSystem ^^";

		return s;
	}
}

/// <summary>
/// Represents a group of connected belts
/// </summary>
public class BeltGroup {
	public int id;
	public HashSet<int> beltIds;

	public BeltGroup(int id) {
		this.id = id;
		beltIds = new HashSet<int>();
	}
}