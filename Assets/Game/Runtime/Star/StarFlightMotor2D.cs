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
        private readonly HashSet<string> touched = new HashSet<string>(StringComparer.Ordinal);
        private StarFlightState path;
        private float speed;
        private float range;
        public StarFlightState Path => path;
        public event Action<StarContact> Impact;

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
            touched.Clear();
        }

        public bool Tick(float deltaTime)
        {
            contacts.Clear();
            solver.Advance(ref path, Mathf.Min(speed * deltaTime, range - path.Travelled), null, contacts);
            body.position = path.Position;
            foreach (var contact in contacts)
            {
                var fragment = contact.Collider.GetComponentInParent<StarFragment>();
                if (fragment != null)
                {
                    if (touched.Add(fragment.StableId)) fragment.TryCollect();
                }
                else
                    Impact?.Invoke(contact);
            }
            return path.Ended || path.Travelled >= range - 0.0001f;
        }
    }
}
