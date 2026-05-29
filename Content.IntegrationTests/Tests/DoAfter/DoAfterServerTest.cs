using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.DoAfter
{
    [TestFixture]
    [TestOf(typeof(DoAfterComponent))]
    public sealed partial class DoAfterServerTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: DoAfterDummy
  id: DoAfterDummy
  components:
  - type: DoAfter
";

        [Serializable, NetSerializable]
        private sealed partial class TestDoAfterEvent : DoAfterEvent
        {
            public override DoAfterEvent Clone()
            {
                return this;
            }
        };

        // TODO: Add integration test for checking that the DoAfter event is also yaml-serializable for persistance reasons.
        // Some DoAfter events currently violate this since they store NetEntities, which are not yaml-serializable.
        // Fixing this is tricky though since you cannot use EntityUids either since they are not (net)serializable.
        // But in some cases they can just use the existing user, target, used or eventTarget entities in the DoAfterEntityComponent.
        [Test]
        public async Task TestSerializable()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            await server.WaitIdleAsync();
            var refMan = server.ResolveDependency<IReflectionManager>();

            await server.WaitPost(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var type in refMan.GetAllChildren<DoAfterEvent>(true))
                    {
                        if (type.IsAbstract || type == typeof(TestDoAfterEvent))
                            continue;

                        Assert.That(type.HasCustomAttribute<NetSerializableAttribute>()
                                    && type.HasCustomAttribute<SerializableAttribute>(),
                            $"{nameof(DoAfterEvent)} is not NetSerializable. Event: {type.Name}");
                    }
                });
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestFinished()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var timing = server.ResolveDependency<IGameTiming>();
            var doAfterSystem = entityManager.EntitySysManager.GetEntitySystem<SharedDoAfterSystem>();
            var ev = new TestDoAfterEvent();
            Entity<DoAfterEntityComponent>? doAfterEnt = null;

            // That it finishes successfully
            await server.WaitAssertion(() =>
            {
                var tickTime = 1.0f / timing.TickRate;
                var mob = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                var args = new DoAfterArgs(tickTime / 2, ev)
                {
                    Broadcast = true,
                };
#pragma warning disable NUnit2045 // Interdependent assertions.
                Assert.That(doAfterSystem.TryStartDoAfter(args, out doAfterEnt, mob, null, null, null));
                Assert.That(doAfterSystem.IsRunning(doAfterEnt.Value.AsNullable()), Is.True);
                Assert.That(doAfterSystem.IsCancelled(doAfterEnt.Value.AsNullable()), Is.False);
#pragma warning restore NUnit2045
            });

            await server.WaitRunTicks(1);
            Assert.That(doAfterSystem.IsRunning(doAfterEnt.Value.AsNullable()), Is.False);
            Assert.That(doAfterSystem.IsCancelled(doAfterEnt.Value.AsNullable()), Is.True);

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestCancelled()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var entityManager = server.ResolveDependency<IEntityManager>();
            var timing = server.ResolveDependency<IGameTiming>();
            var doAfterSystem = entityManager.EntitySysManager.GetEntitySystem<SharedDoAfterSystem>();
            var ev = new TestDoAfterEvent();
            Entity<DoAfterEntityComponent>? doAfterEnt = null;

            await server.WaitAssertion(() =>
            {
                var tickTime = 1.0f / timing.TickRate;

                var mob = entityManager.SpawnEntity("DoAfterDummy", MapCoordinates.Nullspace);
                var args = new DoAfterArgs(tickTime / 2, ev)
                {
                    Broadcast = true,
                };

                if (!doAfterSystem.TryStartDoAfter(args, out doAfterEnt, mob, null, null, null))
                {
                    Assert.Fail();
                    return;
                }

                Assert.That(doAfterSystem.IsRunning(doAfterEnt.Value.AsNullable()), Is.True);
                Assert.That(doAfterSystem.IsCancelled(doAfterEnt.Value.AsNullable()), Is.False);

                doAfterSystem.Cancel(doAfterEnt.Value);
                Assert.That(doAfterSystem.IsRunning(doAfterEnt.Value.AsNullable()), Is.False);
                Assert.That(doAfterSystem.IsCancelled(doAfterEnt.Value.AsNullable()), Is.True);

            });

            await server.WaitRunTicks(3);
            Assert.That(doAfterSystem.IsRunning(doAfterEnt.Value.AsNullable()), Is.False);
            Assert.That(doAfterSystem.IsCancelled(doAfterEnt.Value.AsNullable()), Is.True);

            await pair.CleanReturnAsync();
        }
    }
}
