using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HOne.Game
{
    public enum RuntimeLevelPieceKind
    {
        Solid, Hazard, Checkpoint, Fragment, PlatformSocket, PlateSocket,
        PressurePlate, Bridge, Gate, Stone, Lyre, Drum, BrittleWall
    }

    [Serializable]
    public sealed class RuntimeLevelGroupRecord
    {
        public string id;
        public string name;
    }

    [Serializable]
    public sealed class RuntimeLevelPieceRecord
    {
        public string id;
        public string name;
        public RuntimeLevelPieceKind kind;
        public Vector2 position;
        public float rotation;
        public Vector2 size;
        public string groupId;
        public string fragmentId;
        public string checkpointId;
        public string socketId;
        public string weightId;
        public string shadowWeightId;
        public string sourcePieceId;
        public string[] waveTargetIds;
        public bool barrierSolidWhenTrue = true;
    }

    [Serializable]
    public sealed class RuntimeLevelDocument
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public string levelId = "untitled";
        public string entryCheckpointId;
        public List<RuntimeLevelGroupRecord> groups = new List<RuntimeLevelGroupRecord>();
        public List<RuntimeLevelPieceRecord> pieces = new List<RuntimeLevelPieceRecord>();
    }

    public static class RuntimeLevelDocumentStore
    {
        public static string DirectoryPath => Path.Combine(Application.persistentDataPath, "Levels");

        public static string SafeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "untitled";
            var safe = string.Empty;
            foreach (var character in name.Trim())
                if (char.IsLetterOrDigit(character) || character == '-' || character == '_') safe += character;
            return string.IsNullOrEmpty(safe) ? "untitled" : safe;
        }

        public static string Serialize(RuntimeLevelDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            document.version = RuntimeLevelDocument.CurrentVersion;
            return JsonUtility.ToJson(document, true);
        }

        public static RuntimeLevelDocument Deserialize(string json)
        {
            var document = JsonUtility.FromJson<RuntimeLevelDocument>(json);
            if (document == null || document.version != RuntimeLevelDocument.CurrentVersion)
                throw new InvalidDataException("Unsupported or invalid level document.");
            document.groups ??= new List<RuntimeLevelGroupRecord>();
            document.pieces ??= new List<RuntimeLevelPieceRecord>();
            return document;
        }

        public static string Save(RuntimeLevelDocument document, string name)
        {
            Directory.CreateDirectory(DirectoryPath);
            var path = Path.Combine(DirectoryPath, SafeName(name) + ".json");
            File.WriteAllText(path, Serialize(document));
            return path;
        }

        public static RuntimeLevelDocument Load(string name)
        {
            var path = Path.Combine(DirectoryPath, SafeName(name) + ".json");
            if (!File.Exists(path)) throw new FileNotFoundException("Level file not found.", path);
            return Deserialize(File.ReadAllText(path));
        }

        public static string[] List()
        {
            if (!Directory.Exists(DirectoryPath)) return Array.Empty<string>();
            var paths = Directory.GetFiles(DirectoryPath, "*.json");
            var names = new string[paths.Length];
            for (var i = 0; i < paths.Length; i++) names[i] = Path.GetFileNameWithoutExtension(paths[i]);
            Array.Sort(names, StringComparer.OrdinalIgnoreCase);
            return names;
        }
    }

    [Serializable]
    public sealed class RuntimeLevelPieceCatalogEntry
    {
        public RuntimeLevelPieceKind kind;
        public GameObject prefab;
    }

}
