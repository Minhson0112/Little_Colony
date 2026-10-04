using UnityEngine;

namespace LittleColony
{
    /// <summary>Lights the visible lamp shade and keeps its point light aligned with the imported mesh.</summary>
    public sealed class LanternGlow : MonoBehaviour
    {
        private Renderer shade;
        private Material shadeMaterial;
        private Color originalEmission;
        private Light pointLight;

        /// <summary>Creates a private emissive shade material and a stable per-pixel light.</summary>
        public void Initialize(GameObject model, Light light)
        {
            pointLight = light;
            // Vertex lighting interpolates across the large ground mesh and can smear one lamp over the garden.
            pointLight.renderMode = LightRenderMode.ForcePixel;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                if (renderer.name.StartsWith("Lamp body", System.StringComparison.Ordinal))
                {
                    shade = renderer;
                    break;
                }
            }
            if (shade != null)
            {
                shadeMaterial = new Material(shade.sharedMaterial);
                originalEmission = shadeMaterial.GetColor("_EmissionColor");
                shadeMaterial.EnableKeyword("_EMISSION");
                shade.sharedMaterial = shadeMaterial;
            }
            Align(model.transform.position);
            SetBrightness(0);
        }

        /// <summary>Follows the actual shade center after movement or rotation, accounting for FBX axis conversion.</summary>
        public void Align(Vector3 modelPosition)
        {
            transform.position = shade != null ? shade.bounds.center : modelPosition + Vector3.up * 1.57f;
        }

        /// <summary>Fades both the visible shade and surrounding illumination with the day-night cycle.</summary>
        public void SetBrightness(float amount)
        {
            float brightness = Mathf.Clamp01(amount);
            pointLight.enabled = brightness > .02f;
            pointLight.intensity = 1.8f * brightness;
            if (shadeMaterial != null)
            {
                shadeMaterial.SetColor("_EmissionColor", originalEmission + VillageWorld.ColorOf("#FFD077") * (1.6f * brightness));
            }
        }

        /// <summary>Releases the material owned by this lamp when the village is replaced or closed.</summary>
        private void OnDestroy()
        {
            if (shadeMaterial != null)
            {
                Destroy(shadeMaterial);
            }
        }
    }
}
