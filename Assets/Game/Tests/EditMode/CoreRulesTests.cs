using System.Collections.Generic;
using HOne.Platformer;
using NUnit.Framework;
using UnityEngine;

namespace HOne.Game.Tests
{
    public sealed class CoreRulesTests
    {
        private readonly List<Object> owned = new List<Object>();
        private const int Layer = 30;
        private const int Mask = 1 << Layer;
        private GameObject New(string name)
        {
            var go = new GameObject(name); go.layer = Layer; owned.Add(go); return go;
        }
        private BoxCollider2D Wall(Vector2 position, Vector2 size, float angle = 0f)
        {
            var go = New("surface"); go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var collider = go.AddComponent<BoxCollider2D>(); collider.size = size;
            Physics2D.SyncTransforms(); return collider;
        }
        private LevelSession Session()
        {
            var definition = ScriptableObject.CreateInstance<LevelDefinition>(); owned.Add(definition);
            var session = New("session").AddComponent<LevelSession>(); session.Configure(definition, null); return session;
        }
        private StarFragment Fragment(LevelSession session, Vector2 position, bool composite = false)
        {
            var go = New("fragment"); go.transform.position = position;
            var collider = go.AddComponent<CircleCollider2D>(); collider.radius = 0.2f; collider.isTrigger = true;
            if (composite) { var second = go.AddComponent<BoxCollider2D>(); second.size = Vector2.one * 0.4f; second.isTrigger = true; }
            var fragment = go.AddComponent<StarFragment>(); fragment.Configure("core-f1", session, null);
            Physics2D.SyncTransforms(); return fragment;
        }
        [TearDown] public void Cleanup()
        {
            for (var i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        [Test] public void CollectionUsesDeclaredUniqueIdsAndNotifiesOnce()
        {
            var session = Session(); var events = 0; session.FragmentCollected += _ => events++;
            Assert.IsFalse(session.TryCollect("unknown")); Assert.IsFalse(session.TryCollect(""));
            Assert.IsTrue(session.TryCollect("core-f1")); Assert.IsFalse(session.TryCollect("core-f1"));
            Assert.AreEqual(1, session.FragmentCount); Assert.AreEqual(1, events);
            Assert.AreEqual(0, Session().FragmentCount);
            Assert.AreEqual("core-f1", session.Definition.FragmentIds[0]);
        }

        [TestCase(0.01f)] [TestCase(0.06f)]
        public void FullSpeedSweepReflectsAtThinWall(float width)
        {
            Wall(new Vector2(1f, 0f), new Vector2(width, 10f));
            var path = new StarFlightState(Vector2.zero, Vector2.right);
            var points = new List<Vector2>();
            new StarPathSolver(0.22f, Mask).Advance(ref path, 2f, points, null);
            // Unity 2D shape queries include a small contact tolerance (about 0.005 here).
            Assert.That(points[0].x, Is.EqualTo(1f - width / 2f - 0.22f).Within(0.01f));
            Assert.Less(path.Direction.x, -0.99f); Assert.AreEqual(0, path.BouncesLeft);
            Assert.Less(path.Position.x, 1f);
        }

        [Test] public void OneBounceBudgetIsSharedAcrossSurfacesAndSteps()
        {
            Wall(new Vector2(2f, 0f), new Vector2(0.1f, 10f));
            Wall(new Vector2(-2f, 0f), new Vector2(0.1f, 10f));
            var path = new StarFlightState(Vector2.zero, Vector2.right);
            var solver = new StarPathSolver(0.22f, Mask);
            solver.Advance(ref path, 3f, null, null);
            Assert.IsFalse(path.Ended); Assert.AreEqual(0, path.BouncesLeft);
            solver.Advance(ref path, 10f, null, null);
            Assert.IsTrue(path.Ended); Assert.That(path.Position.x, Is.EqualTo(-1.73f).Within(0.01f));
        }

        [Test] public void AngledSurfaceUsesActualNormal()
        {
            Wall(new Vector2(2f, 0f), new Vector2(0.1f, 8f), 30f);
            var path = new StarFlightState(Vector2.zero, Vector2.right);
            var contacts = new List<StarContact>();
            new StarPathSolver(0.22f, Mask).Advance(ref path, 5f, null, contacts);
            Assert.AreEqual(1, contacts.Count);
            Assert.That(Vector2.Distance(path.Direction, Vector2.Reflect(Vector2.right, contacts[0].Normal)), Is.LessThan(0.001f));
        }

        [TestCase(30)] [TestCase(60)] [TestCase(120)]
        public void PartitionedFlightMatchesWholePreview(int fps)
        {
            Wall(new Vector2(2f, 0f), new Vector2(0.06f, 12f));
            Wall(new Vector2(-3f, 0f), new Vector2(0.06f, 12f));
            var preview = new StarFlightState(Vector2.zero, new Vector2(1f, 0.12f));
            var actual = preview;
            var solver = new StarPathSolver(0.22f, Mask);
            solver.Advance(ref preview, 18f, null, null);
            for (var i = 0; i < fps && !actual.Ended; i++) solver.Advance(ref actual, 18f / fps, null, null);
            Assert.IsTrue(actual.Ended); Assert.IsTrue(preview.Ended);
            Assert.That(Vector2.Distance(actual.Position, preview.Position), Is.LessThan(0.01f));
        }

        [Test] public void PredictionHasNoCollectionSideEffectsAndWallOccludesFragment()
        {
            var session = Session(); var fragment = Fragment(session, Vector2.right);
            Wall(new Vector2(2f, 0f), new Vector2(0.06f, 10f));
            var behind = New("behind"); behind.transform.position = new Vector2(3f, 0f);
            var trigger = behind.AddComponent<CircleCollider2D>(); trigger.isTrigger = true;
            behind.AddComponent<StarFragment>().Configure("behind", session, null);
            Physics2D.SyncTransforms();
            var path = new StarFlightState(Vector2.zero, Vector2.right); var contacts = new List<StarContact>();
            new StarPathSolver(0.22f, Mask).Advance(ref path, 5f, null, contacts);
            Assert.AreEqual(0, session.FragmentCount); Assert.IsTrue(fragment.GetComponent<Collider2D>().enabled);
            Assert.IsTrue(contacts.Exists(c => c.Collider == fragment.GetComponent<Collider2D>()));
            Assert.IsFalse(contacts.Exists(c => c.Collider == trigger));
        }

        [Test] public void CompoundFragmentCollectsOnceWhenReachedBeforeWall()
        {
            var session = Session(); Fragment(session, new Vector2(0.4f, 0f), true);
            Wall(new Vector2(0.9f, 0f), new Vector2(0.01f, 10f));
            var body = New("star").AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            var tuning = ScriptableObject.CreateInstance<PlayerTuning>(); owned.Add(tuning);
            var events = 0; session.FragmentCollected += _ => events++;
            var motor = new StarFlightMotor2D(body, tuning, Mask, body.transform);
            motor.Launch(Vector2.right, 1f); motor.Tick(0.1f);
            Assert.AreEqual(1, session.FragmentCount); Assert.AreEqual(1, events);
        }

        [Test] public void HazardWinsCoincidentWallAndTerminatesWithoutBounce()
        {
            var solid = Wall(new Vector2(1f, 0f), Vector2.one);
            var hazard = Wall(new Vector2(1f, 0f), Vector2.one); hazard.isTrigger = true;
            hazard.gameObject.AddComponent<Hazard2D>(); Physics2D.SyncTransforms();
            var path = new StarFlightState(Vector2.zero, Vector2.right);
            var contacts = new List<StarContact>();
            new StarPathSolver(0.22f, Mask).Advance(ref path, 4f, null, contacts);
            Assert.IsTrue(path.Ended); Assert.AreEqual(1, path.BouncesLeft);
            Assert.AreEqual(hazard, contacts[0].Collider);
        }

        [Test] public void DecorativeTriggersAndOwnCollidersDoNotBlock()
        {
            var decorative = Wall(Vector2.right, Vector2.one); decorative.isTrigger = true;
            var self = Wall(Vector2.zero, Vector2.one);
            Physics2D.SyncTransforms();
            var path = new StarFlightState(Vector2.zero, Vector2.right);
            new StarPathSolver(0.22f, Mask, self.transform).Advance(ref path, 4f, null, null);
            Assert.IsFalse(path.Ended); Assert.AreEqual(1, path.BouncesLeft);
            Assert.That(path.Position.x, Is.EqualTo(4f).Within(0.001f));
        }

        [Test] public void CornerAndInitialOverlapTerminateInBoundedTime()
        {
            Wall(Vector2.zero, Vector2.one);
            var path = new StarFlightState(Vector2.zero, Vector2.one);
            new StarPathSolver(0.22f, Mask).Advance(ref path, 18f, null, null);
            Assert.IsTrue(path.Ended); Assert.IsFalse(float.IsNaN(path.Position.x));
        }

        [Test] public void SmallRoomCentersCameraAtAllTargetPositions()
        {
            var bounds = New("bounds").AddComponent<LevelBounds2D>(); bounds.SetBounds(new Vector2(-2f, -1f), new Vector2(2f, 1f));
            Assert.AreEqual(new Vector3(0f, 0f, -10f), bounds.ClampCameraCenter(new Vector3(500f, -200f, -10f), 9.6f, 5.4f));
        }

        [TestCase(0f, 10f)] [TestCase(1f, 18f)]
        public void MeasuredOpenFlightRangeRespectsTimeAndDistance(float charge, float expected)
        {
            var tuning = ScriptableObject.CreateInstance<PlayerTuning>(); owned.Add(tuning);
            var body = New("star").AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            var motor = new StarFlightMotor2D(body, tuning, Mask, body.transform);
            motor.Launch(Vector2.right, charge);
            var ended = false;
            for (var i = 0; i < 60 && !ended; i++) ended = motor.Tick(0.02f);
            Assert.IsTrue(ended);
            Assert.That(body.position.x, Is.EqualTo(expected).Within(0.002f));
            TestContext.WriteLine("Open range at charge " + charge + ": " + body.position.x.ToString("F3") + " units");
        }
    }
}
