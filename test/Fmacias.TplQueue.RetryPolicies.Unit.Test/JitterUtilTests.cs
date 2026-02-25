using NUnit.Framework;

namespace Fmacias.TplQueue.RetryPolicies.Test
{
    [TestFixture]
    public class JitterUtilTests
    {
        [Test]
        public void JitterMs_WithNonPositiveBase_ClampsToAtLeastOne()
        {
            using var jitterUtil = JitterUtil.Create();
            var jitter = jitterUtil.JitterMs(baseMs: 0, percent: 0.25);

            Assert.That(jitter, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void JitterMs_WithInvalidPercent_ReturnsBaseDelay()
        {
            using var jitterUtil = JitterUtil.Create();
            var negativePercent = jitterUtil.JitterMs(baseMs: 100, percent: -1);
            var nanPercent = jitterUtil.JitterMs(baseMs: 100, percent: double.NaN);
            var infinitePercent = jitterUtil.JitterMs(baseMs: 100, percent: double.PositiveInfinity);

            Assert.That(negativePercent, Is.EqualTo(100));
            Assert.That(nanPercent, Is.EqualTo(100));
            Assert.That(infinitePercent, Is.EqualTo(100));
        }

        [Test]
        public void NextRandomDouble_ReturnsValueWithinExpectedRange()
        {
            using var jitterUtil = JitterUtil.Create();
            var value = jitterUtil.NextRandomDouble();

            Assert.That(value, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(value, Is.LessThan(1.0));
        }

        [Test]
        public void JitterUtil_AfterDispose_ThrowsObjectDisposedException()
        {
            var jitterUtil = JitterUtil.Create();
            jitterUtil.Dispose();

            Assert.Throws<ObjectDisposedException>(() => jitterUtil.JitterMs(baseMs: 100, percent: 0.2));
        }
    }
}
