using System.Collections.Generic;
using UnityEngine;

namespace HOne.Game
{
    // Presentation only: one reusable mesh and bounded event/path buffers. Nothing here moves a body.
    public sealed class ImpactFeedbackView : MonoBehaviour
    {
        private const int MaximumBursts = 24;
        private const int MaximumTrailPoints = 96;
        private const float TrailSeconds = .14f;
        [SerializeField] private PlayerStateMachine player;
        [SerializeField] private int sortingOrder = 24;
        private static readonly Color Gold = new Color(1f, .79f, .34f);
        private static readonly Color PaleGold = new Color(1f, .91f, .64f);
        private static readonly Color Stone = new Color(.66f, .69f, .7f);
        private static readonly Color Terracotta = new Color(.8f, .39f, .24f);
        private static readonly Color Blue = new Color(.45f, .63f, .8f);
        private enum BurstKind { Stone, Lyre, Drum, Ceramic, Embed, Reject, Pickup, Failure, Reform, Launch, Checkpoint }
        private struct Burst
        {
            public bool Active;
            public BurstKind Kind;
            public Vector2 Position, Direction, Size;
            public float Started, Duration, Strength;
            public uint Seed;
        }
        private struct TrailPoint
        {
            public Vector2 Position;
            public float Time;
        }
        private readonly Burst[] bursts = new Burst[MaximumBursts];
        private readonly List<TrailPoint> trail = new List<TrailPoint>(MaximumTrailPoints);
        private readonly List<Vector3> vertices = new List<Vector3>(4096);
        private readonly List<Color> colors = new List<Color>(4096);
        private readonly List<int> triangles = new List<int>(6144);
        private PlayerStateMachine boundPlayer;
        private LevelSession boundSession;
        private Mesh mesh;
        private Material material;
        private MeshRenderer drawing;
        private Transform geometry;
        private uint sequence;

        public int ActiveBurstCount
        {
            get
            {
                var count = 0;
                foreach (var burst in bursts)
                    if (burst.Active && Time.time - burst.Started < burst.Duration) count++;
                return count;
            }
        }
        public int TrailPointCount => trail.Count;

        public void Configure(PlayerStateMachine owner)
        {
            Unbind();
            Clear();
            player = owner;
            if (isActiveAndEnabled) Bind();
        }

        private void OnEnable()
        {
            EnsureGeometry();
            Bind();
        }

        private void Bind()
        {
            if (player == null || boundPlayer == player) return;
            boundPlayer = player;
            boundPlayer.StarImpact += Impact;
            boundPlayer.StarPathAdvanced += PathAdvanced;
            boundPlayer.StateChanged += StateChanged;
            boundPlayer.Reformed += Reformed;
            BindSession();
        }

        private void BindSession()
        {
            UnbindSession();
            boundSession = player != null ? player.Session : null;
            if (boundSession == null) return;
            boundSession.FragmentCollected += Collected;
            boundSession.WallBroken += WallBroken;
            boundSession.CheckpointChanged += CheckpointChanged;
            boundSession.ProgressReset += Clear;
            boundSession.PuzzleReset += Clear;
        }

        private void UnbindSession()
        {
            if (boundSession == null) return;
            boundSession.FragmentCollected -= Collected;
            boundSession.WallBroken -= WallBroken;
            boundSession.CheckpointChanged -= CheckpointChanged;
            boundSession.ProgressReset -= Clear;
            boundSession.PuzzleReset -= Clear;
            boundSession = null;
        }

        private void Unbind()
        {
            if (boundPlayer != null)
            {
                boundPlayer.StarImpact -= Impact;
                boundPlayer.StarPathAdvanced -= PathAdvanced;
                boundPlayer.StateChanged -= StateChanged;
                boundPlayer.Reformed -= Reformed;
            }
            boundPlayer = null;
            UnbindSession();
        }

        private void OnDisable() { Unbind(); Clear(); }
        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
            if (geometry != null) Destroy(geometry.gameObject);
        }

        private void EnsureGeometry()
        {
            if (!Application.isPlaying || mesh != null) return;
            geometry = new GameObject("ImpactGeometry").transform;
            geometry.gameObject.hideFlags = HideFlags.DontSave;
            geometry.SetParent(transform, false);
            mesh = new Mesh { name = "Procedural impact feedback", hideFlags = HideFlags.DontSave };
            mesh.MarkDynamic();
            geometry.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            drawing = geometry.gameObject.AddComponent<MeshRenderer>();
            material = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
            drawing.sharedMaterial = material;
            drawing.sortingOrder = sortingOrder;
            drawing.enabled = false;
        }

        private void Clear()
        {
            for (var i = 0; i < bursts.Length; i++) bursts[i].Active = false;
            trail.Clear();
            if (mesh != null) mesh.Clear();
            if (drawing != null) drawing.enabled = false;
        }

        private void Spawn(BurstKind kind, Vector2 position, Vector2 direction, float duration,
            float strength = 1f, Vector2 size = default)
        {
            var index = 0;
            for (var i = 0; i < bursts.Length; i++)
            {
                if (!bursts[i].Active || Time.time - bursts[i].Started >= bursts[i].Duration) { index = i; break; }
                if (bursts[i].Started < bursts[index].Started) index = i;
            }
            bursts[index] = new Burst
            {
                Active = true, Kind = kind, Position = position,
                Direction = direction.sqrMagnitude > .001f ? direction.normalized : Vector2.up,
                Started = Time.time, Duration = duration, Strength = strength, Size = size, Seed = ++sequence
            };
        }

        private void Impact(StarContact contact)
        {
            // A collectible's completed session event owns its feedback, including multi-collider pickups.
            if (contact.Collider != null && contact.Collider.GetComponentInParent<StarFragment>() != null) return;
            var direction = contact.Normal.sqrMagnitude > .001f ? contact.Normal : -contact.IncomingDirection;
            var strength = Mathf.Lerp(.75f, 1.15f, contact.Charge);
            switch (contact.Kind)
            {
                case StarImpactKind.Lyre: Spawn(BurstKind.Lyre, contact.Point, direction, .36f, strength); break;
                case StarImpactKind.Drum: Spawn(BurstKind.Drum, contact.Point, direction, .25f, strength); break;
                case StarImpactKind.Ceramic:
                    // WallBroken owns the shards, also when the drum wave breaks a distant wall.
                    Spawn(BurstKind.Drum, contact.Point, direction, .2f, strength); break;
                case StarImpactKind.Socket: Spawn(BurstKind.Embed, contact.Point, direction, .42f); break;
                case StarImpactKind.UnavailableSocket: Spawn(BurstKind.Reject, contact.Point, direction, .3f); break;
                case StarImpactKind.Hazard: break; // The ensuing failure state owns the dissolve.
                default: Spawn(BurstKind.Stone, contact.Point, direction, .36f, strength); break;
            }
        }

        private void WallBroken(BrittleWall wall)
        {
            if (wall == null) return;
            var center = (Vector2)wall.transform.position;
            var size = Vector2.one;
            var box = wall.GetComponentInChildren<BoxCollider2D>(true);
            if (box != null)
            {
                center = box.transform.TransformPoint(box.offset);
                size = Vector2.Scale(box.size, new Vector2(Mathf.Abs(box.transform.lossyScale.x), Mathf.Abs(box.transform.lossyScale.y)));
            }
            Spawn(BurstKind.Ceramic, center, Vector2.up, .6f, 1f, size);
        }

        private void Collected(string id)
        {
            if (boundSession == null) return;
            // Collections are rare; query current content here so runtime editor additions/removals work.
            foreach (var fragment in boundSession.GetComponentsInChildren<StarFragment>(true))
                if (fragment.StableId == id)
                {
                    Spawn(BurstKind.Pickup, fragment.transform.position, Vector2.up, .48f);
                    return;
                }
        }

        private void CheckpointChanged(Checkpoint checkpoint)
        {
            if (checkpoint != null) Spawn(BurstKind.Checkpoint, checkpoint.transform.position, Vector2.up, .5f);
        }

        private void StateChanged(PlayerState state)
        {
            if (state == PlayerState.StarFlight)
            {
                trail.Clear();
                Spawn(BurstKind.Launch, player.Position, player.AimDirection, .2f, .7f);
            }
            else if (state == PlayerState.Reforming)
            {
                if (player.ReformReason == ReformReason.PuzzleReset) Clear();
                else if (player.ReformReason == ReformReason.Failure)
                    Spawn(BurstKind.Failure, player.Position, Vector2.up, .4f);
            }
        }

        private void Reformed()
        {
            // Teleport is a discontinuity, never a segment connecting the failure point and safe dais.
            trail.Clear();
            Spawn(BurstKind.Reform, player.Position, Vector2.up, .42f);
        }

        private void PathAdvanced(IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count == 0) return;
            if (trail.Count > 0 && Vector2.SqrMagnitude(trail[trail.Count - 1].Position - points[0]) > .01f)
                trail.Clear();
            for (var i = 0; i < points.Count; i++)
            {
                if (trail.Count > 0 && Vector2.SqrMagnitude(trail[trail.Count - 1].Position - points[i]) < .000001f) continue;
                if (trail.Count == MaximumTrailPoints) trail.RemoveAt(0);
                trail.Add(new TrailPoint { Position = points[i], Time = Time.time });
            }
        }

        private void LateUpdate()
        {
            if (player == null) return;
            if (player.Session != boundSession) { Clear(); BindSession(); }
            EnsureGeometry();
            if (mesh == null) return;
            vertices.Clear(); colors.Clear(); triangles.Clear();
            while (trail.Count > 0 && Time.time - trail[0].Time > TrailSeconds) trail.RemoveAt(0);
            DrawTrail();
            for (var i = 0; i < bursts.Length; i++)
            {
                if (!bursts[i].Active) continue;
                var age = Time.time - bursts[i].Started;
                if (age >= bursts[i].Duration) { bursts[i].Active = false; continue; }
                DrawBurst(bursts[i], age);
            }
            mesh.Clear();
            drawing.enabled = vertices.Count > 0;
            if (!drawing.enabled) return;
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private void DrawTrail()
        {
            for (var i = 1; i < trail.Count; i++)
            {
                var a = trail[i - 1]; var b = trail[i];
                var fadeA = Mathf.Clamp01(1f - (Time.time - a.Time) / TrailSeconds);
                var fadeB = Mathf.Clamp01(1f - (Time.time - b.Time) / TrailSeconds);
                Line(a.Position, b.Position, .13f * fadeA, .13f * fadeB, Tint(Gold, .15f * fadeA), Tint(Gold, .15f * fadeB));
                Line(a.Position, b.Position, .045f * fadeA, .045f * fadeB, Tint(PaleGold, .65f * fadeA), Tint(PaleGold, .65f * fadeB));
            }
        }

        private void DrawBurst(Burst burst, float age)
        {
            var t = age / burst.Duration;
            var fade = (1f - t) * (1f - t);
            var point = burst.Position;
            var normal = burst.Direction;
            var tangent = new Vector2(-normal.y, normal.x);
            if (burst.Kind == BurstKind.Ceramic)
            {
                for (var i = 0; i < 10; i++)
                {
                    var random = Noise(burst.Seed, i);
                    var horizontal = Noise(burst.Seed + 13u, i) * 2f - 1f;
                    var origin = point + new Vector2(horizontal * burst.Size.x * .35f, (random - .5f) * burst.Size.y * .6f);
                    var position = origin + new Vector2(horizontal * 1.6f, .6f + random * 1.5f) * age + Vector2.down * (3.5f * age * age);
                    var axis = Rotate(Vector2.right, random * 360f + age * (horizontal < 0f ? -220f : 220f));
                    Shard(position, axis, (.06f + random * .05f) * (1f - t * .7f), Tint(Color.Lerp(Terracotta, Stone, random * .4f), fade));
                }
                return;
            }
            if (burst.Kind == BurstKind.Pickup || burst.Kind == BurstKind.Reform || burst.Kind == BurstKind.Embed)
            {
                var radius = Mathf.Lerp(.5f, .06f, Mathf.SmoothStep(0f, 1f, t));
                Ring(point, .16f + .4f * t, .026f * (1f - t), Tint(Gold, fade * .75f));
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * 45f + t * 65f;
                    var position = point + Rotate(Vector2.right, angle) * radius;
                    Diamond(position, Vector2.up, .06f * (1f - t * .65f), .035f * (1f - t), Tint(PaleGold, 1f - t));
                }
                return;
            }
            if (burst.Kind == BurstKind.Failure)
            {
                for (var i = 0; i < 7; i++)
                {
                    var direction = Rotate(Vector2.right, i * 360f / 7f + Noise(burst.Seed, i) * 25f);
                    var position = point + direction * (.1f + t * .5f) + Vector2.down * age * .25f;
                    Diamond(position, direction, .075f * (1f - t), .035f * (1f - t), Tint(i % 3 == 0 ? Gold : Blue, fade));
                }
                return;
            }
            if (burst.Kind == BurstKind.Checkpoint)
            {
                Ring(point, .2f + t * .7f, .025f * (1f - t), Tint(Gold, fade));
                for (var i = 0; i < 5; i++)
                    Diamond(point + new Vector2((i - 2) * .15f, .1f + t * .6f), Vector2.up, .06f * (1f - t), .025f, Tint(PaleGold, fade));
                return;
            }
            var flash = Mathf.Clamp01(1f - age / .16f) * burst.Strength;
            var accent = burst.Kind == BurstKind.Reject ? Stone : PaleGold;
            Diamond(point + normal * .025f, normal, .31f * flash, .055f * flash, Tint(accent, flash));
            Diamond(point + normal * .025f, tangent, .22f * flash, .045f * flash, Tint(accent, flash));
            if (burst.Kind == BurstKind.Launch) return;
            if (burst.Kind == BurstKind.Lyre)
            {
                Ring(point, .12f + t * .45f, .018f * (1f - t), Tint(Gold, fade));
                return;
            }
            if (burst.Kind == BurstKind.Drum) return;
            // Dust leaves the struck face along its normal; it never sprays through the wall.
            for (var i = 0; i < 6; i++)
            {
                var random = Noise(burst.Seed, i);
                var direction = Rotate(normal, (random - .5f) * 135f);
                var position = point + normal * .035f + direction * age * (1.2f + random) * burst.Strength + Vector2.down * age * age;
                var size = (.025f + random * .025f) * (1f - t);
                Diamond(position, direction, size * 1.5f, size, Tint(Stone, fade * .8f));
            }
        }

        private void Ring(Vector2 center, float radius, float width, Color color)
        {
            const int segments = 24;
            var previous = center + Vector2.right * radius;
            for (var i = 1; i <= segments; i++)
            {
                var point = center + Rotate(Vector2.right, i * 360f / segments) * radius;
                Line(previous, point, width, width, color, color);
                previous = point;
            }
        }

        private void Line(Vector2 a, Vector2 b, float widthA, float widthB, Color colorA, Color colorB)
        {
            var delta = b - a;
            if (delta.sqrMagnitude < .000001f) return;
            var side = new Vector2(-delta.y, delta.x).normalized * .5f;
            Quad(a - side * widthA, b - side * widthB, b + side * widthB, a + side * widthA, colorA, colorB, colorB, colorA);
        }

        private void Diamond(Vector2 center, Vector2 axis, float length, float width, Color color)
        {
            var side = new Vector2(-axis.y, axis.x);
            Quad(center + axis * length, center + side * width, center - axis * length, center - side * width, color, color, color, color);
        }

        private void Shard(Vector2 center, Vector2 axis, float size, Color color)
        {
            var side = new Vector2(-axis.y, axis.x);
            var start = vertices.Count;
            Vertex(center + axis * size, color); Vertex(center - axis * size * .6f + side * size * .6f, color);
            Vertex(center - axis * size * .3f - side * size * .45f, color);
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        }

        private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ca, Color cb, Color cc, Color cd)
        {
            var start = vertices.Count;
            Vertex(a, ca); Vertex(b, cb); Vertex(c, cc); Vertex(d, cd);
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }

        private void Vertex(Vector2 point, Color color)
        {
            vertices.Add(geometry.InverseTransformPoint(new Vector3(point.x, point.y, 0f)));
            colors.Add(color);
        }

        private static Color Tint(Color color, float alpha) { color.a = Mathf.Clamp01(alpha); return color; }
        private static Vector2 Rotate(Vector2 direction, float degrees)
        {
            var angle = degrees * Mathf.Deg2Rad;
            var c = Mathf.Cos(angle); var s = Mathf.Sin(angle);
            return new Vector2(direction.x * c - direction.y * s, direction.x * s + direction.y * c);
        }
        private static float Noise(uint seed, int index)
        {
            var value = seed * 747796405u + (uint)(index + 1) * 2891336453u;
            value = ((value >> (int)((value >> 28) + 4)) ^ value) * 277803737u;
            return ((value >> 22) ^ value) / (float)uint.MaxValue;
        }
    }
}
