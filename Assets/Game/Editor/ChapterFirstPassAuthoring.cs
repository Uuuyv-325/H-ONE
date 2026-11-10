using System;
using System.IO;
using System.Linq;
using HOne.Platformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HOne.Game.Editor
{
    // Creates new authored scenes once. Further iteration happens on those scenes, not by regeneration.
    public static class ChapterFirstPassAuthoring
    {
        [MenuItem("H-ONE/Create Chapter 1 First Pass (new only)")]
        public static void Create()
        {
            if (Application.isPlaying || File.Exists(GameFlow.FirstLevelScene) || File.Exists(GameFlow.SecondLevelScene))
                throw new InvalidOperationException("Requires stopped Play Mode and two unused chapter scene paths.");
            var tuning = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PlayerTuning>("Assets/Game/Data/Player/CorePlayerTuning.asset"));
            tuning.FlightDistance = 8f;
            AssetDatabase.CreateAsset(tuning, "Assets/Game/Data/Player/ChapterPlayerTuning.asset");
            Build(GameFlow.FirstLevelScene, false, tuning);
            Build(GameFlow.SecondLevelScene, true, tuning);
            UpdateMenu();
            ConfigureBuild();
            AssetDatabase.SaveAssets();
        }
        private static void Build(string path, bool second, PlayerTuning tuning)
        {
            M2SceneAuthoring.CreateLevel(path, false);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var root = scene.GetRootGameObjects().Single(g => g.GetComponent<LevelSession>() != null);
                var session = root.GetComponent<LevelSession>();
                var player = root.GetComponentInChildren<PlayerStateMachine>();
                var camera = root.GetComponentInChildren<Camera>();
                var bounds = root.GetComponent<LevelBounds2D>();
                bounds.SetBounds(new Vector2(-3,-3), new Vector2(second ? 58 : 37, 12));
                player.Configure(tuning, session, bounds, camera);
                UnityEngine.Object.DestroyImmediate(root.GetComponent<RuntimeLevelEditor>());
                // Only the just-created scene's template geometry is removed.
                var geometry = root.transform.Find("Geometry");
                while (geometry.childCount > 0) UnityEngine.Object.DestroyImmediate(geometry.GetChild(0).gameObject);
                var entry = session.EntryCheckpoint;
                entry.transform.position = new Vector2(1,0);
                player.transform.position = entry.SpawnPosition;
                session.Definition.LevelId = second ? "chapter01-02" : "chapter01-01";
                session.Definition.FragmentIds = second ? new[] { "l2-f1", "l2-f2", "l2-f3" } : new[] { "l1-f1", "l1-f2", "l1-f3" };
                session.Definition.SwitchIds = second ? new[] { "l2-s1", "l2-s2" } : Array.Empty<string>();
                session.Definition.NextScene = second ? string.Empty : GameFlow.SecondLevelScene;
                EditorUtility.SetDirty(session.Definition);
                session.Configure(session.Definition, entry);
                var group = root.GetComponentInChildren<PuzzleGroup>(); group.Configure(second ? "l2-a" : "l1-a");
                group.name = "A";
                entry.ConfigurePuzzle(group);
                if (second) LevelTwo(root, session, player, group); else LevelOne(root, session, player, group);
                var groups = root.GetComponentsInChildren<PuzzleGroup>();
                foreach (var puzzle in groups)
                    puzzle.Configure(puzzle.StableId, puzzle.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c is IPuzzleResettable).ToArray());
                session.ConfigureMechanisms(root.GetComponent<ShadowService>(), groups);
                M2MenuAuthoring.UpgradePause(root.GetComponentInChildren<PuzzlePauseMenu>(true));
                root.GetComponentInChildren<PuzzlePauseMenu>(true).transform.Find("Panel/MenuControls/Restart level")
                    .GetComponentInChildren<UnityEngine.UI.Text>().text = "重新开始本关";
                var roomLayout = root.AddComponent<ChapterCameraLayout>();
                roomLayout.Configure(second ? new[] {
                    Room(-3,5,3.3f,5.2f), Room(10,17,3.3f,5.2f), Room(24,29,3.7f,5.2f),
                    Room(33,37,3.4f,5.4f), Room(42,49,5,6.3f)
                } : new[] { Room(-3,5,3.3f,5.2f), Room(10,16.3f,3.3f,5.4f), Room(21,27,5,6.3f) });
                camera.GetComponent<ChapterCamera2D>().ConfigureRooms(roomLayout);
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    EditorUtility.SetDirty(component);
                    if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
                EditorSceneManager.SaveScene(scene, path);
            }
            finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
        }
        private static ChapterRoom Room(float x, float cx, float cy, float size) => new ChapterRoom(x,new Vector2(cx,cy),size);
        private static void LevelOne(GameObject root, LevelSession s, PlayerStateMachine p, PuzzleGroup a)
        {
            var b = Group(root,"B_Reflection","l1-b"); var c = Group(root,"C_Shadow","l1-c");
            Solid(root,s,"TempleFloor",17,-.5f,40,1);
            Solid(root,s,"JumpStep",3,.35f,1.3f,.7f);
            Dais(root,s,a,4.5f,0,"LaunchDais");
            Fragment(root,s,"l1-f1",6,2.2f);
            Gate(root,s,a.transform,"FirstFragmentDoor",9.7f,6,.35f,12,new[]{"l1-f1"});
            Dais(root,s,b,12,0,"ReflectionDais");
            var lyre = Place(root,s,LevelPiece.Lyre,b.transform,"TeachingLyre",17,.7f);
            lyre.transform.rotation = Quaternion.Euler(0,0,-45);
            Fragment(root,s,"l1-f2",16.62f,4.6f);
            // Star-sized aperture. A human cannot stand behind the shield before solving the reflection.
            Gate(root,s,b.transform,"ReflectionShield",15,6.55f,.35f,10.9f,new[]{"l1-f2"});
            Solid(root,s,"ApertureSill",15,.1f,.4f,.2f);
            Dais(root,s,c,23,0,"ShadowDais");
            Place(root,s,LevelPiece.PlatformSocket,c.transform,"TeachingSocket",25,1.6f);
            Fragment(root,s,"l1-f3",25,9.5f);
            MakeExit(root,s,p,33,sessionNext:true);
            Tutorial(root,p,false,float.MaxValue,
                Lesson(ChapterLessonKind.Walk,-3,1.8f,null,null,new Vector2(1,.7f),new Vector2(2,.7f)),
                Lesson(ChapterLessonKind.Jump,1.8f,4,null,null,new Vector2(2.3f,.7f),new Vector2(3,2),new Vector2(4,.7f)),
                Lesson(ChapterLessonKind.Launch,4,10,"l1-f1",null,new Vector2(4.5f,.7f),new Vector2(6,2.2f)),
                Lesson(ChapterLessonKind.Reflect,10,21,"l1-f2",null,new Vector2(12.6f,.7f),new Vector2(16.62f,.7f),new Vector2(16.62f,4.6f)),
                Follow(Lesson(ChapterLessonKind.Shadow,21,37,"l1-f3",null,new Vector2(23,.7f),new Vector2(25,1.6f)),
                    new Vector2(23,.7f),new Vector2(25,2.6f),new Vector2(25,9.5f)));
        }
        private static void LevelTwo(GameObject root, LevelSession s, PlayerStateMachine p, PuzzleGroup a)
        {
            var b = Group(root,"B_ShadowBridge","l2-b"); var c = Group(root,"C_Drum","l2-c");
            var d = Group(root,"D_Lyre","l2-d"); var e = Group(root,"E_Flute","l2-e");
            Solid(root,s,"WestFloor",6,-.5f,20,1);
            Solid(root,s,"EastFloor",40,-.5f,36,1);
            var pit = Place(root,s,LevelPiece.Hazard,root.transform.Find("Geometry"),"StarMist",19,-2.3f);
            M2SceneAuthoring.Resize(pit,new Vector2(6,.4f));
            Place(root,s,LevelPiece.Stone,a.transform,"StarStone",3,.55f);
            var plate = Place(root,s,LevelPiece.PressurePlate,a.transform,"StonePlate",5.3f,0).GetComponent<PressurePlate>();
            Solid(root,s,"StoneStop",6.25f,.28f,.4f,.56f);
            Barrier(root,s,a.transform,"StoneGate",7.5f,4,.3f,8,plate,false);
            Circuit(a.transform,plate,true,new Vector2(5.3f,.1f),new Vector2(5.3f,-.3f),new Vector2(7.5f,-.3f),new Vector2(7.5f,1.4f));
            Fragment(root,s,"l2-f1",9,2.2f);
            Dais(root,s,a,9,0,"StoneSolvedDais");
            Dais(root,s,b,12.3f,0,"BridgeDais");
            var socket = Place(root,s,LevelPiece.PlateSocket,b.transform,"BridgeSocket",14,3);
            Barrier(root,s,b.transform,"StarBridge",19,-.1f,6,.2f,socket.GetComponentInChildren<PressurePlate>(),true);
            Circuit(b.transform,socket.GetComponentInChildren<PressurePlate>(),false,new Vector2(14,3),new Vector2(15.5f,3),new Vector2(15.5f,.1f),new Vector2(19,.1f));
            Fragment(root,s,"l2-f2",23.5f,2.2f);
            Dais(root,s,c,24,0,"DrumDais");
            var wall = Place(root,s,LevelPiece.BrittleWall,c.transform,"ResonantWall",31,1.6f).GetComponent<BrittleWall>();
            M2SceneAuthoring.Resize(wall.gameObject,new Vector2(.4f,3.2f));
            var drum = Place(root,s,LevelPiece.Drum,c.transform,"Tympanon",29,1.1f).GetComponent<StarInstrument>();
            drum.Configure(StarInstrumentKind.Drum,s,"l2-drum",wall);
            // The paired round seal opens with the drum strike; the wall still obeys ordinary ceramic rules.
            var drumSeal=Gate(root,s,c.transform,"DrumSeal",32,5,.3f,10,Array.Empty<string>(),new[]{"l2-drum"});
            Circuit(c.transform,drumSeal,true,new Vector2(29,1.1f),new Vector2(29,.3f),new Vector2(32,.3f),new Vector2(32,1.4f));
            Dais(root,s,d,34,0,"LyreDais");
            var lyre = Place(root,s,LevelPiece.Lyre,d.transform,"SwitchLyre",39,.7f);
            lyre.transform.rotation = Quaternion.Euler(0,0,-45);
            lyre.GetComponent<StarInstrument>().Configure(StarInstrumentKind.Lyre,s,"l2-lyre");
            var s1 = Switch(root,s,d.transform,"l2-s1",38.62f,4.6f,lyre.GetComponent<StarInstrument>());
            Gate(root,s,d.transform,"SwitchShield",37,6.55f,.35f,10.9f,Array.Empty<string>(),new[]{"l2-s1"});
            d.transform.Find("SwitchShield/ProgressNodes").GetComponent<ChapterGoalView>().SetMechanismShape(false);
            Circuit(d.transform,s1,false,new Vector2(38.62f,4.6f),new Vector2(40.5f,4.6f),new Vector2(40.5f,.3f),new Vector2(37,.3f));
            Solid(root,s,"SecondApertureSill",37,.1f,.4f,.2f);
            Dais(root,s,e,44,0,"FluteDais");
            var lift = new GameObject("FluteLift",typeof(Rigidbody2D),typeof(BoxCollider2D),typeof(PanFluteLift));
            lift.transform.SetParent(e.transform); lift.transform.position = new Vector2(48,.15f);
            lift.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            lift.GetComponent<BoxCollider2D>().size = new Vector2(3,.3f);
            LevelPieceFactory.View(lift.transform,"stone",new Vector2(3,.3f),new Color(.62f,.75f,.85f));
            var platform = lift.GetComponent<PanFluteLift>(); platform.Configure(new Vector2(48,.15f),new Vector2(48,1.85f));
            var up = Port(e.transform,"UpperPipe",45,1.6f,platform,true);
            var down = Port(e.transform,"LowerPipe",45,.55f,platform,false);
            var s2 = Switch(root,s,e.transform,"l2-s2",48,9.7f,null);
            Fragment(root,s,"l2-f3",48,10.3f);
            MakeExit(root,s,p,54,sessionNext:false);
            root.AddComponent<ChapterMechanismView>().Configure(p,
                AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>("Assets/Game/Audio/Mixers/Core.mixer").FindMatchingGroups("SFX")[0],new[]{s1,s2},new[]{up,down});
            Tutorial(root,p,true,23,
                Lesson(ChapterLessonKind.Push,-3,10,"l2-f1",null,new Vector2(2,.7f),new Vector2(5.3f,.7f)),
                Follow(Lesson(ChapterLessonKind.Bridge,10,24,"l2-f2",null,new Vector2(12.3f,.7f),new Vector2(14,3)),
                    new Vector2(12.3f,.7f),new Vector2(23,.7f),new Vector2(23.5f,2.2f)),
                Lesson(ChapterLessonKind.Drum,24,33,null,"l2-drum",new Vector2(26,.7f),new Vector2(29,1.1f)),
                Lesson(ChapterLessonKind.Reflect,33,42,null,"l2-s1",new Vector2(34.6f,.7f),new Vector2(38.62f,.7f),new Vector2(38.62f,4.6f)),
                Follow(Lesson(ChapterLessonKind.Flute,42,58,"l2-f3",null,new Vector2(44,.7f),new Vector2(45,1.6f)),
                    new Vector2(44,.7f),new Vector2(48,2.7f),new Vector2(48,9.7f),new Vector2(48,10.3f)));
        }
        private static void Tutorial(GameObject root,PlayerStateMachine p,bool familiar,float recallX,params ChapterLesson[] lessons)
        {
            var canvas = M2MenuAuthoring.Canvas(root.transform.Find("Presentation"),"ProgressiveGlyphs",30);
            var glyphs = M2MenuAuthoring.Label(canvas,"CurrentAction",string.Empty,new Vector2(.2f,.02f),new Vector2(.8f,.085f),28);
            root.transform.Find("Tutorial").gameObject.AddComponent<ChapterTutorial>().Configure(p,glyphs,familiar,recallX,lessons);
        }
        private static ChapterLesson Lesson(ChapterLessonKind kind,float start,float end,string fragment,string mechanism,params Vector2[] path)
            => new ChapterLesson { Kind=kind,StartX=start,EndX=end,CompletedFragment=fragment,CompletedMechanism=mechanism,Path=path };
        private static ChapterLesson Follow(ChapterLesson lesson,params Vector2[] points)
        { lesson.FollowupPath=points; return lesson; }
        private static void Circuit(Transform parent,BoolSignal signal,bool round,params Vector2[] path)
        { LevelPieceFactory.Child(parent,"Circuit").gameObject.AddComponent<ChapterCircuitView>().Configure(signal,round,path); }
        private static FlutePort Port(Transform parent,string name,float x,float y,PanFluteLift lift,bool up)
        {
            var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = new Vector2(x,y);
            LevelPieceFactory.Box(go,new Vector2(.65f,.65f),true);
            var port=go.AddComponent<FlutePort>(); port.Configure(lift,up); return port;
        }
        private static StarSwitch Switch(GameObject root,LevelSession s,Transform parent,string id,float x,float y,BoolSignal prerequisite)
        {
            var go = new GameObject(id); go.transform.SetParent(parent); go.transform.position = new Vector2(x,y);
            LevelPieceFactory.Box(go,new Vector2(.65f,.65f),true);
            var target = go.AddComponent<StarSwitch>(); target.Configure(s,id,prerequisite); return target;
        }
        private static void MakeExit(GameObject root,LevelSession s,PlayerStateMachine p,float x,bool sessionNext)
        {
            var condition = Gate(root,s,root.transform.Find("Mechanisms"),"ConstellationDoor",x,2,.4f,4,s.Definition.FragmentIds,s.Definition.SwitchIds);
            var exit = new GameObject("ChapterExit"); exit.transform.SetParent(root.transform.Find("Mechanisms")); exit.transform.position = new Vector2(x+1.1f,1);
            LevelPieceFactory.Box(exit,new Vector2(1.4f,2),true);
            GameObject panel = null;
            if (!sessionNext)
            {
                var canvas = M2MenuAuthoring.Canvas(root.transform.Find("Presentation"),"ChapterCompletion",2200);
                panel = canvas.gameObject;
                var ink = M2MenuAuthoring.Panel(canvas,"Panel",new Vector2(.2f,.2f),new Vector2(.8f,.8f),new Color(.035f,.065f,.12f,.97f));
                M2MenuAuthoring.Label(ink,"ChapterComplete","第一章完成",new Vector2(.1f,.6f),new Vector2(.9f,.9f),50);
                var replay = M2MenuAuthoring.Button(ink,"Replay","重新游玩",new Vector2(.15f,.32f),new Vector2(.85f,.47f));
                var menu = M2MenuAuthoring.Button(ink,"Title","回到标题",new Vector2(.15f,.12f),new Vector2(.85f,.27f));
                panel.SetActive(false);
                panel.AddComponent<ChapterCompletionView>().Configure(p,replay,menu);
            }
            exit.AddComponent<ChapterExit>().Configure(s,condition,panel);
        }
        private static PuzzleGroup Group(GameObject root,string name,string id)
        { var go=LevelPieceFactory.Child(root.transform.Find("Mechanisms"),name).gameObject; var g=go.AddComponent<PuzzleGroup>();g.Configure(id);return g; }
        private static GameObject Place(GameObject root,LevelSession s,LevelPiece kind,Transform parent,string name,float x,float y)
        { var go=LevelPieceFactory.Place(kind,parent,new Vector2(x,y),s); go.name=name; return go; }
        private static void Solid(GameObject root,LevelSession s,string name,float x,float y,float w,float h)
        { var go=Place(root,s,LevelPiece.Solid,root.transform.Find("Geometry"),name,x,y); M2SceneAuthoring.Resize(go,new Vector2(w,h)); }
        private static void Fragment(GameObject root,LevelSession s,string id,float x,float y)
        { var go=Place(root,s,LevelPiece.Fragment,root.transform.Find("Collectibles"),id,x,y);go.GetComponent<StarFragment>().Configure(id,s,go.transform.Find("Visual").gameObject); }
        private static void Dais(GameObject root,LevelSession s,PuzzleGroup group,float x,float y,string name)
        { var go=Place(root,s,LevelPiece.Checkpoint,root.transform.Find("Checkpoints"),name,x,y);go.GetComponent<Checkpoint>().ConfigurePuzzle(group); }
        private static ProgressCondition Gate(GameObject root,LevelSession s,Transform parent,string name,float x,float y,float w,float h,string[] fragments,string[] mechanisms=null)
        {
            var condition=LevelPieceFactory.Child(parent,name+"Condition").gameObject.AddComponent<ProgressCondition>();
            condition.Configure(s,fragments,mechanisms);
            var go=Barrier(root,s,parent,name,x,y,w,h,condition,false);
            var nodes=LevelPieceFactory.Child(go.transform,"ProgressNodes"); nodes.position=new Vector2(x,2.2f);
            nodes.gameObject.AddComponent<ChapterGoalView>().Configure(s,condition);
            return condition;
        }
        private static GameObject Barrier(GameObject root,LevelSession s,Transform parent,string name,float x,float y,float w,float h,BoolSignal signal,bool bridge)
        {
            var go=Place(root,s,bridge?LevelPiece.Bridge:LevelPiece.Gate,parent,name,x,y);
            M2SceneAuthoring.Resize(go,new Vector2(w,h));
            go.GetComponent<SignalBarrier>().Configure(signal,bridge,go.GetComponent<BoxCollider2D>(),go.GetComponentInChildren<SpriteRenderer>()); return go;
        }
        private static void UpdateMenu()
        {
            var scene=SceneManager.GetSceneByPath(GameFlow.MainMenuScene);
            var opened=!scene.IsValid() || !scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(GameFlow.MainMenuScene,OpenSceneMode.Additive);
            try
            {
                var menu=scene.GetRootGameObjects()[0].GetComponentInChildren<MainMenuController>(true);
                menu.transform.Find("Home/Start/Label").GetComponent<UnityEngine.UI.Text>().text="开始第一章";
                MainMenuPresentationAuthoring.ApplyHomeLayout(menu.transform);
                EditorSceneManager.SaveScene(scene,GameFlow.MainMenuScene);
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
        public static void ConfigureBuild()
        {
            PlayerSettings.productName = GameFlow.GameTitle;
            EditorBuildSettings.scenes=new[]{GameFlow.MainMenuScene,GameFlow.FirstLevelScene,GameFlow.SecondLevelScene,GameFlow.GalleryScene}
                .Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(GameFlow.MainMenuScene);
        }
    }
}
