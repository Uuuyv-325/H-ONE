using UnityEngine;

namespace HOne.Game
{
    // One shared delivery also reaches existing scenes and runtime-authored levels.
    // Missing clips retain their technical fallback; audio never owns gameplay timing.
    [CreateAssetMenu(menuName = "H-ONE/Chapter Audio Set")]
    public sealed class ChapterAudioSet : ScriptableObject
    {
        public const string ResourcePath = "ChapterAudio";
        public AudioClip Stone, Lyre, Drum, Ceramic, Launch, Embed;

        public static ChapterAudioSet LoadDefault() => Resources.Load<ChapterAudioSet>(ResourcePath);
    }
}
