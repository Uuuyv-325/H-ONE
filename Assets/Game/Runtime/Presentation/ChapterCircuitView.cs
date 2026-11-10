using System;
using UnityEngine;

namespace HOne.Game
{
    // Engraved relationship, never a second owner of the mechanism's state.
    public sealed class ChapterCircuitView : MonoBehaviour
    {
        [SerializeField] private BoolSignal source;
        [SerializeField] private Vector2[] path = Array.Empty<Vector2>();
        [SerializeField] private bool round = true;
        private GameplayFeedbackMesh mesh;
        public void Configure(BoolSignal signal, bool circles, params Vector2[] points)
        { source = signal; round = circles; path = points; }
        private void Start() => mesh = new GameplayFeedbackMesh(transform, "InlaidCircuit", null, 10);
        private void LateUpdate()
        {
            if (mesh == null || source == null || path.Length < 2) return;
            mesh.Begin();
            var tint = source.Value ? new Color(1,.82f,.4f,.8f) : new Color(.65f,.75f,.86f,.5f);
            for(var i=1;i<path.Length;i++)
            {
                var steps = Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(path[i-1],path[i])/.22f));
                for(var j=0;j<steps;j++) mesh.Line(Vector2.Lerp(path[i-1],path[i],j/(float)steps),
                    Vector2.Lerp(path[i-1],path[i],(j+(source.Value?1f:.45f))/steps),.022f,tint);
            }
            foreach(var p in new[]{path[0],path[path.Length-1]})
            {
                if(round) mesh.Arc(p,.19f,0,360,.035f,tint); else mesh.Diamond(p,.19f,.035f,tint);
                if(source.Value) mesh.Star(p,.12f,tint);
            }
            mesh.Upload();
        }
        private void OnDestroy() => mesh?.Dispose();
    }
}
