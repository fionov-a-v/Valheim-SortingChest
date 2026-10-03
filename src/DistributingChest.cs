using System.Collections.Generic;
using UnityEngine;

namespace SortingChest
{
    /// <summary>
    /// Метка распределяющего сундука на префабе и его «магия»: круг рун, лежащий на полу под сундуком и медленно
    /// вращающийся вокруг него, мерцающий свет и искры. Во время сортировки круг раскручивается, свет вспыхивает.
    /// Кольцо радиуса сортировки (как у охранного камня) видно, пока сундук ставят, и при наведении на него.
    /// </summary>
    public class DistributingChest : MonoBehaviour
    {
        public static readonly List<DistributingChest> All = new List<DistributingChest>();

        private const float LightIntensity = 1.1f;

        /// <summary>Скорость вращения круга рун, градусов в секунду: в покое и на пике сортировки.</summary>
        private const float CircleSpeed = 8f;
        private const float CircleSurgeSpeed = 180f;

        /// <summary>Идёт раскладка (сундук заперт до её конца).</summary>
        public bool IsSorting { get; set; }

        private Transform m_magic;
        private Transform m_circle;
        private Light m_light;
        private ParticleSystem m_motes;
        private GameObject m_marker;
        private CircleProjector m_projector;
        private bool m_ghost;
        private float m_markerUntil;
        private float m_surge;
        private float m_angle;
        private float m_phase;

        private void Awake()
        {
            // Призрак при строительстве (и превью иконки) создаётся с отключённой сетевой инициализацией.
            m_ghost = ZNetView.m_forceDisableInit;
            m_magic = transform.Find(MagicFx.RootName);
            if (m_magic != null)
            {
                m_circle = m_magic.Find(MagicFx.CircleName);
                m_light = m_magic.GetComponentInChildren<Light>(true);
                m_motes = m_magic.GetComponentInChildren<ParticleSystem>(true);
            }
            Transform marker = transform.Find(MagicFx.MarkerName);
            if (marker != null)
            {
                m_marker = marker.gameObject;
                m_projector = marker.GetComponent<CircleProjector>();
            }
            m_phase = Random.value * 100f;
            m_angle = Random.value * 360f;
            All.Add(this);
        }

        private void OnDestroy()
        {
            All.Remove(this);
        }

        /// <summary>Показать кольцо радиуса на полсекунды (вызывается при наведении, как у охранного камня).</summary>
        public void ShowMarker()
        {
            m_markerUntil = Time.time + 0.5f;
        }

        /// <summary>Вспышка: круг раскручивается, свет ярче, из круга бьют искры.</summary>
        public void Surge()
        {
            m_surge = 1f;
            if (m_motes == null || !m_motes.gameObject.activeInHierarchy)
            {
                return;
            }
            var emit = new ParticleSystem.EmitParams();
            Vector3 center = m_magic != null ? m_magic.position : transform.position;
            for (int i = 0; i < 36; i++)
            {
                float a = Random.value * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                emit.position = center + dir * MagicFx.CircleRadius * 0.82f + Vector3.up * 0.05f;
                emit.velocity = Vector3.up * Random.Range(1.2f, 2.6f) + dir * Random.Range(0.1f, 0.5f);
                emit.startLifetime = Random.Range(0.8f, 1.4f);
                emit.startSize = Random.Range(0.06f, 0.14f);
                m_motes.Emit(emit, 1);
            }
        }

        private void Update()
        {
            UpdateMarker();
            if (m_magic == null)
            {
                return;
            }
            bool show = ChestConfig.ShowEffects.Value;
            if (m_magic.gameObject.activeSelf != show)
            {
                m_magic.gameObject.SetActive(show);
            }
            if (!show)
            {
                return;
            }

            float dt = Time.deltaTime;
            float t = Time.time + m_phase;
            m_surge = Mathf.MoveTowards(m_surge, IsSorting ? 1f : 0f, dt / (IsSorting ? 0.2f : 1.6f));
            m_angle = Mathf.Repeat(m_angle + (CircleSpeed + CircleSurgeSpeed * m_surge) * dt, 360f);

            if (m_circle != null)
            {
                // Только вокруг вертикали: круг всегда лежит на полу, сундук — в его центре.
                m_circle.localRotation = Quaternion.Euler(0f, m_angle, 0f);
            }

            if (m_light != null)
            {
                m_light.intensity = LightIntensity * (0.85f + 0.15f * Mathf.Sin(t * 2.1f)) * (1f + 2.5f * m_surge);
            }
        }

        private void UpdateMarker()
        {
            if (m_marker == null)
            {
                return;
            }
            bool on = m_ghost || Time.time < m_markerUntil;
            if (m_marker.activeSelf != on)
            {
                m_marker.SetActive(on);
            }
            if (on && m_projector != null)
            {
                m_projector.m_radius = ChestConfig.Radius.Value;
            }
        }
    }
}
