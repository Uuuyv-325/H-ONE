using System;
using UnityEngine;

namespace HOne.Game
{
    [CreateAssetMenu(menuName = "H-ONE/Runtime Level Piece Catalog")]
    public sealed class RuntimeLevelPieceCatalog : ScriptableObject
    {
        [SerializeField] private RuntimeLevelPieceCatalogEntry[] entries = Array.Empty<RuntimeLevelPieceCatalogEntry>();

        public GameObject Get(RuntimeLevelPieceKind kind)
        {
            foreach (var entry in entries)
                if (entry.kind == kind) return entry.prefab;
            return null;
        }
    }
}
