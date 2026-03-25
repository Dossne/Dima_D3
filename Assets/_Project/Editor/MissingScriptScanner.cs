using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TapMiner.EditorTools
{
    public static class MissingScriptScanner
    {
        [MenuItem("Tools/Tap Miner/Scan Missing Scripts")]
        public static void ScanMissingScripts()
        {
            var results = new List<string>();
            var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => !EditorUtility.IsPersistent(go))
                .Distinct()
                .OrderBy(go => go.scene.path)
                .ThenBy(go => GetHierarchyPath(go))
                .ToArray();

            foreach (var gameObject in gameObjects)
            {
                var missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
                if (missingCount <= 0)
                {
                    continue;
                }

                results.Add(
                    $"MissingScriptCount={missingCount} | Scene='{gameObject.scene.path}' | Path='{GetHierarchyPath(gameObject)}'");
            }

            if (results.Count == 0)
            {
                Debug.Log("[MissingScriptScanner] No missing script references found on loaded GameObjects.");
                return;
            }

            foreach (var result in results)
            {
                Debug.LogError($"[MissingScriptScanner] {result}");
            }
        }

        private static string GetHierarchyPath(GameObject gameObject)
        {
            var path = gameObject.name;
            var current = gameObject.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
