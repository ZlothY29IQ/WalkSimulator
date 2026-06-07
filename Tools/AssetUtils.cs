using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace WalkSimulator.Tools
{
    public static class AssetUtils
    {
        private static string FormatPath(string path) => path.Replace("/", ".").Replace("\\", ".");

        public static AssetBundle LoadAssetBundle(string path)
        {
            path = FormatPath(path);
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);

            if (stream != null)
                return AssetBundle.LoadFromStream(stream);

            Debug.LogError($"Resource not found: {path}");

            return null;

        }

        public static string[] GetResourceNames()
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            string[] names           = callingAssembly.GetManifestResourceNames();

            if (names.Length != 0)
                return names;

            Debug.Log("No manifest resources found.");

            return Array.Empty<string>();

        }
    }
}