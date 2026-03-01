using System;
using System.Collections;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MogulShadows;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public sealed class MogulShadowsPlugin : BaseUnityPlugin
{
    private const string IgnoredSceneName = "MainMenu";

    private Harmony? _harmony;
    private Coroutine? _sceneLoadRoutine;

    internal static MogulShadowsPlugin? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        _harmony = new Harmony(ModInfo.PLUGIN_GUID);
        _harmony.PatchAll();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _harmony?.UnpatchSelf();
        _harmony = null;

        if (_sceneLoadRoutine != null)
        {
            StopCoroutine(_sceneLoadRoutine);
            _sceneLoadRoutine = null;
        }

        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_sceneLoadRoutine != null)
        {
            StopCoroutine(_sceneLoadRoutine);
            _sceneLoadRoutine = null;
        }

        _sceneLoadRoutine = StartCoroutine(ApplySceneLoadPasses(mode));
    }

    private IEnumerator ApplySceneLoadPasses(LoadSceneMode mode)
    {
        string reason = $"scene load ({mode})";
        ApplyShadowsToAllLights(reason);

        yield return null;
        ApplyShadowsToAllLights($"{reason} +1f");

        yield return new WaitForSecondsRealtime(0.25f);
        ApplyShadowsToAllLights($"{reason} +0.25s");

        yield return new WaitForSecondsRealtime(1f);
        ApplyShadowsToAllLights($"{reason} +1.25s");

        _sceneLoadRoutine = null;
    }

    internal void HandleInstantiatedObject(UnityEngine.Object? spawned)
    {
        if (spawned == null || IsIgnoredSceneActive())
        {
            return;
        }

        GameObject? target = ResolveSpawnedGameObject(spawned);
        if (target == null)
        {
            return;
        }

        TryAttachSpawnWatcher(target);
        ApplyShadowsToGameObjectHierarchy(target, "spawned object");
    }

    private void ApplyShadowsToAllLights(string reason)
    {
        if (IsIgnoredSceneActive())
        {
            return;
        }

        Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int changed = 0;

        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null)
            {
                continue;
            }

            Scene lightScene = light.gameObject.scene;
            if (!lightScene.IsValid() || !lightScene.isLoaded)
            {
                continue;
            }

            if (ApplyShadowSettings(light))
            {
                changed++;
            }
        }

        if (changed > 0)
        {
            Logger.LogInfo($"Applied shadows to {changed} lights ({reason}).");
        }
    }

    internal void ApplyShadowsToGameObjectHierarchy(GameObject root, string reason)
    {
        Scene rootScene = root.scene;
        if (!rootScene.IsValid() || !rootScene.isLoaded || IsIgnoredSceneActive())
        {
            return;
        }

        Light[] lights = root.GetComponentsInChildren<Light>(true);
        int changed = 0;
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null)
            {
                continue;
            }

            Scene lightScene = light.gameObject.scene;
            if (!lightScene.IsValid() || !lightScene.isLoaded)
            {
                continue;
            }

            if (ApplyShadowSettings(light))
            {
                changed++;
            }
        }

        if (changed > 0)
        {
            Logger.LogInfo($"Applied shadows to {changed} lights ({reason}).");
        }
    }

    private static GameObject? ResolveSpawnedGameObject(UnityEngine.Object spawned)
    {
        return spawned switch
        {
            GameObject gameObject => gameObject,
            Component component => component.gameObject,
            _ => null
        };
    }

    private static bool ApplyShadowSettings(Light light)
    {
        bool changed = false;

        if (light.shadows != LightShadows.Soft)
        {
            light.shadows = LightShadows.Soft;
            changed = true;
        }

        if (light.shadowStrength < 1f)
        {
            light.shadowStrength = 1f;
            changed = true;
        }

        return changed;
    }

    private static void TryAttachSpawnWatcher(GameObject root)
    {
        if (root.GetComponent<SpawnedLightWatcher>() != null)
        {
            return;
        }

        Scene scene = root.scene;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        root.AddComponent<SpawnedLightWatcher>();
    }

    private static bool IsIgnoredSceneActive()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() &&
               activeScene.name.Equals(IgnoredSceneName, StringComparison.OrdinalIgnoreCase);
    }
}
