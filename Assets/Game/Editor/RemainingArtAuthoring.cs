using System;
using System.Linq;
using HOne.Platformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HOne.Game.Editor
{
    public static class RemainingArtAuthoring
    {
        // Explicitly invoked migration; never runs automatically on import or replaces authored scenes.
        public static void ApplyScene(GameObject root)
        {
            var art = DeliveredArtAuthoring.Art;
            foreach (var box in root.GetComponentsInChildren<BoxCollider2D>(true))
            {
                if (box.isTrigger || box.GetComponent<SignalBarrier>() != null || box.GetComponent<PlayerStateMachine>() != null ||
                    box.GetComponent<StarInstrument>() != null || box.GetComponent<BrittleWall>() != null ||
                    box.GetComponentInParent<StarSocket>() != null || box.GetComponent<Rigidbody2D>() != null) continue;
                var visual = box.transform.Find("Visual");
                if (visual == null || visual.GetComponent<SpriteRenderer>() == null) continue;
                var terrain = box.GetComponent<GrassTerrainView>();
                if (terrain == null) terrain = box.gameObject.AddComponent<GrassTerrainView>();
                terrain.Configure(art);
            }
        }

        public static void AddGalleryStation(GameObject root)
        {
            if (root.transform.Find("Mechanisms/04_Instruments") != null) return;
            var session = root.GetComponent<LevelSession>();
            root.GetComponent<LevelBounds2D>().SetBounds(new Vector2(-12,-4),new Vector2(67,9));
            var geometry = root.transform.Find("Geometry");
            var wall = geometry.Find("RightWall");
            wall.position = new Vector3(67.25f,3,0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(wall);
            var floor = LevelPieceFactory.Place(LevelPiece.Solid,geometry,new Vector2(58,-.5f),session);
            floor.name = "InstrumentFloor"; M2SceneAuthoring.Resize(floor,new Vector2(18,1));
            var group = LevelPieceFactory.Child(root.transform.Find("Mechanisms"),"04_Instruments").gameObject.AddComponent<PuzzleGroup>();
            group.Configure("gallery-instruments");
            var dais = LevelPieceFactory.Place(LevelPiece.Checkpoint,root.transform.Find("Checkpoints"),new Vector2(50,0),session).GetComponent<Checkpoint>();
            dais.name = "InstrumentDais"; dais.ConfigurePuzzle(group);
            var lyre = LevelPieceFactory.Place(LevelPiece.Lyre,group.transform,new Vector2(54,1.2f),session).GetComponent<StarInstrument>();
            lyre.name = "Lyre";
            var gate = LevelPieceFactory.Place(LevelPiece.Gate,group.transform,new Vector2(57,1.25f),session).GetComponent<SignalBarrier>();
            gate.name = "LyreGate"; gate.Configure(lyre,false,gate.GetComponent<BoxCollider2D>(),gate.GetComponentInChildren<SpriteRenderer>());
            var brittle = LevelPieceFactory.Place(LevelPiece.BrittleWall,group.transform,new Vector2(64,1),session).GetComponent<BrittleWall>();
            brittle.name = "DrumWaveWall";
            var drum = LevelPieceFactory.Place(LevelPiece.Drum,group.transform,new Vector2(62,.9f),session).GetComponent<StarInstrument>();
            drum.name = "Drum"; drum.Configure(StarInstrumentKind.Drum,session,drum.StableId,brittle);
            session.ConfigureMechanisms(root.GetComponent<ShadowService>(),root.GetComponentsInChildren<PuzzleGroup>(true));
            group.Configure(group.StableId,gate);
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                EditorUtility.SetDirty(component);
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }

        // A circular brush selects grid cells; it never creates a hidden circular collision surface.
        public static void PaintGrassDisk(Transform parent, Vector2 centre, float radius, LevelSession session)
        {
            const float cell = 1f;
            var min = Vector2Int.FloorToInt(centre-Vector2.one*radius);
            var max = Vector2Int.CeilToInt(centre+Vector2.one*radius);
            Physics2D.SyncTransforms();
            var existing = parent.GetComponentsInChildren<GrassTerrainView>().Select(t=>t.GetComponent<BoxCollider2D>()).ToArray();
            for (var y=min.y;y<max.y;y++)
            {
                var xs = Enumerable.Range(min.x,Mathf.Max(0,max.x-min.x)).Where(x =>
                    Vector2.Distance(new Vector2(x+.5f,y+.5f),centre)<=radius && !existing.Any(b=>
                        b.bounds.max.x>x+.0001f && b.bounds.min.x<x+1f-.0001f &&
                        b.bounds.max.y>y+.0001f && b.bounds.min.y<y+1f-.0001f)).ToArray();
                if (xs.Length==0) continue;
                for (var first=0;first<xs.Length;)
                {
                    var last=first;
                    while(last+1<xs.Length && xs[last+1]==xs[last]+1) last++;
                    var size = new Vector2((last-first+1)*cell,cell);
                    var position = new Vector2(xs[first]+size.x*.5f,y+.5f);
                    var go = LevelPieceFactory.Place(LevelPiece.Solid,parent,position,session);
                    go.name="GrassBrushRow"; M2SceneAuthoring.Resize(go,size);
                    first=last+1;
                }
            }
        }
    }
}
