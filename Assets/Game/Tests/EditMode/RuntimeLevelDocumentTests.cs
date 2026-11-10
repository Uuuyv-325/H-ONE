using System.IO;
using HOne.Game;
using NUnit.Framework;
using UnityEngine;

namespace HOne.Game.Tests
{
    public sealed class RuntimeLevelDocumentTests
    {
        [Test]
        public void JsonRoundTripPreservesPiecePlacementAndMechanismLinks()
        {
            var source = new RuntimeLevelDocument
            {
                levelId = "m2-sample",
                entryCheckpointId = "dais-entry",
                groups = { new RuntimeLevelGroupRecord { id = "puzzle-a", name = "Puzzle A" } },
                pieces =
                {
                    new RuntimeLevelPieceRecord
                    {
                        id = "bridge-1",
                        name = "Bridge West",
                        kind = RuntimeLevelPieceKind.Bridge,
                        position = new Vector2(3.5f, -0.25f),
                        rotation = 90f,
                        size = new Vector2(4f, 0.2f),
                        groupId = "puzzle-a",
                        sourcePieceId = "plate-1",
                        barrierSolidWhenTrue = true
                    }
                }
            };

            var json = RuntimeLevelDocumentStore.Serialize(source);
            var restored = RuntimeLevelDocumentStore.Deserialize(json);

            Assert.AreEqual(RuntimeLevelDocument.CurrentVersion, restored.version);
            Assert.AreEqual(source.levelId, restored.levelId);
            Assert.AreEqual(source.entryCheckpointId, restored.entryCheckpointId);
            Assert.AreEqual("puzzle-a", restored.groups[0].id);
            Assert.AreEqual(1, restored.pieces.Count);
            Assert.AreEqual(RuntimeLevelPieceKind.Bridge, restored.pieces[0].kind);
            Assert.AreEqual(new Vector2(3.5f, -0.25f), restored.pieces[0].position);
            Assert.AreEqual(90f, restored.pieces[0].rotation);
            Assert.AreEqual(new Vector2(4f, 0.2f), restored.pieces[0].size);
            Assert.AreEqual("plate-1", restored.pieces[0].sourcePieceId);
            Assert.IsTrue(restored.pieces[0].barrierSolidWhenTrue);
        }

        [Test]
        public void DeserializeRejectsUnknownDocumentVersion()
        {
            Assert.Throws<InvalidDataException>(() => RuntimeLevelDocumentStore.Deserialize("{\"version\":99}"));
        }

        [Test]
        public void SafeNameRemovesPathSeparatorsAndWhitespace()
        {
            Assert.AreEqual("M2Sample", RuntimeLevelDocumentStore.SafeName("../M2 Sample"));
        }
    }
}
