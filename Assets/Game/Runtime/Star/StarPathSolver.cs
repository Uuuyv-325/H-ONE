using System.Collections.Generic;
using UnityEngine;

namespace HOne.Game
{
    public struct StarFlightState
    {
        public Vector2 Position;
        public Vector2 Direction;
        public float Travelled;
        public int BouncesLeft;
        public bool Ended;
        internal Collider2D LastSurface;
        internal Vector2 LastNormal;
        internal HashSet<BrittleWall> PredictedBroken;

        public StarFlightState(Vector2 origin, Vector2 direction)
        {
            Position = origin;
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            Travelled = 0f;
            BouncesLeft = 1;
            Ended = false;
            LastSurface = null;
            LastNormal = Vector2.zero;
            PredictedBroken = new HashSet<BrittleWall>();
        }
    }

    public enum StarImpactKind { Stone, Lyre, Drum, Ceramic, Socket, UnavailableSocket, Hazard }

    public readonly struct StarContact
    {
        public readonly Collider2D Collider;
        public readonly Vector2 Point;
        public readonly Vector2 Normal;
        public readonly bool Reflected;
        public readonly bool Embedded;
        public readonly StarImpactKind Kind;
        public readonly Vector2 IncomingDirection;
        public readonly Vector2 OutgoingDirection;
        public readonly float Speed;
        public readonly float Charge;
        public readonly int ShotId;
        public StarContact(RaycastHit2D hit, bool reflected, bool embedded = false, Vector2 incoming = default)
        {
            Collider = hit.collider;
            Point = hit.point;
            Normal = hit.normal;
            Reflected = reflected;
            Embedded = embedded;
            IncomingDirection = incoming;
            OutgoingDirection = reflected ? Vector2.Reflect(incoming, hit.normal).normalized : incoming;
            Speed = Charge = 0f;
            ShotId = 0;
            var instrument = hit.collider != null ? hit.collider.GetComponentInParent<StarInstrument>() : null;
            Kind = hit.collider == null ? StarImpactKind.Stone :
                hit.collider.GetComponentInParent<Hazard2D>() != null ? StarImpactKind.Hazard :
                hit.collider.GetComponentInParent<BrittleWall>() != null ? StarImpactKind.Ceramic :
                instrument != null ? (instrument.Kind == StarInstrumentKind.Lyre ? StarImpactKind.Lyre : StarImpactKind.Drum) :
                hit.collider.GetComponent<StarSocket>() != null ? (embedded ? StarImpactKind.Socket : StarImpactKind.UnavailableSocket) : StarImpactKind.Stone;
        }

        private StarContact(StarContact contact, float speed, float charge, int shotId)
        {
            Collider = contact.Collider; Point = contact.Point; Normal = contact.Normal;
            Reflected = contact.Reflected; Embedded = contact.Embedded; Kind = contact.Kind;
            IncomingDirection = contact.IncomingDirection; OutgoingDirection = contact.OutgoingDirection;
            Speed = speed; Charge = charge; ShotId = shotId;
        }
        public StarContact WithShot(float speed, float charge, int shotId) => new StarContact(this, speed, charge, shotId);
    }

    // A read-only query: callers decide whether to apply contacts to gameplay.
    // Both preview and flight keep their own state and invoke this exact solver.
    public sealed class StarPathSolver
    {
        public const float Skin = 0.005f;
        private const int MaximumIterations = 8;
        private readonly float radius;
        private readonly ContactFilter2D filter;
        private readonly Transform ignoredRoot;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(32);

        public StarPathSolver(float starRadius, int worldMask, Transform self = null)
        {
            radius = starRadius;
            filter = new ContactFilter2D { useLayerMask = true, layerMask = worldMask, useTriggers = true };
            ignoredRoot = self;
        }

        private static int Priority(Collider2D collider)
        {
            if (collider.GetComponentInParent<Hazard2D>() != null) return 0;
            var socket = collider.GetComponent<StarSocket>();
            if (socket != null && socket.Available) return 1;
            if (collider.GetComponentInParent<StarFragment>() != null) return 2;
            return 3;
        }

        private static int CompareHits(RaycastHit2D a, RaycastHit2D b)
        {
            var distance = a.distance.CompareTo(b.distance);
            if (Mathf.Abs(a.distance - b.distance) > 0.0001f) return distance;
            var priority = Priority(a.collider).CompareTo(Priority(b.collider));
            return priority != 0 ? priority : a.collider.GetInstanceID().CompareTo(b.collider.GetInstanceID());
        }

        public void Advance(ref StarFlightState state, float distance, List<Vector2> points,
            List<StarContact> contacts)
        {
            var remaining = Mathf.Max(0f, distance);
            for (var iteration = 0; remaining > 0.00001f && !state.Ended; iteration++)
            {
                if (iteration == MaximumIterations)
                {
                    state.Ended = true;
                    break;
                }
                hits.Clear();
                Physics2D.CircleCast(state.Position, radius, state.Direction, filter, hits, remaining);
                hits.Sort(CompareHits);
                var blocked = false;
                foreach (var hit in hits)
                {
                    var collider = hit.collider;
                    if (collider == null || (ignoredRoot != null && collider.transform.IsChildOf(ignoredRoot)))
                        continue;
                    var wall = collider.GetComponentInParent<BrittleWall>();
                    if (wall != null)
                    {
                        if (!wall.Broken && state.PredictedBroken.Add(wall)) contacts?.Add(new StarContact(hit, false, false, state.Direction));
                        continue;
                    }
                    var hazard = collider.GetComponentInParent<Hazard2D>() != null;
                    var fragment = collider.GetComponentInParent<StarFragment>() != null;
                    var socket = collider.GetComponent<StarSocket>();
                    if (collider.isTrigger && !hazard && socket == null)
                    {
                        if (fragment || collider.GetComponentInParent<StarSwitch>() != null ||
                            collider.GetComponentInParent<FlutePort>() != null)
                            contacts?.Add(new StarContact(hit, false, false, state.Direction));
                        continue;
                    }
                    // A cast may still report the surface we are leaving at distance zero.
                    if (!hazard && hit.distance < Skin && collider == state.LastSurface &&
                        Vector2.Dot(state.Direction, state.LastNormal) > 0f)
                        continue;

                    state.Position += state.Direction * hit.distance;
                    state.Travelled += hit.distance;
                    remaining -= hit.distance;
                    points?.Add(state.Position);
                    var instrument = collider.GetComponentInParent<StarInstrument>();
                    if (instrument != null && instrument.Kind == StarInstrumentKind.Drum)
                        foreach (var target in instrument.WaveTargets)
                            if (target != null && Vector2.Distance(instrument.transform.position, target.transform.position) <= instrument.WaveRadius)
                                state.PredictedBroken.Add(target);
                    if (!hazard && socket != null && socket.Available)
                    {
                        contacts?.Add(new StarContact(hit, false, true, state.Direction));
                        state.Ended = true;
                        return;
                    }
                    var reflect = !hazard && state.BouncesLeft > 0 && hit.distance > 0.00001f;
                    contacts?.Add(new StarContact(hit, reflect, false, state.Direction));
                    if (!reflect)
                    {
                        state.Ended = true;
                        return;
                    }
                    state.BouncesLeft--;
                    state.Direction = Vector2.Reflect(state.Direction, hit.normal).normalized;
                    state.LastSurface = collider;
                    state.LastNormal = hit.normal;
                    var separation = Mathf.Min(Skin, remaining);
                    state.Position += hit.normal * separation;
                    if (separation > 0f) points?.Add(state.Position);
                    state.Travelled += separation;
                    remaining -= separation;
                    blocked = true;
                    break;
                }
                if (blocked) continue;
                state.Position += state.Direction * remaining;
                state.Travelled += remaining;
                points?.Add(state.Position);
                remaining = 0f;
            }
        }
    }
}
