using UnityEngine;

namespace HOne.Game
{
    public static class GameFlow
    {
        public const string GameTitle = "希腊星";
        public const string MainMenuScene = "Assets/Game/Scenes/MainMenu.unity";
        public const string GalleryScene = "Assets/Game/Scenes/M2_MechanismGallery.unity";
        public const string FirstLevelScene = "Assets/Game/Scenes/Chapter01_01.unity";
        public const string SecondLevelScene = "Assets/Game/Scenes/Chapter01_02.unity";

        public static void StartChapter() => LoadScene(FirstLevelScene);
        public static void StartChapter(UnityEngine.UI.Button button) => SceneTransition.Load(FirstLevelScene, button);
        public static void OpenGallery() => LoadScene(GalleryScene);
        public static void LoadScene(string path) => SceneTransition.Load(path);
        public static void ReturnToMenu(PlayerStateMachine player)
        {
            if (player != null) player.SetPaused(false);
            LoadScene(MainMenuScene);
        }
    }
}
