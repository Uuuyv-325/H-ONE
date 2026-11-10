using System;
using System.Collections.Generic;
using UnityEngine;

namespace HOne.Game
{
    public sealed class StarFlightMotor2D
    {
        private readonly StarPathSolver solver;
        private readonly Rigidbody2D body;
        private readonly PlayerTuning tuning;
        private readonly List<StarContact> contacts = new List<StarContact>(16);
        private readonly List<Vector2> points = new List<Vector2>(12);
        private readonly HashSet<string> touched = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<StarInstrument> instruments = new HashSet<StarInstrument>();
        private readonly HashSet<FlutePort> flutePorts = new HashSet<FlutePort>();
        private StarFlightState path;
        private float speed;
        private float range;
        private float charge;
        private int shotId;
        public StarFlightState Path => path;
        public event Action<StarContact> Impact;
        // Borrowed list: consumers must copy points during this callback.
        public event Action<IReadOnlyList<Vector2>> PathAdvanced;

        public StarFlightMotor2D(Rigidbody2D rigidbody, PlayerTuning config, int mask, Transform self)
        {
            body = rigidbody;
            tuning = config;
            solver = new StarPathSolver(config.StarRadius, mask, self);
        }

        public void Launch(Vector2 aim, float charge)
        {
            path = new StarFlightState(body.position, aim);
            speed = tuning.SpeedAt(charge);
            range = tuning.RangeAt(charge);
            this.charge = Mathf.Clamp01(charge);
            shotId++;
            touched.Clear();
            instruments.Clear();
            flutePorts.Clear();
        }

        public bool Tick(float deltaTime)
        {
            contacts.Clear();
            points.Clear();
            points.Add(path.Position);
            solver.Advance(ref path, Mathf.Min(speed * deltaTime, range - path.Travelled), points, contacts);
            body.position = path.Position;
            PathAdvanced?.Invoke(points);
            foreach (var queryContact in contacts)
            {
                var contact = queryContact.WithShot(speed, charge, shotId);
                var starSwitch = contact.Collider.GetComponentInParent<StarSwitch>();
                if (starSwitch != null) { starSwitch.Strike(); continue; }
                var flute = contact.Collider.GetComponentInParent<FlutePort>();
                if (flute != null) { if (flutePorts.Add(flute)) flute.Strike(); continue; }
                var fragment = contact.Collider.GetComponentInParent<StarFragment>();
                if (fragment != null)
                {
                    if (touched.Add(fragment.StableId)) fragment.TryCollect();
                }
                else
                {
                    var instrument = contact.Collider.GetComponentInParent<StarInstrument>();
                    if (instrument != null)
                    {
                        if (!instruments.Add(instrument)) continue;
                        instrument.Strike();
                    }
                    var wall = contact.Collider.GetComponentInParent<BrittleWall>();
                    if (wall != null)
                    {
                        if (wall.Broken) continue;
                        wall.Break();
                    }
                    if (contact.Embedded)
                    {
                        var socket = contact.Collider.GetComponent<StarSocket>();
                        if (socket == null || !socket.TryEmbed()) continue;
                    }
                    Impact?.Invoke(contact);
                }
            }
            return path.Ended || path.Travelled >= range - 0.0001f;
        }
    }
}
