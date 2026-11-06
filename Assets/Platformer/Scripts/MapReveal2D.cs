using System;
using System.Collections.Generic;
using UnityEngine;

namespace HOne.Platformer
{
    public sealed class MapReveal2D : MonoBehaviour
    {
        private const string FootstepChannel = "footsteps";
        private const int MaximumActiveLights = 16;
        private const float PixelsPerUnit = 12f;

        [SerializeField] private LevelBounds2D worldBounds;
        [SerializeField] private Transform player;
        [SerializeField] private DistanceFootstepEmitter2D footstepSource;
        [SerializeField] private Material fogMaterialTemplate;
        [SerializeField, Min(0.1f)] private float initialVisibilityRadius = 1.5f;
        [SerializeField, Min(0.1f)] private float movementRevealRadius = 2.3f;
        [SerializeField, Min(0.1f)] private float flashDuration = 0.9f;
        [SerializeField, Min(0.1f)] private float pointDuration = 0.5f;
        [SerializeField, Min(0.01f)] private float edgeFeather = 1.1f;

        private readonly Dictionary<string, List<RevealSample>> pendingByChannel =
            new Dictionary<string, List<RevealSample>>(StringComparer.Ordinal);
        private readonly List<LightPoint> activeLights = new List<LightPoint>();
        private readonly Vector4[] lightData = new Vector4[MaximumActiveLights];
        private Texture2D revealTexture;
        private Texture2D flashTexture;
        private Material fogMaterial;
        private byte[] revealPixels;
        private byte[] flashPixels;
        private Color32[] revealColors;
        private Color32[] flashColors;
        private Vector2 previousPlayerPosition;
        private int textureWidth;
        private int textureHeight;
        private bool flashActive;

        private struct RevealSample
        {
            public Vector2 Position;
            public float Radius;
        }

        private struct LightPoint
        {
            public Vector2 Position;
            public float Age;
        }

        public void Configure(LevelBounds2D bounds, Transform trackedPlayer,
            DistanceFootstepEmitter2D steps, Material materialTemplate)
        {
            worldBounds = bounds;
            player = trackedPlayer;
            footstepSource = steps;
            fogMaterialTemplate = materialTemplate;
        }

        private void Awake()
        {
            if (worldBounds == null || player == null || fogMaterialTemplate == null)
            {
                Debug.LogError("Map Reveal needs map bounds, a player, and a fog material.", this);
                enabled = false;
                return;
            }

            var size = worldBounds.Maximum - worldBounds.Minimum;
            if (size.x <= 0f || size.y <= 0f)
            {
                Debug.LogError("Map Reveal needs positive map dimensions.", this);
                enabled = false;
                return;
            }

            textureWidth = Mathf.Max(1, Mathf.CeilToInt(size.x * PixelsPerUnit));
            textureHeight = Mathf.Max(1, Mathf.CeilToInt(size.y * PixelsPerUnit));
            revealPixels = new byte[textureWidth * textureHeight];
            flashPixels = new byte[revealPixels.Length];
            revealColors = new Color32[revealPixels.Length];
            flashColors = new Color32[flashPixels.Length];
            revealTexture = CreateMaskTexture("Map Reveal Mask", revealPixels, revealColors);
            flashTexture = CreateMaskTexture("Map Reveal Flash", flashPixels, flashColors);
            previousPlayerPosition = player.position;
            StampReveal(previousPlayerPosition, initialVisibilityRadius, false);
            UploadRevealMask();

            fogMaterial = new Material(fogMaterialTemplate) { name = "Map Reveal Fog (Runtime)" };
            fogMaterial.SetTexture("_RevealMask", revealTexture);
            fogMaterial.SetTexture("_FlashMask", flashTexture);
            fogMaterial.SetVector("_MapMinimum", new Vector4(worldBounds.Minimum.x, worldBounds.Minimum.y, 0f, 0f));
            fogMaterial.SetVector("_MapSize", new Vector4(size.x, size.y, 0f, 0f));

            var overlay = new GameObject("Fog Overlay");
            overlay.transform.SetParent(transform, false);
            overlay.transform.position = (Vector3)((worldBounds.Minimum + worldBounds.Maximum) * 0.5f);
            overlay.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = overlay.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 5;
            renderer.sharedMaterial = fogMaterial;
            overlay.AddComponent<SolidShape2D>();
        }

        private Texture2D CreateMaskTexture(string name, byte[] pixels, Color32[] colors)
        {
            var texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = name
            };
            UploadMask(texture, pixels, colors);
            return texture;
        }

        private void OnEnable()
        {
            if (footstepSource != null)
                footstepSource.Step += OnFootstep;
        }

        private void OnDisable()
        {
            if (footstepSource != null)
                footstepSource.Step -= OnFootstep;
        }

        private void OnDestroy()
        {
            if (fogMaterial != null)
                Destroy(fogMaterial);
            if (revealTexture != null)
                Destroy(revealTexture);
            if (flashTexture != null)
                Destroy(flashTexture);
        }

        private void Update()
        {
            if (fogMaterial == null)
                return;

            var currentPlayerPosition = (Vector2)player.position;
            var movement = currentPlayerPosition - previousPlayerPosition;
            previousPlayerPosition = currentPlayerPosition;
            if (Mathf.Abs(movement.x) > 0.0001f && movement.magnitude < 8f)
                RecordPending(FootstepChannel, currentPlayerPosition, movementRevealRadius);

            FadeFlash(Time.deltaTime);
            for (var i = activeLights.Count - 1; i >= 0; i--)
            {
                var light = activeLights[i];
                light.Age += Time.deltaTime;
                if (light.Age >= pointDuration)
                    activeLights.RemoveAt(i);
                else
                    activeLights[i] = light;
            }

            for (var i = 0; i < activeLights.Count; i++)
            {
                var light = activeLights[i];
                lightData[i] = new Vector4(light.Position.x, light.Position.y,
                    1f - light.Age / pointDuration, 0f);
            }
            fogMaterial.SetInt("_LightCount", activeLights.Count);
            fogMaterial.SetVectorArray("_LightData", lightData);
        }

        public void RecordPending(string channel, Vector2 position, float radius)
        {
            if (string.IsNullOrEmpty(channel) || radius <= 0f)
                return;

            if (!pendingByChannel.TryGetValue(channel, out var samples))
            {
                samples = new List<RevealSample>();
                pendingByChannel.Add(channel, samples);
            }
            samples.Add(new RevealSample { Position = position, Radius = radius });
        }

        public void IlluminatePending(string channel, Vector2 eventPosition)
        {
            if (revealTexture == null)
                return;

            if (pendingByChannel.TryGetValue(channel, out var samples) && samples.Count > 0)
            {
                for (var i = 0; i < samples.Count; i++)
                    StampReveal(samples[i].Position, samples[i].Radius, true);
                samples.Clear();
                UploadRevealMask();
                UploadFlashMask();
                flashActive = true;
            }
            AddLightPoint(eventPosition);
        }

        public void RevealAt(Vector2 position, float radius)
        {
            if (revealTexture == null || radius <= 0f)
                return;

            StampReveal(position, radius, true);
            UploadRevealMask();
            UploadFlashMask();
            flashActive = true;
            AddLightPoint(position);
        }

        private void OnFootstep(Vector2 position)
        {
            RecordPending(FootstepChannel, player.position, movementRevealRadius);
            IlluminatePending(FootstepChannel, position);
        }

        private void AddLightPoint(Vector2 position)
        {
            if (activeLights.Count == MaximumActiveLights)
                activeLights.RemoveAt(0);
            activeLights.Add(new LightPoint { Position = position });
        }

        private void FadeFlash(float deltaTime)
        {
            if (!flashActive || deltaTime <= 0f)
                return;

            var fadeAmount = Mathf.Max(1, Mathf.CeilToInt(255f * deltaTime / flashDuration));
            flashActive = false;
            for (var i = 0; i < flashPixels.Length; i++)
            {
                flashPixels[i] = (byte)Mathf.Max(0, flashPixels[i] - fadeAmount);
                flashActive |= flashPixels[i] > 0;
            }
            UploadFlashMask();
        }

        private void StampReveal(Vector2 center, float radius, bool addFlash)
        {
            var minimum = worldBounds.Minimum;
            var size = worldBounds.Maximum - minimum;
            var minX = Mathf.Max(0, Mathf.FloorToInt((center.x - radius - edgeFeather - minimum.x) / size.x * textureWidth));
            var maxX = Mathf.Min(textureWidth - 1, Mathf.CeilToInt((center.x + radius + edgeFeather - minimum.x) / size.x * textureWidth));
            var minY = Mathf.Max(0, Mathf.FloorToInt((center.y - radius - edgeFeather - minimum.y) / size.y * textureHeight));
            var maxY = Mathf.Min(textureHeight - 1, Mathf.CeilToInt((center.y + radius + edgeFeather - minimum.y) / size.y * textureHeight));
            for (var y = minY; y <= maxY; y++)
            {
                var worldY = minimum.y + (y + 0.5f) / textureHeight * size.y;
                for (var x = minX; x <= maxX; x++)
                {
                    var worldX = minimum.x + (x + 0.5f) / textureWidth * size.x;
                    var distance = Vector2.Distance(center, new Vector2(worldX, worldY));
                    var edgePosition = Mathf.InverseLerp(radius - edgeFeather,
                        radius + edgeFeather, distance);
                    var coverage = Mathf.RoundToInt((1f - Mathf.SmoothStep(0f, 1f, edgePosition)) * 255f);
                    var index = y * textureWidth + x;
                    if (addFlash)
                        flashPixels[index] = (byte)Mathf.Max(flashPixels[index], coverage - revealPixels[index]);
                    revealPixels[index] = (byte)Mathf.Max(revealPixels[index], coverage);
                }
            }
        }

        private void UploadRevealMask()
        {
            UploadMask(revealTexture, revealPixels, revealColors);
        }

        private void UploadFlashMask()
        {
            UploadMask(flashTexture, flashPixels, flashColors);
        }

        private static void UploadMask(Texture2D texture, byte[] pixels, Color32[] colors)
        {
            for (var i = 0; i < pixels.Length; i++)
                colors[i] = new Color32(pixels[i], pixels[i], pixels[i], 255);
            texture.SetPixelData(colors, 0);
            texture.Apply(false, false);
        }
    }
}
