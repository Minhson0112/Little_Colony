using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleColony
{
    /// <summary>
    /// Presents orbiting fireflies and a gentle surface glow on interactable worksites.
    /// </summary>
    public sealed class WorkSiteFireflies : MonoBehaviour
    {
        private const int FireflyCount = 6;
        private readonly ParticleSystem.Particle[] particles = new ParticleSystem.Particle[FireflyCount];
        private ParticleSystem swarm;
        private Material glowMaterial;
        private float height;
        private readonly List<HighlightMaterial> highlights = new List<HighlightMaterial>();
        private bool highlighted;

        /// <summary>
        /// Keeps one private material instance and its original colors for reversible highlighting.
        /// </summary>
        private sealed class HighlightMaterial
        {
            public Material material;
            public Color color;
            public Color emission;
            public bool hasEmission;
        }

        /// <summary>
        /// Creates billboard fireflies and private highlight materials without colliders or saved state.
        /// </summary>
        public void Initialize()
        {
            height = .9f;
            var modelRenderers = GetComponentsInChildren<Renderer>();
            foreach (var modelRenderer in modelRenderers)
            {
                height = Mathf.Max(height, modelRenderer.bounds.max.y - transform.position.y);
            }

            InitializeHighlights(modelRenderers);
            var root = new GameObject("Worksite fireflies");
            root.transform.SetParent(transform, false);
            swarm = root.AddComponent<ParticleSystem>();
            swarm.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = swarm.main;
            main.playOnAwake = false;
            main.maxParticles = FireflyCount;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSpeed = 0;
            var emission = swarm.emission;
            emission.enabled = false;
            var shape = swarm.shape;
            shape.enabled = false;
            var swarmRenderer = root.GetComponent<ParticleSystemRenderer>();
            glowMaterial = new Material(Resources.Load<Shader>("Shaders/WorkSiteFirefly"));
            swarmRenderer.sharedMaterial = glowMaterial;
            swarmRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            swarmRenderer.shadowCastingMode = ShadowCastingMode.Off;
            swarmRenderer.receiveShadows = false;
        }

        /// <summary>
        /// Enables emission on private material copies while retaining the imported assets unchanged.
        /// </summary>
        private void InitializeHighlights(Renderer[] modelRenderers)
        {
            var copies = new Dictionary<Material, HighlightMaterial>();
            foreach (var modelRenderer in modelRenderers)
            {
                var materials = modelRenderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var original = materials[i];
                    if (original == null || !original.HasProperty("_Color"))
                    {
                        continue;
                    }

                    if (!copies.TryGetValue(original, out var highlight))
                    {
                        highlight = new HighlightMaterial
                        {
                            material = new Material(original),
                            color = original.color,
                            hasEmission = original.HasProperty("_EmissionColor")
                        };
                        if (highlight.hasEmission)
                        {
                            highlight.emission = original.GetColor("_EmissionColor");
                            highlight.material.EnableKeyword("_EMISSION");
                            highlight.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                        }

                        copies.Add(original, highlight);
                        highlights.Add(highlight);
                    }

                    materials[i] = highlight.material;
                }

                modelRenderer.sharedMaterials = materials;
            }
        }

        /// <summary>
        /// Slowly brightens surfaces while actionable and restores their original colors when work starts.
        /// </summary>
        private void AnimateHighlight(bool visible, float time, int buildingId)
        {
            if (!visible && !highlighted)
            {
                return;
            }

            float pulse = .5f + .5f * Mathf.Sin(time * 2.1f + buildingId * .73f);
            foreach (var highlight in highlights)
            {
                highlight.material.color = visible
                    ? Color.Lerp(highlight.color,
                        new Color(1f, .94f, .76f, highlight.color.a), .04f + .1f * pulse)
                    : highlight.color;
                if (highlight.hasEmission)
                {
                    highlight.material.SetColor("_EmissionColor", visible
                        ? highlight.emission + highlight.color.linear * (.08f + .26f * pulse)
                        : highlight.emission);
                }
            }

            highlighted = visible;
        }

        /// <summary>
        /// Shows fireflies and surface glow when work can be assigned, harvested, watered, or restocked.
        /// </summary>
        /// <param name="building">The current worksite state; working jobs and upgrades suppress the effects.</param>
        /// <param name="time">The shared visual clock, used without altering Unity's random state.</param>
        public void Animate(Building building, float time)
        {
            bool visible = building.upgradeRemaining <= 0
                && (building.phase == JobPhase.Idle
                    || building.phase == JobPhase.Ready
                    || building.phase == JobPhase.Empty);
            AnimateHighlight(visible, time, building.id);
            if (!visible)
            {
                if (swarm.gameObject.activeSelf)
                {
                    swarm.Clear();
                    swarm.gameObject.SetActive(false);
                }

                return;
            }

            swarm.gameObject.SetActive(true);
            Color tint = building.phase == JobPhase.Ready
                ? new Color(1f, .82f, .28f)
                : new Color(.8f, 1f, .36f);
            for (int i = 0; i < FireflyCount; i++)
            {
                float phase = i * Mathf.PI * 2 / FireflyCount + building.id * .73f;
                float angle = phase + time * (.45f + i * .037f);
                float radius = .7f + .16f * Mathf.Sin(time * .9f + phase * 2);
                float pulse = .65f + .35f * Mathf.Sin(time * 2.3f + phase);
                particles[i].position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    height + .3f + .18f * Mathf.Sin(time * 1.4f + phase),
                    Mathf.Sin(angle) * radius * .72f);
                particles[i].startSize = .38f + .07f * pulse;
                particles[i].startColor = new Color(tint.r, tint.g, tint.b, .6f + .4f * pulse);
                particles[i].startLifetime = 1000;
                particles[i].remainingLifetime = 1000;
                particles[i].rotation = Mathf.Sin(time * .8f + phase) * 25;
            }

            swarm.SetParticles(particles, FireflyCount);
        }

        /// <summary>
        /// Restores surface colors when a replaced model or its scene becomes inactive.
        /// </summary>
        private void OnDisable()
        {
            AnimateHighlight(false, 0, 0);
        }

        /// <summary>
        /// Releases transient firefly and highlight materials when the building is removed.
        /// </summary>
        private void OnDestroy()
        {
            if (glowMaterial != null)
            {
                Destroy(glowMaterial);
            }

            foreach (var highlight in highlights)
            {
                Destroy(highlight.material);
            }
        }
    }
}
