
using System.Collections.Generic;
using UnityEngine;

public class WeightedChoice<T> {
	private readonly Dictionary<T, ulong> weights = new();
	private ulong totalWeight = 0;

	public WeightedChoice(Dictionary<T, ulong> weights) {
		foreach (KeyValuePair<T, ulong> kvp in weights) {
			SetWeight(kvp.Key, kvp.Value);
		}
	}

	public WeightedChoice() {

	}

	public void SetWeight(T value, ulong weight) {
		if (weights.ContainsKey(value)) {
			ulong prevWeight = weights[value];
			weights[value] = weight;
			totalWeight += weight - prevWeight;
		} else {
			weights.Add(value, weight);
			totalWeight += weight;
		}
	}

	public T Sample() {
		ulong cur = 0;
		ulong r = (ulong) Random.Range(0f, totalWeight);
		foreach (KeyValuePair<T, ulong> kvp in weights) {
			cur += kvp.Value;
			if (cur >= r) {
				return kvp.Key;
			}
		}
		Debug.LogError("Failed to sample a weighted choice value");
		return default;
	}
}
