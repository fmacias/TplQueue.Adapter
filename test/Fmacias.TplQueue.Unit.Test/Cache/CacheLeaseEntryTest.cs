using NUnit.Framework;
using Moq;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Cache.Abstract;

namespace Fmacias.TplQueue.Test.Cache
{
    [TestFixture]
    public class CacheLeaseEntryTests
    {
        [Test]
        public void Create_SetsAllProperties_AndDefaultsToPending()
        {
            var leaseId = Guid.NewGuid();
            var rootId = Guid.NewGuid();
            var runnerId = Guid.NewGuid();
            var parentRunnerId = rootId;
            var mockRetryDescriptor = new Mock<IRetryPolicyDescriptor>();
            var nodeDto = Mock.Of<IJobNodeDto>(n =>
                n.JobId == runnerId &&
                n.ParentJobId == parentRunnerId &&
                n.Name == "node" &&
                n.PayloadJson == "{}" &&
                n.PayloadType == "type" && 
                n.IsFifo == true &&
                n.IsRoot == true &&
                n.RetryDescriptor == mockRetryDescriptor.Object);

            var ct = new CancellationTokenSource().Token;

            var entry = Facade.CreateLeaseEntry(
                leaseId,
                rootId,
                runnerId,
                parentRunnerId,
                nodeDto,
                cacheUtc: DateTime.UtcNow);
            Assert.That(entry.LeaseId, Is.EqualTo(leaseId));
            Assert.That(entry.JobRootId, Is.EqualTo(rootId));
            Assert.That(entry.JobId, Is.EqualTo(runnerId));
            Assert.That(entry.ParentJobId, Is.EqualTo(parentRunnerId));
            Assert.That(entry.JobNodeDto, Is.EqualTo(nodeDto));
            Assert.That(entry.IsFifo, Is.True);
            Assert.That(entry.Status, Is.EqualTo(EntryStatus.Pending));
            Assert.That(entry.IsRoot, Is.True);
            Assert.That(entry.RetryDescriptor, Is.SameAs(mockRetryDescriptor.Object));
        }

        [Test]
        public void MarkLeased_ChangesStatusToLeased()
        {
            var entry = BuildEntry();
            entry.MarkLeased();
            Assert.That(entry.Status, Is.EqualTo(EntryStatus.Leased));
        }

        [Test]
        public void MarkAck_ChangesStatusToAcknowledged()
        {
            var entry = BuildEntry();
            var serializedPayloadMock = new Mock<ISerializedPayload>(MockBehavior.Strict);
            serializedPayloadMock.Setup(o => o.JsonOutput).Returns("{ }");

            entry.MarkAck(serializedPayloadMock.Object);
            Assert.That(entry.Status, Is.EqualTo(EntryStatus.Acknownledged));
        }

        [Test]
        public void MarkFailed_ChangesStatusToFailed()
        {
            var entry = BuildEntry();
            entry.MarkFailed();
            Assert.That(entry.Status, Is.EqualTo(EntryStatus.Failed));
        }

        [Test]
        public void MarkCanceled_ChangesStatusToCanceled()
        {
            var entry = BuildEntry();
            entry.MarkCanceled();
            Assert.That(entry.Status, Is.EqualTo(EntryStatus.Canceled));
        }

        private static CacheLeaseEntry BuildEntry()
        {
            var leaseId = Guid.NewGuid();
            var rootId = Guid.NewGuid();
            var runnerId = Guid.NewGuid();

            var nodeDto = Mock.Of<IJobNodeDto>(n =>
                n.JobId == runnerId &&
                n.Name == "node" &&
                n.PayloadJson == "{}" &&
                n.PayloadType == "type" &&
                n.IsRoot == false);

            var desc = Mock.Of<IRetryPolicyDescriptor>();
            return (CacheLeaseEntry) Facade.CreateLeaseEntry(
                leaseId,
                rootId,
                runnerId,
                parentJobId: Guid.Empty,
                nodeDto,
                cacheUtc: DateTime.UtcNow);
        }
    }
}
