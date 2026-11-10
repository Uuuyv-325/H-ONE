using UnityEngine;

namespace HOne.Game
{
    [CreateAssetMenu(menuName = "H-ONE/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public string LevelId = "ch01-core";
        public string[] FragmentIds = { "core-f1" };
        public string[] SwitchIds = new string[0];
        public string NextScene;
    }
}
