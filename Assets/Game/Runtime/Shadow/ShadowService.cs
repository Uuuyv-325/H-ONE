using System;
using UnityEngine;

namespace HOne.Game
{
    // One authority and one scaled gameplay clock per level, independent of the player body.
    [DefaultExecutionOrder(-100)]
    public sealed class ShadowService : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float recallSeconds = 1f;
        public StarSocket ActiveSocket { get; private set; }
        public bool HasShadow => ActiveSocket != null;
        public bool Recalling { get; private set; }
        public float RecallProgress => Recalling ? 1f - remaining / recallSeconds : 0f;
        private float remaining;
        public event Action Changed;
        public event Action RecallStarted;
        public event Action Recalled;

        public bool TryPlace(StarSocket socket)
        {
            if (socket == null || !socket.isActiveAndEnabled || HasShadow) return false;
            ActiveSocket = socket;
            Recalling = false;
            socket.SetShadow(true);
            Changed?.Invoke();
            return true;
        }

        public bool BeginRecall()
        {
            if (!HasShadow || Recalling) return false;
            Recalling = true;
            remaining = recallSeconds;
            RecallStarted?.Invoke();
            return true;
        }

        private void FixedUpdate()
        {
            if (!Recalling) return;
            remaining -= Time.fixedDeltaTime;
            if (remaining > 0f) return;
            Clear();
            Recalled?.Invoke();
        }

        public void Clear()
        {
            var previous = ActiveSocket;
            var changed = HasShadow || Recalling;
            ActiveSocket = null;
            Recalling = false;
            remaining = 0f;
            if (previous != null) previous.SetShadow(false);
            if (changed) Changed?.Invoke();
        }

        public void Release(StarSocket socket) { if (ActiveSocket == socket) Clear(); }
        private void OnDisable() => Clear();
    }
}
