using System;
using UnityEngine;

namespace HOne.Game
{
    [Serializable]
    public struct ChapterRoom
    {
        public float StartX;
        public Vector2 Center;
        public float HalfHeight;
        public ChapterRoom(float start, Vector2 center, float halfHeight)
        { StartX = start; Center = center; HalfHeight = halfHeight; }
    }
    public sealed class ChapterCameraLayout : MonoBehaviour
    {
        [SerializeField] private ChapterRoom[] rooms = Array.Empty<ChapterRoom>();
        public void Configure(params ChapterRoom[] frames) => rooms = frames;
        public ChapterRoom RoomAt(float x)
        {
            var room = rooms[0];
            foreach (var candidate in rooms) if (x >= candidate.StartX) room = candidate;
            return room;
        }
    }
}
