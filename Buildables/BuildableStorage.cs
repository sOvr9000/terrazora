using System;
using System.Collections.Generic;
using UnityEngine;


public class BuildableStorage {

	public static BuildableStorage Instance { get; private set; }

	private Dictionary<BuildableType, GameObject> prefabs;

	public void Initialize() {
		Instance = this;

		prefabs = new Dictionary<BuildableType, GameObject>();
	}

	public void LoadPrefabs() {
		GameObject[] objs = Resources.LoadAll<GameObject>("Buildables");
		foreach (GameObject obj in objs) {
			BuildableType type = (BuildableType) Enum.Parse(typeof(BuildableType), obj.name);
			if (!prefabs.ContainsKey(type)) {
				prefabs.Add(type, obj);
			} else {
				Debug.LogWarning("BuildableType." + type + " already exists in the prefabs!");
			}
		}

		// Verify that all BuildableTypes have a registered object
		foreach (BuildableType type in Enum.GetValues(typeof(BuildableType))) {
			if (!prefabs.ContainsKey(type)) {
				Debug.LogWarning("No prefab found for BuildableType." + type);
			}
		}
	}

	public GameObject GetPrefab(BuildableType type) {
		GameObject prefab;
		if (prefabs.TryGetValue(type, out prefab)) {
			return prefab;
		}
		throw new Exception("Unrecognized type: BuildableType." + type);
	}
}

