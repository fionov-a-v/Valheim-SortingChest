using System;
using System.Collections.Generic;
using SortingChest.Visuals;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace SortingChest
{
    /// <summary>
    /// «Магия» распределяющего сундука без ассет-бандлов: круг рун на полу рисуется кодом (RuneArt), материалы — на встроенном
    /// Sprites/Default (без «мягких частиц», поэтому круг, лежащий на земле, не растворяется), с HDR-цветом под bloom.
    /// Кольцо радиуса берётся у охранного камня. Огоньки к сундукам рассылаются всем игрокам рядом.
    /// </summary>
    internal static class MagicFx
    {
        public const string RootName = "sortingchest_magic";
        public const string CircleName = "rune_circle";
        public const string MarkerName = "sortingchest_area";

        /// <summary>Радиус круга рун у основания сундука, м.</summary>
        public const float CircleRadius = 1.5f;

        private const string RpcFx = "SortingChest_Fx";
        private const float FxDistance = 80f;
        private const int MaxWisps = 48;

        public static readonly Color Teal = new Color(0.3f, 0.88f, 1f);

        private static Material s_circleMat;
        private static Material s_dotMat;
        private static Material s_bodyMat;
        private static Mesh s_circleMesh;
        private static Mesh s_dotMesh;
        private static GameObject s_areaMarker;
        private static bool s_ready;

        // ================================================================== материалы

        public static void Init()
        {
            if (s_ready)
            {
                return;
            }
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader == null)
            {
                Logger.LogWarning("Sorting Chest: шейдер Sprites/Default не найден — магия будет без свечения");
            }

            Texture2D circle = Texture("sortingchest_circle", RuneArt.Circle(512), 512, 512);
            Texture2D dot = Texture("sortingchest_dot", RuneArt.Dot(64), 64, 64);

            // Цвет больше 1 — HDR: ярче белого, камера игры даёт на нём ореол (bloom).
            s_circleMat = Glow(shader, "sortingchest_circle", circle, 1.5f);
            s_dotMat = Glow(shader, "sortingchest_dot", dot, 2.4f);

            s_circleMesh = FlatQuad(CircleRadius);
            s_dotMesh = Quad(0.22f, 0.22f, 0f, 1f);

            GameObject ward = PrefabManager.Instance.GetPrefab("guard_stone");
            PrivateArea area = ward != null ? ward.GetComponent<PrivateArea>() : null;
            s_areaMarker = area != null && area.m_areaMarker != null ? area.m_areaMarker.gameObject : null;
            if (s_areaMarker == null)
            {
                Logger.LogWarning("Sorting Chest: кольцо радиуса охранного камня не найдено — радиус не будет показан");
            }
            s_ready = true;
        }

        private static Texture2D Texture(string name, byte[] rgba, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
            };
            // Не LoadRawTextureData: при текстуре с mip-уровнями он ждёт данные сразу для всех уровней.
            // SetPixels32 задаёт только нулевой, остальные строит Apply.
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], rgba[i * 4 + 3]);
            }
            tex.SetPixels32(pixels);
            tex.Apply(true, true);
            return tex;
        }

        private static Material Glow(Shader shader, string name, Texture2D tex, float hdr)
        {
            var m = new Material(shader) { name = name, mainTexture = tex };
            m.color = new Color(hdr, hdr, hdr, 1f);
            return m;
        }

        /// <summary>Квад в плоскости XY (лицом к -Z), u вдоль X — участок атласа [u0, u1].</summary>
        private static Mesh Quad(float halfW, float halfH, float u0, float u1)
        {
            var mesh = new Mesh { name = "sortingchest_quad" };
            mesh.vertices = new[]
            {
                new Vector3(-halfW, -halfH, 0f), new Vector3(halfW, -halfH, 0f),
                new Vector3(halfW, halfH, 0f), new Vector3(-halfW, halfH, 0f),
            };
            mesh.uv = new[] { new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2(u1, 1f), new Vector2(u0, 1f) };
            mesh.colors32 = White(4);
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Лежащий квад (круг рун): u вдоль +X, v вдоль +Z — сверху руны читаются не зеркально.</summary>
        private static Mesh FlatQuad(float r)
        {
            var mesh = new Mesh { name = "sortingchest_circle" };
            mesh.vertices = new[]
            {
                new Vector3(-r, 0f, -r), new Vector3(r, 0f, -r), new Vector3(r, 0f, r), new Vector3(-r, 0f, r),
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.colors32 = White(4);
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Color32[] White(int n)
        {
            var c = new Color32[n];
            for (int i = 0; i < n; i++)
            {
                c[i] = new Color32(255, 255, 255, 255);
            }
            return c;
        }

        // ================================================================== сборка префаба

        /// <summary>Добавить к клону армированного сундука тонировку, круг рун на полу, свет, искры и кольцо радиуса.</summary>
        public static void Build(GameObject prefab)
        {
            // Основание модели считаем до того, как добавим свои меши.
            Vector3 baseCenter = ModelBase(prefab, out float height);
            TintBody(prefab);

            var root = new GameObject(RootName);
            root.transform.SetParent(prefab.transform, false);
            root.transform.localPosition = baseCenter;

            // Круг лежит на полу под сундуком, сундук — ровно в его центре; вращает круг DistributingChest.
            var circle = new GameObject(CircleName);
            circle.transform.SetParent(root.transform, false);
            circle.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            AddRenderer(circle, s_circleMesh, s_circleMat);

            var lightGo = new GameObject("light");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, height + 0.3f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Teal;
            light.range = 4f;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;

            var motes = new GameObject("motes");
            motes.transform.SetParent(root.transform, false);
            motes.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            Motes(motes);

            if (s_areaMarker != null)
            {
                GameObject marker = Object.Instantiate(s_areaMarker, prefab.transform, false);
                marker.name = MarkerName;
                marker.transform.localPosition = new Vector3(baseCenter.x, 0f, baseCenter.z);
                marker.SetActive(false);
            }
        }

        /// <summary>
        /// Центр дна модели сундука в координатах префаба и её высота — по мешам видимых частей
        /// (Renderer.bounds у неактивного префаба пустые; выключенные части — снег, открытая крышка — не считаем).
        /// </summary>
        private static Vector3 ModelBase(GameObject prefab, out float height)
        {
            Transform root = prefab.transform;
            bool any = false;
            Vector3 min = Vector3.zero, max = Vector3.zero;
            foreach (MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || !ActiveUnder(mf.transform, root))
                {
                    continue;
                }
                Bounds b = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? b.min.x : b.max.x,
                        (i & 2) == 0 ? b.min.y : b.max.y,
                        (i & 4) == 0 ? b.min.z : b.max.z);
                    Vector3 p = root.InverseTransformPoint(mf.transform.TransformPoint(corner));
                    min = any ? Vector3.Min(min, p) : p;
                    max = any ? Vector3.Max(max, p) : p;
                    any = true;
                }
            }
            if (!any)
            {
                height = 1.1f;
                return Vector3.zero;
            }
            height = max.y - min.y;
            return new Vector3((min.x + max.x) * 0.5f, min.y, (min.z + max.z) * 0.5f);
        }

        private static bool ActiveUnder(Transform t, Transform root)
        {
            for (; t != null && t != root; t = t.parent)
            {
                if (!t.gameObject.activeSelf)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Железо и дерево сундука — с холодным голубым отливом, чтобы его было видно среди обычных.</summary>
        private static void TintBody(GameObject prefab)
        {
            foreach (MeshRenderer r in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !mats[i].name.StartsWith("ironchest", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    if (s_bodyMat == null)
                    {
                        s_bodyMat = new Material(mats[i]) { name = "sortingchest_body" };
                        s_bodyMat.color = new Color(0.72f, 0.84f, 1f, 1f);
                    }
                    mats[i] = s_bodyMat;
                    changed = true;
                }
                if (changed)
                {
                    r.sharedMaterials = mats;
                }
            }
        }

        private static void AddRenderer(GameObject go, Mesh mesh, Material mat)
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        /// <summary>Редкие искры, поднимающиеся от круга рун.</summary>
        private static void Motes(GameObject go)
        {
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 2f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(Teal, new Color(0.8f, 1f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 7f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = CircleRadius * 0.82f;
            shape.radiusThickness = 0.15f;
            shape.rotation = new Vector3(90f, 0f, 0f); // круг формы — в плоскости XY, кладём на землю

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = s_dotMat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // ================================================================== огоньки к сундукам

        public static void RegisterRpc()
        {
            ZRoutedRpc.instance?.Register<ZPackage>(RpcFx, RPC_Fx);
        }

        /// <summary>Разослать всем (и себе): из распределяющего сундука к каждому сундуку, куда что-то легло, летит огонёк.</summary>
        public static void Broadcast(Vector3 from, List<Vector3> to)
        {
            var pkg = new ZPackage();
            pkg.Write(from);
            int count = Math.Min(to.Count, MaxWisps);
            pkg.Write(count);
            for (int i = 0; i < count; i++)
            {
                pkg.Write(to[i]);
            }
            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RpcFx, pkg);
            }
        }

        private static void RPC_Fx(long sender, ZPackage pkg)
        {
            Vector3 from = pkg.ReadVector3();
            int count = Math.Min(pkg.ReadInt(), MaxWisps);
            var to = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                to.Add(pkg.ReadVector3());
            }
            Player player = Player.m_localPlayer;
            if (player == null || !s_ready || !ChestConfig.ShowEffects.Value ||
                Vector3.Distance(player.transform.position, from) > FxDistance)
            {
                return;
            }
            foreach (DistributingChest chest in DistributingChest.All)
            {
                if (chest != null && Vector3.Distance(chest.transform.position, from) < 0.5f)
                {
                    chest.Surge();
                }
            }
            for (int i = 0; i < to.Count; i++)
            {
                Wisp.Spawn(from, to[i], i * 0.07f, s_dotMat, s_dotMesh);
            }
        }
    }

    /// <summary>Огонёк: летит по дуге от распределяющего сундука к сундуку, оставляя след искр, и рассыпается над ним.</summary>
    internal sealed class Wisp : MonoBehaviour
    {
        private Vector3 m_from;
        private Vector3 m_to;
        private Vector3 m_control;
        private float m_delay;
        private float m_duration;
        private float m_time;
        private bool m_arrived;
        private Transform m_head;
        private ParticleSystem m_trail;
        private Light m_light;

        public static void Spawn(Vector3 from, Vector3 to, float delay, Material mat, Mesh headMesh)
        {
            var go = new GameObject("sortingchest_wisp");
            Wisp w = go.AddComponent<Wisp>();
            w.m_from = from + Vector3.up * 1.3f;
            w.m_to = to + Vector3.up * 1.0f;
            float distance = Vector3.Distance(w.m_from, w.m_to);
            w.m_control = (w.m_from + w.m_to) * 0.5f + Vector3.up * (1.2f + distance * 0.18f);
            w.m_duration = 0.45f + distance * 0.035f;
            w.m_delay = delay;
            go.transform.position = w.m_from;

            var head = new GameObject("head");
            head.transform.SetParent(go.transform, false);
            head.AddComponent<MeshFilter>().sharedMesh = headMesh;
            MeshRenderer hr = head.AddComponent<MeshRenderer>();
            hr.sharedMaterial = mat;
            hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            w.m_head = head.transform;
            head.SetActive(false);

            w.m_light = go.AddComponent<Light>();
            w.m_light.type = LightType.Point;
            w.m_light.color = MagicFx.Teal;
            w.m_light.range = 3f;
            w.m_light.intensity = 0f;
            w.m_light.shadows = LightShadows.None;

            w.m_trail = Trail(go, mat);
            Destroy(go, delay + w.m_duration + 2f);
        }

        private static ParticleSystem Trail(GameObject go, Material mat)
        {
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.duration = 5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(MagicFx.Teal, new Color(0.85f, 1f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 28f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(fade);
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        private void Update()
        {
            if (m_arrived)
            {
                m_light.intensity = Mathf.MoveTowards(m_light.intensity, 0f, Time.deltaTime * 4f);
                return;
            }
            if (m_delay > 0f)
            {
                m_delay -= Time.deltaTime;
                if (m_delay > 0f)
                {
                    return;
                }
                m_head.gameObject.SetActive(true);
                ParticleSystem.EmissionModule emission = m_trail.emission;
                emission.enabled = true;
                m_light.intensity = 1.6f;
            }
            m_time += Time.deltaTime;
            float t = Mathf.Clamp01(m_time / m_duration);
            float s = t * t * (3f - 2f * t);
            float u = 1f - s;
            transform.position = u * u * m_from + 2f * u * s * m_control + s * s * m_to;

            Transform cam = GameCamera.instance != null ? GameCamera.instance.transform : null;
            if (cam != null)
            {
                m_head.rotation = Quaternion.LookRotation(m_head.position - cam.position, Vector3.up);
            }
            if (t >= 1f)
            {
                Arrive();
            }
        }

        private void Arrive()
        {
            m_arrived = true;
            m_head.gameObject.SetActive(false);
            ParticleSystem.EmissionModule emission = m_trail.emission;
            emission.enabled = false;
            var burst = new ParticleSystem.EmitParams();
            for (int i = 0; i < 22; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                burst.position = m_to + dir * 0.1f;
                burst.velocity = dir * Random.Range(0.6f, 1.6f) + Vector3.up * 0.4f;
                burst.startLifetime = Random.Range(0.4f, 0.9f);
                burst.startSize = Random.Range(0.06f, 0.15f);
                m_trail.Emit(burst, 1);
            }
            m_light.intensity = 2.4f;
        }
    }
}
