
using System;
using UnityEngine;
using UnityEngine.UI;

public static class Events {
	public static Action<CaveSystemLayer> CaveSystemLayerAdded;
	public static void TriggerCaveSystemLayerAdded(CaveSystemLayer layer) => CaveSystemLayerAdded?.Invoke(layer);

	public static Action<CaveSystemLayer> CaveSystemLayerAdding;
	public static void TriggerCaveSystemLayerAdding(CaveSystemLayer layer) => CaveSystemLayerAdding?.Invoke(layer);

	public static Action<CaveSystemLayer> CaveSystemLayerGenerated;
	public static void TriggerCaveSystemLayerGenerated(CaveSystemLayer layer) => CaveSystemLayerGenerated?.Invoke(layer);

	public static Action<CaveSystemLayer> CaveSystemLayerLoaded;
	public static void TriggerCaveSystemLayerLoaded(CaveSystemLayer layer) => CaveSystemLayerLoaded?.Invoke(layer);

	public static Action<CaveSystemLayer, float> CaveSystemLayerGenerationProgress;
	public static void TriggerCaveSystemLayerGenerationProgress(CaveSystemLayer layer, float progress) => CaveSystemLayerGenerationProgress?.Invoke(layer, progress);

	public static Action<CaveSystemLayer, float> CaveSystemLayerLoadProgress;
	public static void TriggerCaveSystemLayerLoadProgress(CaveSystemLayer layer, float progress) => CaveSystemLayerLoadProgress?.Invoke(layer, progress);

	public static Action<CaveSystem> CaveSystemGenerated;
	public static void TriggerCaveSystemGenerated(CaveSystem caveSystem) => CaveSystemGenerated?.Invoke(caveSystem);

	public static Action<CaveSystem> CaveSystemLoaded;
	public static void TriggerCaveSystemLoaded(CaveSystem caveSystem) => CaveSystemLoaded?.Invoke(caveSystem);

	public static Action<EntityController, CaveSystemLayer> EntityChangedCaveSystemLayers;
	public static void TriggerEntityChangedCaveSystemLayers(EntityController entity, CaveSystemLayer prevLayer) => EntityChangedCaveSystemLayers?.Invoke(entity, prevLayer);

	public static Action<EntityController> EntitySpawned;
	public static void TriggerEntitySpawned(EntityController entity) => EntitySpawned?.Invoke(entity);

	public static Action<EntityController, Vector2> EntityTeleported;
	public static void TriggerEntityTeleported(EntityController entity, Vector2 prevPos) => EntityTeleported?.Invoke(entity, prevPos);

	public static Action<EntityController> EntityPlayerControllerChanged;
	public static void TriggerEntityPlayerControllerChanged(EntityController entity) => EntityPlayerControllerChanged?.Invoke(entity);

	public static Action GamePaused;
	public static void TriggerGamePaused() => GamePaused?.Invoke();

	public static Action GameUnpaused;
	public static void TriggerGameUnpaused() => GameUnpaused?.Invoke();

	public static Action<string> ConsoleTextSubmitted;
	public static void TriggerConsoleTextSubmitted(string text) => ConsoleTextSubmitted?.Invoke(text);

	public static Action<string> ConsoleMessageAdded;
	public static void TriggerConsoleMessageAdded(string text) => ConsoleMessageAdded?.Invoke(text);

	public static Action<Image> MenuUIShown;
	public static void TriggerMenuUIShown(Image menuUI) => MenuUIShown?.Invoke(menuUI);

	public static Action<Image> MenuUIHidden;
	public static void TriggerMenuUIHidden(Image menuUI) => MenuUIHidden?.Invoke(menuUI);

	public static Action<MainMenuView> MenuViewChanged;
	public static void TriggerMenuViewChanged(MainMenuView view) => MenuViewChanged?.Invoke(view);

	public static Action<BitArray> SaveLoading;
	public static void TriggerSaveLoading(BitArray bitArray) => SaveLoading?.Invoke(bitArray);
}
