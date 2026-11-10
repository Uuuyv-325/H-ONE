using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HOne.Game
{
    // Presentation only: all progress, occupancy and recall timing remain owned by gameplay.
    public sealed class GameplayFeedbackView : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1f, 0.83f, 0.42f, 0.95f);
        private static readonly Color Pale = new Color(0.76f, 0.84f, 0.93f, 0.8f);
        private PlayerStateMachine player;
        private LevelSession session;
        private ShadowService shadows;
        private RuntimeLevelEditor levelEditor;
        private ChapterTutorial tutorial;
        private Canvas canvas;
        private ProceduralHudGraphic fragmentHud, chargeHud, shadowHud;
        private UnityEngine.UI.Text chargeKey, recallKey;
        private GameplayFeedbackMesh world;
        private StarSocket[] sockets = Array.Empty<StarSocket>();
        private readonly List<string> fragmentIds = new List<string>();
        private readonly List<bool> collected = new List<bool>();
        private float nextRefresh, collectedAt, fullAt;
        private string latestFragment;
        private bool bound, fullShown;
        public int DisplayedFragmentSlots => fragmentIds.Count;
        public int DisplayedCollectedFragments { get; private set; }
        public FeedbackShadowState DisplayedShadowState { get; private set; }

        public void Configure(PlayerStateMachine owner)
        {
            Unbind();
            player = owner;
            session = player != null ? player.Session : null;
            shadows = session != null ? session.Shadows : null;
            if (player == null || session == null) return;
            levelEditor = session.GetComponentInChildren<RuntimeLevelEditor>(true);
            tutorial = session.GetComponentInChildren<ChapterTutorial>(true);
            EnsureVisuals();
            if (isActiveAndEnabled) Bind();
            ResetFeedback();
        }

        private void OnEnable()
        {
            if (player == null || session == null) return;
            EnsureVisuals();
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
            if (canvas != null) canvas.enabled = false;
            world?.SetVisible(false);
        }

        private void OnDestroy()
        {
            world?.Dispose();
            if (canvas != null) Destroy(canvas.gameObject);
        }

        private void Bind()
        {
            if (bound || session == null) return;
            session.FragmentCollected += FragmentCollected;
            session.ProgressReset += ResetFeedback;
            session.PuzzleReset += ResetFeedback;
            bound = true;
        }

        private void Unbind()
        {
            if (!bound || session == null) return;
            session.FragmentCollected -= FragmentCollected;
            session.ProgressReset -= ResetFeedback;
            session.PuzzleReset -= ResetFeedback;
            bound = false;
        }

        private void ResetFeedback()
        {
            latestFragment = null;
            fullShown = false;
            nextRefresh = 0f;
            if (world != null) { world.Begin(); world.Upload(); }
            RefreshContent();
        }

        private void FragmentCollected(string id)
        {
            latestFragment = id;
            collectedAt = Time.time;
        }

        private void EnsureVisuals()
        {
            if (world == null)
            {
                var preview = session.GetComponentInChildren<TrajectoryPreview>(true);
                var material = preview != null ? preview.GetComponent<LineRenderer>().sharedMaterial : null;
                world = new GameplayFeedbackMesh(transform, "ChargeAndShadowShapes", material, 22);
            }
            if (canvas != null) return;
            var go = new GameObject("GameplayFeedbackHUD", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Below the existing controls, development toolbar and pause/menu artwork.
            canvas.sortingOrder = 25;
            var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            fragmentHud = Panel("Fragments", FeedbackHudKind.Fragments, new Vector2(0f, 1f),
                new Vector2(32f, -108f), new Vector2(150f, 72f));
            chargeHud = Panel("Charge", FeedbackHudKind.Charge, Vector2.zero,
                new Vector2(32f, 28f), new Vector2(124f, 78f));
            shadowHud = Panel("Shadow", FeedbackHudKind.Shadow, Vector2.right,
                new Vector2(-32f, 28f), new Vector2(124f, 78f));
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (var label in session.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (label.font != null) { font = label.font; break; }
            chargeKey = KeyLabel(chargeHud.transform, font);
            recallKey = KeyLabel(shadowHud.transform, font);
        }

        private ProceduralHudGraphic Panel(string label, FeedbackHudKind kind, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(ProceduralHudGraphic));
            go.transform.SetParent(canvas.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var graphic = go.GetComponent<ProceduralHudGraphic>();
            graphic.Configure(kind);
            return graphic;
        }

        private static UnityEngine.UI.Text KeyLabel(Transform parent, Font font)
        {
            var go = new GameObject("Binding", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(91f, 0f);
            rect.sizeDelta = new Vector2(27f, 29f);
            var text = go.GetComponent<UnityEngine.UI.Text>();
            text.font = font;
            text.fontSize = 20;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 11;
            text.resizeTextMaxSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.88f, 0.91f, 0.94f);
            text.raycastTarget = false;
            return text;
        }

        private void RefreshContent()
        {
            if (session == null || fragmentHud == null) return;
            fragmentIds.Clear();
            foreach (var id in session.AllowedFragmentIds)
                if (!string.IsNullOrWhiteSpace(id)) fragmentIds.Add(id);
            fragmentIds.Sort(StringComparer.Ordinal);
            // Authored chapters use three nodes; wrap very large user-made levels across a compact panel.
            fragmentHud.rectTransform.sizeDelta = new Vector2(Mathf.Max(64f, Mathf.Min(8, fragmentIds.Count) * 43f + 21f),
                72f + Mathf.Max(0, (fragmentIds.Count - 1) / 8) * 43f);
            sockets = shadows != null ? shadows.GetComponentsInChildren<StarSocket>(true) : Array.Empty<StarSocket>();
            var actions = player.Input != null ? player.Input.Actions : null;
            chargeKey.text = Binding(actions, "Gameplay/Charge");
            recallKey.text = Binding(actions, "Gameplay/Recall");
            nextRefresh = Time.time + 0.5f;
        }

        private static string Binding(InputActionAsset actions, string actionName)
        {
            var action = actions != null ? actions.FindAction(actionName, false) : null;
            return action != null ? action.GetBindingDisplayString() : string.Empty;
        }

        private void LateUpdate()
        {
            if (player == null || session == null || canvas == null) return;
            var editing = levelEditor != null && levelEditor.IsEditing;
            canvas.enabled = !player.Paused && !editing;
            world.SetVisible(!editing);
            if (player.Paused) return;
            if (Time.time >= nextRefresh) RefreshContent();
            collected.Clear();
            DisplayedCollectedFragments = 0;
            for (var i = 0; i < fragmentIds.Count; i++)
            {
                var hasFragment = session.HasFragment(fragmentIds[i]);
                collected.Add(hasFragment);
                if (hasFragment) DisplayedCollectedFragments++;
            }
            fragmentHud.gameObject.SetActive(fragmentIds.Count > 0 && (tutorial == null || tutorial.CollectionVisible));
            chargeHud.gameObject.SetActive(tutorial == null || tutorial.ChargeVisible);
            shadowHud.gameObject.SetActive(tutorial == null || tutorial.ShadowVisible);
            recallKey.enabled = tutorial == null || tutorial.RecallVisible;
            fragmentHud.SetFragments(collected, fragmentIds.IndexOf(latestFragment),
                Mathf.Clamp01(1f - (Time.time - collectedAt) / 0.55f));

            var charging = player.State == PlayerState.Charging;
            if (!charging) fullShown = false;
            if (charging && player.Charge >= 0.999f && !fullShown)
            { fullShown = true; fullAt = Time.time; }
            var fullFlash = charging && fullShown ? Mathf.Clamp01(1f - (Time.time - fullAt) / 0.3f) : 0f;
            chargeHud.SetCharge(charging, charging ? player.Charge : 0f, fullFlash);

            var hasSockets = false;
            foreach (var socket in sockets)
                if (socket != null && socket.isActiveAndEnabled && socket.Shadows == shadows) { hasSockets = true; break; }
            var shadowState = shadows == null || !hasSockets ? FeedbackShadowState.Unavailable :
                shadows.Recalling ? FeedbackShadowState.Recalling : shadows.HasShadow ? FeedbackShadowState.Placed : FeedbackShadowState.Available;
            DisplayedShadowState = shadowState;
            shadowHud.SetShadow(shadowState, shadows != null ? shadows.RecallProgress : 0f);
            recallKey.color = shadowState == FeedbackShadowState.Unavailable || shadowState == FeedbackShadowState.Available
                ? new Color(0.65f, 0.71f, 0.78f, 0.65f) : new Color(0.96f, 0.9f, 0.7f);

            world.Begin();
            if (!editing)
            {
                if (charging) DrawCharge(fullFlash);
                DrawSockets();
            }
            world.Upload();
        }

        private void DrawCharge(float flash)
        {
            var center = player.Position;
            const float radius = 0.66f;
            world.Arc(center, radius, -90f, 360f, 0.014f, new Color(Pale.r, Pale.g, Pale.b, 0.35f));
            world.Arc(center, radius, -90f, -360f * player.Charge, 0.034f, Gold);
            var direction = player.AimDirection;
            world.Arrow(center + direction * (radius + 0.2f), direction, 0.13f, 0.03f, Gold);
            if (flash > 0f)
            {
                world.Arc(center, radius + (1f - flash) * 0.25f, 0f, 360f, 0.027f,
                    new Color(Gold.r, Gold.g, Gold.b, flash * 0.7f));
                world.Star(center + Vector2.up * (radius + 0.11f), 0.12f, Gold);
            }
        }

        private void DrawSockets()
        {
            foreach (var socket in sockets)
            {
                if (socket == null || !socket.isActiveAndEnabled || socket.Shadows != shadows) continue;
                var center = (Vector2)socket.transform.position;
                var occupied = shadows.ActiveSocket == socket;
                if (occupied)
                {
                    world.Diamond(center, 0.44f, 0.022f, Gold);
                    // Leave a full-width indication until the support really disappears at recall completion.
                    var support = socket.Shadow != null ? socket.Shadow.GetComponent<Collider2D>() : null;
                    if (support != null && support.enabled && support.gameObject.activeInHierarchy)
                    {
                        var bounds = support.bounds;
                        world.Line(new Vector2(bounds.min.x, bounds.max.y + 0.012f),
                            new Vector2(bounds.max.x, bounds.max.y + 0.012f), 0.023f, Gold);
                    }
                    if (shadows.Recalling)
                    {
                        world.Arc(center, 0.51f, 90f, -360f * shadows.RecallProgress, 0.035f, Gold);
                        DrawReturnStream(center, player.Position + Vector2.up * 0.15f);
                    }
                }
                else if (socket.Available)
                {
                    world.Arc(center, 0.43f, 35f, 290f, 0.019f, Pale);
                    world.Arrow(center + Vector2.up * 0.51f, Vector2.down, 0.13f, 0.026f, Gold);
                }
                else
                {
                    // The padlock is deliberately geometric, so the quota is readable with muted sound or no color.
                    world.Lock(center + Vector2.up * 0.49f, 0.12f, 0.025f, Pale);
                }
            }
        }

        private void DrawReturnStream(Vector2 start, Vector2 end)
        {
            var control = (start + end) * 0.5f + Vector2.up * Mathf.Min(1.6f, Vector2.Distance(start, end) * 0.22f);
            for (var i = 0; i < 6; i++)
            {
                var t = Mathf.Repeat(shadows.RecallProgress * 1.8f + i / 6f, 1f);
                var point = (1f - t) * (1f - t) * start + 2f * (1f - t) * t * control + t * t * end;
                world.Star(point, Mathf.Lerp(0.035f, 0.07f, t),
                    new Color(Gold.r, Gold.g, Gold.b, 0.35f + 0.55f * Mathf.Sin(t * Mathf.PI)));
            }
        }
    }

    // One dynamic mesh per view, using the existing sprite material. No external texture or asset is generated.
    internal sealed class GameplayFeedbackMesh : IDisposable
    {
        private readonly Mesh mesh;
        private readonly MeshRenderer renderer;
        private readonly Transform transform;
        private readonly Material ownedMaterial;
        private readonly List<Vector3> vertices = new List<Vector3>(1024);
        private readonly List<Color> colors = new List<Color>(1024);
        private readonly List<Vector2> uv = new List<Vector2>(1024);
        private readonly List<int> triangles = new List<int>(1536);

        public GameplayFeedbackMesh(Transform parent, string name, Material material, int sortingOrder)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            transform = go.transform;
            transform.SetParent(parent, false);
            mesh = new Mesh { name = name + "Mesh" };
            mesh.MarkDynamic();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            renderer = go.GetComponent<MeshRenderer>();
            if (material == null)
            {
                ownedMaterial = new Material(Shader.Find("Sprites/Default")) { name = name + "Material" };
                material = ownedMaterial;
            }
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        public void SetVisible(bool value) { if (renderer != null) renderer.enabled = value; }
        public void Begin() { vertices.Clear(); colors.Clear(); uv.Clear(); triangles.Clear(); }
        public void Upload()
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private void Vertex(Vector2 point, Color tint)
        {
            vertices.Add(transform.InverseTransformPoint(new Vector3(point.x, point.y, -0.55f)));
            colors.Add(tint);
            uv.Add(new Vector2(0.5f, 0.5f));
        }

        public void Line(Vector2 a, Vector2 b, float width, Color tint)
        {
            var direction = b - a;
            if (direction.sqrMagnitude < 0.0000001f) return;
            var normal = new Vector2(-direction.y, direction.x).normalized * (width * 0.5f);
            var index = vertices.Count;
            Vertex(a + normal, tint); Vertex(b + normal, tint); Vertex(b - normal, tint); Vertex(a - normal, tint);
            triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
            triangles.Add(index); triangles.Add(index + 2); triangles.Add(index + 3);
        }

        public void Arc(Vector2 center, float radius, float start, float sweep, float width, Color tint)
        {
            var steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(sweep) / 9f));
            for (var i = 0; i < steps; i++)
            {
                var a = (start + sweep * i / steps) * Mathf.Deg2Rad;
                var b = (start + sweep * (i + 1) / steps) * Mathf.Deg2Rad;
                Line(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width, tint);
            }
        }

        public void Diamond(Vector2 center, float radius, float width, Color tint)
        {
            Line(center + Vector2.up * radius, center + Vector2.right * radius, width, tint);
            Line(center + Vector2.right * radius, center + Vector2.down * radius, width, tint);
            Line(center + Vector2.down * radius, center + Vector2.left * radius, width, tint);
            Line(center + Vector2.left * radius, center + Vector2.up * radius, width, tint);
        }

        public void Star(Vector2 center, float radius, Color tint)
        {
            for (var i = 0; i < 8; i++)
            {
                var angleA = i * Mathf.PI * 0.25f;
                var angleB = (i + 1) * Mathf.PI * 0.25f;
                var index = vertices.Count;
                Vertex(center, tint);
                Vertex(center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * radius * (i % 2 == 0 ? 0.28f : 1f), tint);
                Vertex(center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * radius * (i % 2 == 0 ? 1f : 0.28f), tint);
                triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
            }
        }

        public void Arrow(Vector2 tip, Vector2 direction, float size, float width, Color tint)
        {
            var normal = new Vector2(-direction.y, direction.x);
            Line(tip - direction * size + normal * size * 0.65f, tip, width, tint);
            Line(tip - direction * size - normal * size * 0.65f, tip, width, tint);
        }

        public void Lock(Vector2 center, float radius, float width, Color tint)
        {
            var half = radius * 0.85f;
            Line(center + new Vector2(-half, -radius), center + new Vector2(-half, 0f), width, tint);
            Line(center + new Vector2(-half, 0f), center + new Vector2(half, 0f), width, tint);
            Line(center + new Vector2(half, 0f), center + new Vector2(half, -radius), width, tint);
            Line(center + new Vector2(half, -radius), center + new Vector2(-half, -radius), width, tint);
            Arc(center, half * 0.7f, 0f, 180f, width, tint);
            Line(center + new Vector2(0f, -radius * 0.32f), center + new Vector2(0f, -radius * 0.7f), width, tint);
        }

        public void Dispose()
        {
            if (mesh != null) UnityEngine.Object.Destroy(mesh);
            if (ownedMaterial != null) UnityEngine.Object.Destroy(ownedMaterial);
            if (transform != null) UnityEngine.Object.Destroy(transform.gameObject);
        }
    }
}
