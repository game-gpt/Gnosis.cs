using Gnosis.Network;
using NUnit.Framework;

namespace Gnosis.Tests.Network
{
    public class LockstepSystemTests : TestBase
    {
        private LockstepSystem _system = null!;
        private NetworkManager _networkManager = null!;

        public override void Setup()
        {
            base.Setup();
            _networkManager = new NetworkManager(NetworkBackendType.None);
            _system = new LockstepSystem(_networkManager, tickRate: 30);
        }

        public override void Teardown()
        {
            _networkManager.Shutdown();
            base.Teardown();
        }

        [Test]
        public void LockstepSystem_实现ILockstepSystem接口()
        {
            Assert.That(_system, Is.InstanceOf<ILockstepSystem>());
        }

        [Test]
        public void TickRate_返回设置的帧率()
        {
            Assert.That(_system.TickRate, Is.EqualTo(30));
        }

        [Test]
        public void CurrentFrame_初始为0()
        {
            Assert.That(_system.CurrentFrame, Is.EqualTo(0));
        }

        [Test]
        public void ReadyToAdvance_无玩家时为False()
        {
            Assert.That(_system.ReadyToAdvance, Is.False);
        }

        [Test]
        public void RegisterPlayer_增加玩家数量()
        {
            _system.RegisterPlayer(1);

            Assert.That(_system.ConnectedPlayerCount, Is.EqualTo(1));
        }

        [Test]
        public void UnregisterPlayer_减少玩家数量()
        {
            _system.RegisterPlayer(1);
            _system.UnregisterPlayer(1);

            Assert.That(_system.ConnectedPlayerCount, Is.EqualTo(0));
        }

        [Test]
        public void SubmitInput_收集玩家输入()
        {
            _system.RegisterPlayer(1);
            _system.SubmitInput(1, new byte[] { 42 });

            var input = _system.GetPlayerInput(1);

            Assert.That(input, Is.EqualTo(new byte[] { 42 }));
        }

        [Test]
        public void ReadyToAdvance_所有玩家提交输入后为True()
        {
            _system.RegisterPlayer(1);
            _system.RegisterPlayer(2);
            _system.SubmitInput(1, new byte[] { 1 });
            _system.SubmitInput(2, new byte[] { 2 });

            Assert.That(_system.ReadyToAdvance, Is.True);
        }

        [Test]
        public void ReadyToAdvance_部分玩家未提交时为False()
        {
            _system.RegisterPlayer(1);
            _system.RegisterPlayer(2);
            _system.SubmitInput(1, new byte[] { 1 });

            Assert.That(_system.ReadyToAdvance, Is.False);
        }

        [Test]
        public void TryAdvance_未准备好时返回False()
        {
            var result = _system.TryAdvance();

            Assert.That(result, Is.False);
        }

        [Test]
        public void TryAdvance_准备好后推进帧号()
        {
            _system.RegisterPlayer(1);
            _system.SubmitInput(1, new byte[] { 1 });

            _system.TryAdvance();

            Assert.That(_system.CurrentFrame, Is.EqualTo(1));
        }

        [Test]
        public void SubmitStateHash_相同哈希不触发事件()
        {
            bool desyncFired = false;
            _system.OnDesyncDetected += (_, _, _) => desyncFired = true;

            _system.SubmitStateHash(1, 12345);
            _system.SubmitStateHash(1, 12345);

            Assert.That(desyncFired, Is.False);
        }

        [Test]
        public void SubmitStateHash_不同哈希触发事件()
        {
            int? desyncFrame = null;
            _system.OnDesyncDetected += (frame, _, _) => desyncFrame = frame;

            _system.SubmitStateHash(1, 12345);
            _system.SubmitStateHash(1, 99999);

            Assert.That(desyncFrame, Is.EqualTo(1));
        }

        [Test]
        public void OnLockstepUpdate_触发OnFrameAdvanced事件()
        {
            int? advancedFrame = null;
            _system.OnFrameAdvanced += (frame, _) => advancedFrame = frame;

            var inputs = new Dictionary<int, byte[]> { { 1, new byte[] { 1 } } };
            _system.OnLockstepUpdate(5, inputs);

            Assert.That(advancedFrame, Is.EqualTo(5));
        }

        [Test]
        public void Clear_重置所有状态()
        {
            _system.RegisterPlayer(1);
            _system.SubmitInput(1, new byte[] { 1 });
            _system.OnLockstepUpdate(10, new Dictionary<int, byte[]>());

            _system.Clear();

            Assert.That(_system.CurrentFrame, Is.EqualTo(0));
            Assert.That(_system.ConnectedPlayerCount, Is.EqualTo(0));
        }
    }
}
