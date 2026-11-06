using UnityEngine;

namespace HOne.Platformer
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SolidShape2D : MonoBehaviour
    {
        private static Sprite unitSprite;

        private void OnEnable()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite == null)
                renderer.sprite = GetUnitSprite();
        }

        private static Sprite GetUnitSprite()
        {
            if (unitSprite == null)
            {
                unitSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                unitSprite.name = "Runtime Unit Square";
                unitSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            return unitSprite;
        }
    }
}
