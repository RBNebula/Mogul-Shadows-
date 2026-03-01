using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MogulShadows;

[HarmonyPatch]
internal static class InstantiatePatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        return typeof(Object)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == nameof(Object.Instantiate) &&
                             method.ReturnType != typeof(void));
    }

    private static void Postfix(Object? __result)
    {
        MogulShadowsPlugin.Instance?.HandleInstantiatedObject(__result);
    }
}
