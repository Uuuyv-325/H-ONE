using UnityEngine;

namespace HOne.Game
{
    public static class FeedbackMenuStyle
    {
        public static void ApplyTextButton(UnityEngine.UI.Button button)
        {
            var image = button.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = Color.clear;
                var rim = image.GetComponent<UnityEngine.UI.Outline>();
                if (rim != null) rim.enabled = false;
            }
            var label = button.GetComponentInChildren<UnityEngine.UI.Text>();
            button.targetGraphic = label;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = colors.selectedColor = new Color(1f, 1f, .78f);
            colors.pressedColor = new Color(1f, .72f, .32f);
            colors.disabledColor = new Color(.7f, .7f, .7f, .6f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = .08f;
            button.colors = colors;
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        }

        public static void Apply(Transform root)
        {
            if (!Application.isPlaying) return;
            foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if (!(button.targetGraphic is UnityEngine.UI.Image image)) continue;
                image.color = Color.white;
                var colors = button.colors;
                colors.normalColor = new Color(.08f, .14f, .22f, .96f);
                colors.highlightedColor = colors.selectedColor = new Color(.22f, .32f, .39f);
                colors.pressedColor = new Color(.39f, .31f, .16f);
                colors.disabledColor = new Color(.09f, .12f, .16f, .55f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = .08f;
                button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
                button.colors = colors;
                var rim = image.GetComponent<UnityEngine.UI.Outline>();
                if (rim == null) rim = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
                rim.effectColor = new Color(.91f, .75f, .42f, .55f);
                rim.effectDistance = new Vector2(1.5f, -1.5f);
                rim.useGraphicAlpha = true;
            }
        }
    }
}
