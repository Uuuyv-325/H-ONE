using UnityEngine;

namespace HOne.Game
{
    public sealed class Hazard2D : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerStateMachine>();
            if (player != null)
                player.Fail();
        }
    }
}
