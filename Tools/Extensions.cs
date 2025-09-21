using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WalkSimulator.Tools
{
    public static class Extensions
    {
        private static readonly System.Random rng = new System.Random();

        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            return component != null ? component : obj.AddComponent<T>();
        }

        public static void Obliterate(this GameObject self) => UnityEngine.Object.Destroy(self);

        public static void Obliterate(this Component self) => UnityEngine.Object.Destroy(self);

        public static float Distance(this Vector3 self, Vector3 other)
        {
            return Vector3.Distance(self, other);
        }

        public static int Wrap(int x, int min, int max)
        {
            int range = max - min;
            int wrapped = (x - min) % range;
            if (wrapped < 0)
            {
                wrapped += range;
            }
            return wrapped + min;
        }

        public static float Map(float x, float a1, float a2, float b1, float b2)
        {
            float t = (x - a1) / (a2 - a1);
            return b1 + t * (b2 - b1);
        }

        public static byte[] StringToBytes(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static string BytesToString(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
