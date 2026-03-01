using System.Collections;
using UnityEngine;

namespace MogulShadows;

[DisallowMultipleComponent]
internal sealed class SpawnedLightWatcher : MonoBehaviour
{
    private int _framesRemaining;
    private Coroutine? _scanRoutine;

    private void OnEnable()
    {
        QueueScan(6);
    }

    private void OnTransformChildrenChanged()
    {
        QueueScan(6);
    }

    private IEnumerator ScanRoutine()
    {
        while (_framesRemaining > 0)
        {
            _framesRemaining--;
            MogulShadowsPlugin.Instance?.ApplyShadowsToGameObjectHierarchy(gameObject, "spawn watcher");
            yield return null;
        }

        _scanRoutine = null;
    }

    private void QueueScan(int frames)
    {
        if (frames > _framesRemaining)
        {
            _framesRemaining = frames;
        }

        if (_scanRoutine == null)
        {
            _scanRoutine = StartCoroutine(ScanRoutine());
        }
    }
}
