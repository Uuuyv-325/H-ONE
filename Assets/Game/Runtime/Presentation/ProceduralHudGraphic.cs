using System.Collections.Generic;
using UnityEngine;

namespace HOne.Game
{
    public enum FeedbackHudKind { Fragments, Charge, Shadow }
    public enum FeedbackShadowState { Unavailable, Available, Placed, Recalling }

    // Temporary, texture-free UI. The state comes from the session, never from an animation.
    public sealed class ProceduralHudGraphic : UnityEngine.UI.MaskableGraphic
    {
        private static readonly Color Ink = new Color(0.035f, 0.065f, 0.115f, 0.88f);
        private static readonly Color Rim = new Color(0.65f, 0.73f, 0.82f, 0.45f);
        private static readonly Color Gold = new Color(1f, 0.83f, 0.42f);
        private static readonly Color Empty = new Color(0.72f, 0.79f, 0.86f, 0.8f);
        private readonly List<bool> fragments = new List<bool>();
        private FeedbackHudKind kind;
        private FeedbackShadowState shadowState;
        private float progress, highlight;
        private int highlightedFragment = -1;
        private bool charging;

        public void Configure(FeedbackHudKind value)
        {
            kind = value;
            raycastTarget = false;
            SetVerticesDirty();
        }

        public void SetFragments(IReadOnlyList<bool> collected, int highlighted, float amount)
        {
            var changed = fragments.Count != collected.Count || highlightedFragment != highlighted ||
                !Mathf.Approximately(highlight, amount);
            if (fragments.Count != collected.Count)
            {
                fragments.Clear();
                for (var i = 0; i < collected.Count; i++) fragments.Add(collected[i]);
            }
            else for (var i = 0; i < collected.Count; i++)
            {
                changed |= fragments[i] != collected[i];
                fragments[i] = collected[i];
            }
            highlightedFragment = highlighted;
            highlight = amount;
            if (changed) SetVerticesDirty();
        }

        public void SetCharge(bool active, float amount, float fullFlash)
        {
            if (charging == active && Mathf.Approximately(progress, amount) &&
                Mathf.Approximately(highlight, fullFlash)) return;
            charging = active;
            progress = amount;
            highlight = fullFlash;
            SetVerticesDirty();
        }

        public void SetShadow(FeedbackShadowState state, float amount)
        {
            if (shadowState == state && Mathf.Approximately(progress, amount)) return;
            shadowState = state;
            progress = amount;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vertices)
        {
            vertices.Clear();
            var rect = GetPixelAdjustedRect();
            var origin = rect.min;
            var size = rect.size;
            var center = origin + size * 0.5f;
            Quad(vertices, origin, origin + Vector2.up * size.y, rect.max,
                origin + Vector2.right * size.x, Ink);
            // Short engraved corners keep the panel lighter than a solid rectangular border.
            var frame = new Color(Rim.r, Rim.g, Rim.b, 0.65f);
            Line(vertices, origin + new Vector2(1f, size.y - 1f), origin + new Vector2(25f, size.y - 1f), 2f, frame);
            Line(vertices, origin + new Vector2(1f, size.y - 1f), origin + new Vector2(1f, size.y - 15f), 2f, frame);
            Line(vertices, origin + new Vector2(size.x - 25f, 1f), origin + new Vector2(size.x - 1f, 1f), 2f, frame);
            Line(vertices, origin + new Vector2(size.x - 1f, 1f), origin + new Vector2(size.x - 1f, 15f), 2f, frame);

            if (kind == FeedbackHudKind.Fragments)
            {
                for (var i = 0; i < fragments.Count; i++)
                {
                    var point = new Vector2(origin.x + 32f + i % 8 * 43f, rect.yMax - 36f - i / 8 * 43f);
                    Diamond(vertices, point, 15f, 1.7f, fragments[i] ? Gold : Empty);
                    if (fragments[i]) Star(vertices, point, 11f, Gold);
                    else Line(vertices, point + Vector2.left * 3f, point + Vector2.right * 3f, 1.5f, Empty);
                    if (i == highlightedFragment && highlight > 0f)
                        Arc(vertices, point, 19f + (1f - highlight) * 10f, -90f, 360f, 2f,
                            new Color(Gold.r, Gold.g, Gold.b, highlight));
                }
                return;
            }

            var icon = new Vector2(origin.x + 39f, center.y);
            var key = new Vector2(origin.x + 91f, center.y);
            var keyColor = kind == FeedbackHudKind.Shadow && shadowState == FeedbackShadowState.Unavailable
                ? new Color(Rim.r, Rim.g, Rim.b, 0.3f) : Rim;
            Box(vertices, key, new Vector2(29f, 30f), 1.5f, keyColor);
            if (kind == FeedbackHudKind.Charge)
            {
                Arc(vertices, icon, 23f, -90f, 360f, 1.8f, Rim);
                if (charging)
                {
                    Arc(vertices, icon, 23f, -90f, -360f * Mathf.Clamp01(progress), 3f, Gold);
                    Star(vertices, icon, progress >= 0.999f ? 13f : 9f, Gold);
                    if (highlight > 0f)
                        Arc(vertices, icon, 26f + (1f - highlight) * 7f, -90f, 360f, 2f,
                            new Color(Gold.r, Gold.g, Gold.b, highlight));
                }
                else
                {
                    Star(vertices, icon, 8f, Empty);
                    Arrow(vertices, icon + Vector2.right * 9f, Vector2.right, 6f, Empty);
                }
                return;
            }

            if (shadowState == FeedbackShadowState.Unavailable)
            {
                Diamond(vertices, icon, 18f, 1.5f, Rim);
                Line(vertices, icon + new Vector2(-8f, -8f), icon + new Vector2(8f, 8f), 2f, Empty);
                Line(vertices, icon + new Vector2(-8f, 8f), icon + new Vector2(8f, -8f), 2f, Empty);
            }
            else if (shadowState == FeedbackShadowState.Available)
            {
                // The opening and inward arrow distinguish an unused slot in grayscale.
                Arc(vertices, icon, 21f, 35f, 290f, 2f, Empty);
                Arrow(vertices, icon + Vector2.up * 4f, Vector2.down, 7f, Gold);
                Diamond(vertices, icon + Vector2.down * 5f, 7f, 1.6f, Gold);
            }
            else
            {
                Diamond(vertices, icon, 20f, 2f, Gold);
                Star(vertices, icon, 12f, Gold);
                if (shadowState == FeedbackShadowState.Recalling)
                {
                    Arc(vertices, icon, 26f, -90f, -360f * Mathf.Clamp01(progress), 3f, Gold);
                    Arrow(vertices, icon + new Vector2(24f, 0f), Vector2.up, 5f, Gold);
                }
                else Line(vertices, icon + new Vector2(-16f, -24f), icon + new Vector2(16f, -24f), 2f, Gold);
            }
        }

        private static void Quad(UnityEngine.UI.VertexHelper vertices, Vector2 a, Vector2 b,
            Vector2 c, Vector2 d, Color tint)
        {
            var index = vertices.currentVertCount;
            vertices.AddVert(a, tint, Vector2.zero);
            vertices.AddVert(b, tint, Vector2.zero);
            vertices.AddVert(c, tint, Vector2.zero);
            vertices.AddVert(d, tint, Vector2.zero);
            vertices.AddTriangle(index, index + 1, index + 2);
            vertices.AddTriangle(index, index + 2, index + 3);
        }

        private static void Line(UnityEngine.UI.VertexHelper vertices, Vector2 a, Vector2 b, float width, Color tint)
        {
            var direction = b - a;
            if (direction.sqrMagnitude < 0.0001f) return;
            var normal = new Vector2(-direction.y, direction.x).normalized * (width * 0.5f);
            Quad(vertices, a + normal, b + normal, b - normal, a - normal, tint);
        }

        private static void Arc(UnityEngine.UI.VertexHelper vertices, Vector2 center, float radius,
            float start, float sweep, float width, Color tint)
        {
            var steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(sweep) / 9f));
            for (var i = 0; i < steps; i++)
            {
                var a = (start + sweep * i / steps) * Mathf.Deg2Rad;
                var b = (start + sweep * (i + 1) / steps) * Mathf.Deg2Rad;
                Line(vertices, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width, tint);
            }
        }

        private static void Box(UnityEngine.UI.VertexHelper vertices, Vector2 center, Vector2 size, float width, Color tint)
        {
            var half = size * 0.5f;
            var a = center + new Vector2(-half.x, -half.y);
            var b = center + new Vector2(-half.x, half.y);
            var c = center + new Vector2(half.x, half.y);
            var d = center + new Vector2(half.x, -half.y);
            Line(vertices, a, b, width, tint); Line(vertices, b, c, width, tint);
            Line(vertices, c, d, width, tint); Line(vertices, d, a, width, tint);
        }

        private static void Diamond(UnityEngine.UI.VertexHelper vertices, Vector2 center, float radius, float width, Color tint)
        {
            Line(vertices, center + Vector2.up * radius, center + Vector2.right * radius, width, tint);
            Line(vertices, center + Vector2.right * radius, center + Vector2.down * radius, width, tint);
            Line(vertices, center + Vector2.down * radius, center + Vector2.left * radius, width, tint);
            Line(vertices, center + Vector2.left * radius, center + Vector2.up * radius, width, tint);
        }

        private static void Star(UnityEngine.UI.VertexHelper vertices, Vector2 center, float radius, Color tint)
        {
            for (var i = 0; i < 8; i++)
            {
                var angleA = i * Mathf.PI * 0.25f;
                var angleB = (i + 1) * Mathf.PI * 0.25f;
                var a = center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * radius * (i % 2 == 0 ? 1f : 0.28f);
                var b = center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * radius * (i % 2 == 0 ? 0.28f : 1f);
                var index = vertices.currentVertCount;
                vertices.AddVert(center, tint, Vector2.zero);
                vertices.AddVert(b, tint, Vector2.zero);
                vertices.AddVert(a, tint, Vector2.zero);
                vertices.AddTriangle(index, index + 1, index + 2);
            }
        }

        private static void Arrow(UnityEngine.UI.VertexHelper vertices, Vector2 tip, Vector2 direction, float size, Color tint)
        {
            var normal = new Vector2(-direction.y, direction.x);
            Line(vertices, tip - direction * size + normal * size * 0.6f, tip, 2f, tint);
            Line(vertices, tip - direction * size - normal * size * 0.6f, tip, 2f, tint);
        }
    }
}
