using UnityEngine;
using UnityEngine.Rendering;

namespace LittleColony
{
    /// <summary>Draws drifting pollen and evening fireflies in one reusable, noninteractive mesh.</summary>
    public sealed class GardenAtmosphere : MonoBehaviour
    {
        private const int ParticleCount = 48;
        private readonly Vector3[] vertices = new Vector3[ParticleCount * 4];
        private readonly Color[] colors = new Color[ParticleCount * 4];
        private Mesh mesh;
        private Material material;
        private GameObject motes;

        /// <summary>Creates the fixed-size particle mesh without textures, lights, or gameplay colliders.</summary>
        public void Initialize()
        {
            mesh = new Mesh { name = "Garden pollen and fireflies" };
            mesh.MarkDynamic();
            var coordinates = new Vector2[vertices.Length];
            var triangles = new int[ParticleCount * 6];
            for (int i = 0; i < ParticleCount; i++)
            {
                int vertex = i * 4;
                coordinates[vertex] = Vector2.zero;
                coordinates[vertex + 1] = Vector2.right;
                coordinates[vertex + 2] = Vector2.one;
                coordinates[vertex + 3] = Vector2.up;
                int triangle = i * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 1;
                triangles[triangle + 2] = vertex + 2;
                triangles[triangle + 3] = vertex;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }

            mesh.vertices = vertices;
            mesh.uv = coordinates;
            mesh.triangles = triangles;
            material = new Material(Resources.Load<Shader>("Shaders/GardenMotes"));
            motes = new GameObject("Drifting garden life", typeof(MeshFilter), typeof(MeshRenderer));
            motes.transform.SetParent(transform, false);
            motes.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = motes.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        /// <summary>Drifts camera-facing particles and fades their warm glow with the saved daylight.</summary>
        public void Animate(Camera camera, float time, float daylight, bool raining)
        {
            motes.SetActive(!raining);
            if (raining)
            {
                return;
            }

            float evening = Mathf.Clamp01((.65f - daylight) * 2.4f);
            for (int i = 0; i < ParticleCount; i++)
            {
                float phase = i * 2.399f;
                Vector3 position = new Vector3(Mathf.Sin(phase) * 16 + Mathf.Sin(time * .23f + phase) * .7f,
                    .5f + Mathf.Repeat(i * .37f + time * .055f, 2.1f),
                    Mathf.Cos(phase * 1.7f) * 10 + Mathf.Cos(time * .18f + phase) * .5f);
                float pulse = .5f + .5f * Mathf.Sin(time * 1.9f + phase);
                float radius = Mathf.Lerp(.028f, .072f, evening) * (.75f + pulse * .25f);
                Vector3 right = camera.transform.right * radius;
                Vector3 up = camera.transform.up * radius;
                int vertex = i * 4;
                vertices[vertex] = position - right - up;
                vertices[vertex + 1] = position + right - up;
                vertices[vertex + 2] = position + right + up;
                vertices[vertex + 3] = position - right + up;
                Color color = Color.Lerp(new Color(1, .96f, .73f), new Color(.88f, 1, .45f), evening);
                color.a = Mathf.Lerp(.34f, .82f * pulse, evening);
                for (int corner = 0; corner < 4; corner++)
                {
                    colors[vertex + corner] = color;
                }
            }

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }

        /// <summary>Releases the runtime graphics resources when the world is destroyed.</summary>
        private void OnDestroy()
        {
            if (mesh != null)
            {
                Destroy(mesh);
            }

            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
