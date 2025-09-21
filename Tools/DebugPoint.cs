using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;

namespace WalkSimulator.Tools
{

    public class DebugPoint : MonoBehaviour
    {
        public static Dictionary<string, DebugPoint> points = new Dictionary<string, DebugPoint>();

        public float size = 0.1f;
        public Color color = Color.white;

        private Material material;

        private void Awake()
        {
            material = Object.Instantiate(Plugin.Instance.bundle.LoadAsset<Material>("m_xRay"));
            material.color = color;
            GetComponent<MeshRenderer>().material = material;
        }

        private void FixedUpdate()
        {
            material.color = color;
            transform.localScale = Vector3.one * size * GTPlayer.Instance.scale;
        }

        private void OnDestroy() => points.Remove(name);

        public static Transform Get(string name, Vector3 position, Color color = default, float size = 0.1f)
        {
            if (points.ContainsKey(name))
            {
                var dp = points[name];
                dp.color = color;
                dp.transform.position = position;
                dp.size = size;
                return dp.transform;
            }
            return Create(name, position, color, size);
        }

        private static Transform Create(string name, Vector3 position, Color color, float size)
        {
            var transform = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            transform.name = $"Cipher Debugger ({name})";
            transform.localScale = Vector3.one * 0.2f;
            transform.position = position;

            transform.GetComponent<Collider>().enabled = false;

            var mat = Object.Instantiate(GorillaTagger.Instance.offlineVRRig.mainSkin.material);
            transform.GetComponent<Renderer>().material = mat;

            var debugPoint = transform.gameObject.AddComponent<DebugPoint>();
            debugPoint.color = color;
            debugPoint.size = size;

            points.Add(name, debugPoint);
            return transform;
        }
    }
}
