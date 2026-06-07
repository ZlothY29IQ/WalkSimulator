using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Random = System.Random;

namespace WalkSimulator.Tools;

public static class Extensions
{
    private static readonly Random rng = new();

    public static T GetOrAddComponent<T>(this GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();

        return component != null ? component : obj.AddComponent<T>();
    }

    public static void Obliterate(this GameObject self) => Object.Destroy(self);

    public static void Obliterate(this Component self) => Object.Destroy(self);

    public static float Distance(this Vector3 self, Vector3 other) => Vector3.Distance(self, other);

    public static int Wrap(int x, int min, int max)
    {
        int range   = max - min;
        int wrapped = (x - min) % range;
        if (wrapped < 0)
            wrapped += range;

        return wrapped + min;
    }

    public static float Map(float x, float a1, float a2, float b1, float b2)
    {
        float t = (x - a1) / (a2 - a1);

        return b1 + t * (b2 - b1);
    }

    public static byte[] StringToBytes(this string str) => Encoding.UTF8.GetBytes(str);

    public static string BytesToString(this byte[] bytes) => Encoding.UTF8.GetString(bytes);

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k     = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}