using System.Collections.Generic;
using UnityEngine;

namespace HOne.Game
{
    // One authored collision rectangle, rendered as grid cells rather than a stretched picture.
    // Thin/partial cells crop their UVs. Collider, metadata and puzzle coordinates remain authoritative.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class GrassTerrainView : MonoBehaviour
    {
        [SerializeField] private DeliveredArtSet art;
        [SerializeField, Min(0.25f)] private float cellSize = 1f;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        private Mesh mesh;
        private Vector2 builtSize;
        private Vector2 builtOffset;
        private Vector3 builtPosition;
        private Quaternion builtRotation;
        private static int revision;
        private int builtRevision = -1;
        public float CellSize => cellSize;

        public void Configure(DeliveredArtSet delivery)
        {
            art = delivery;
            var visual = transform.Find("Visual");
            if (visual != null && visual.TryGetComponent<SpriteRenderer>(out var old)) old.enabled = false;
            var child = transform.Find("GrassCells");
            if (child == null) { child = new GameObject("GrassCells").transform; child.SetParent(transform, false); }
            meshFilter = child.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = child.gameObject.AddComponent<MeshFilter>();
            meshRenderer = child.GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = child.gameObject.AddComponent<MeshRenderer>();
            Rebuild();
        }
        private void OnEnable() { revision++; if (meshFilter != null) Rebuild(); }
        private void OnDisable() { revision++; }
        private void Start() { Physics2D.SyncTransforms(); Rebuild(); }
        private void LateUpdate()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box.size != builtSize || box.offset != builtOffset || transform.position != builtPosition || transform.rotation != builtRotation)
            { Physics2D.SyncTransforms(); revision++; }
            if (builtRevision != revision) Rebuild();
        }
        public void Rebuild()
        {
            if (art == null || art.TerrainMiddle == null || meshFilter == null) return;
            var box = GetComponent<BoxCollider2D>();
            builtSize = box.size; builtOffset = box.offset;
            builtPosition=transform.position; builtRotation=transform.rotation; builtRevision=revision;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            // All four images use one atlas prepared by the Editor. One draw call per patch.
            var columns = Mathf.CeilToInt(box.size.x / cellSize);
            var rows = Mathf.CeilToInt(box.size.y / cellSize);
            for (var row = 0; row < rows; row++)
            for (var col = 0; col < columns; col++)
            {
                var width = Mathf.Min(cellSize, box.size.x - col * cellSize);
                var height = Mathf.Min(cellSize, box.size.y - row * cellSize);
                var top = box.offset.y+box.size.y*.5f-row*cellSize;
                var left = box.offset.x-box.size.x*.5f+col*cellSize;
                var buried = row > 0 || Neighbour(new Vector2(left+width*.5f,top+.01f));
                var leftEnd = col == 0 && !Neighbour(new Vector2(left-.01f,top-height*.5f));
                var rightEnd = col == columns-1 && !Neighbour(new Vector2(left+width+.01f,top-height*.5f));
                // Cropping only the transparent corners would make a narrow wall look like a twig.
                var narrow = columns == 1 && width < cellSize*.75f;
                var sprite = buried ? art.TerrainSoil : narrow ? art.TerrainMiddle : leftEnd ? art.TerrainLeft : rightEnd ? art.TerrainRight : art.TerrainMiddle;
                // Single-cell patches combine the two end drawings at the centre.
                if (!buried && !narrow && leftEnd && rightEnd && columns == 1)
                {
                    Quad(art.TerrainLeft, col * cellSize, row * cellSize, width * .5f, height, 0f);
                    Quad(art.TerrainRight, col * cellSize + width * .5f, row * cellSize, width * .5f, height, 1f - width * .5f / cellSize);
                }
                else Quad(sprite, col * cellSize, row * cellSize, width, height, col == columns - 1 ? 1f - width / cellSize : 0f);
            }
            if (mesh == null) { mesh = new Mesh { name = "Grass grid cells", hideFlags = HideFlags.DontSave }; }
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh; meshRenderer.sharedMaterial = art.TerrainMaterial; meshRenderer.sortingOrder = -1;

            bool Neighbour(Vector2 point)
            {
                foreach(var other in Physics2D.OverlapPointAll(transform.TransformPoint(point)))
                    if(other.gameObject!=gameObject && !other.isTrigger && other.GetComponent<GrassTerrainView>() is GrassTerrainView terrain && terrain.isActiveAndEnabled)
                        return true;
                return false;
            }

            void Quad(Sprite sprite, float x, float y, float width, float height, float u0)
            {
                var index = sprite == art.TerrainLeft ? 0 : sprite == art.TerrainMiddle ? 1 : sprite == art.TerrainRight ? 2 : 3;
                var rect = art.TerrainAtlasRects[index];
                var left = box.offset.x - box.size.x * .5f + x;
                var top = box.offset.y + box.size.y * .5f - y;
                var n = vertices.Count;
                vertices.Add(new Vector3(left, top - height)); vertices.Add(new Vector3(left + width, top - height));
                vertices.Add(new Vector3(left + width, top)); vertices.Add(new Vector3(left, top));
                var uMin = rect.x + rect.width * u0;
                var uMax = rect.x + rect.width * (u0 + width / cellSize);
                var vMax = rect.yMax;
                var vMin = rect.yMax - rect.height * height / cellSize;
                uv.Add(new Vector2(uMin,vMin)); uv.Add(new Vector2(uMax,vMin)); uv.Add(new Vector2(uMax,vMax)); uv.Add(new Vector2(uMin,vMax));
                triangles.Add(n); triangles.Add(n+2); triangles.Add(n+1); triangles.Add(n); triangles.Add(n+3); triangles.Add(n+2);
            }
        }
        private void OnDestroy()
        {
            if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
        }
    }
}
