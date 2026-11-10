using UnityEngine;

namespace HOne.Game
{
    // Immutable delivery references. Mutable gameplay progress stays in LevelSession.
    public sealed class DeliveredArtSet : ScriptableObject
    {
        public Sprite[] Idle, Walk, Launch, Star, Fragment;
        public Sprite PlateUp, PlateDown, Checkpoint, RockDark, RockLight;
        public Sprite Background, ForegroundGrass, Title;
        public Sprite[] PauseProgress;
        public Material PauseBlur;
        public Sprite Jump, Lyre, DrumIdle;
        public Sprite[] DrumHit;
        public Sprite TerrainMiddle, TerrainLeft, TerrainRight, TerrainSoil, TerrainBrush;
        public Material TerrainMaterial;
        public Rect[] TerrainAtlasRects;

        public static Sprite Frame(Sprite[] frames, float time, float fps)
        {
            if (frames == null || frames.Length == 0) return null;
            return frames[Mathf.FloorToInt(Mathf.Max(0f, time) * fps) % frames.Length];
        }
    }
}
