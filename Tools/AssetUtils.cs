using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace WalkSimulator.Tools
{

    public static class AssetUtils
    {
        private static string FormatPath(string path)
        {
            return path.Replace("/", ".").Replace("\\", ".");
        }

        public static AssetBundle LoadAssetBundle(string path)
        {
            path = FormatPath(path);
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            if (stream == null)
            {
                Debug.LogError($"Resource not found: {path}");
                return null;
            }
            return AssetBundle.LoadFromStream(stream);
        }

        public static string[] GetResourceNames()
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            string[] names = callingAssembly.GetManifestResourceNames();
            if (names == null || names.Length == 0)
            {
                Debug.Log("No manifest resources found.");
                return Array.Empty<string>();
            }
            return names;
        }
    }
}
