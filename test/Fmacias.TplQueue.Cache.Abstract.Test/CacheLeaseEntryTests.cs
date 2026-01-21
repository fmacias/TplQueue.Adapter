using System;
using System.Threading;
using Fmacias.TplQueue;
using Fmacias.TplQueue.Contracts;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Cache.Abstract.Test
{
    [TestFixture]
    public sealed class CacheLeaseEntryTests
    {
        [Test]
        public void Create_ValidArguments_PopulatesProperties_AndDefaults()
        {
            // Arrange
            var leaseId = Guid.NewGuid();
            var jobRootId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var parentJobId = Guid.NewGuid();
            var retryDescriptor = Mock.Of<IRetryPolicyDescriptor>();

            var nodeDto = new Mock<IJobNodeDto>();
            nodeDto.SetupGet(n => n.RetryDescriptor).Returns(retryDescriptor);
            nodeDto.SetupGet(n => n.IsRoot).Returns(true);
            var cacheUtc = DateTime.UtcNow;

            // Act
            var entry = CacheLeaseEntry.Create(
                leaseId,
                jobRootId,
                jobId,
                parentJobId,
                nodeDto.Object,
                cacheUtc);

            // Assert
            Assert.That(entry.LeaseId, Is.EqualTo(leaseId));
            Assert.That(entry.JobRootId, Is.EqualTo(jobRootId));
            Assert.That(entry.JobId, Is.EqualTo(jobId));
            Assert.That(entry.ParentJobId, Is.EqualTo(parentJobId));
            Assert.That(entry.JobNodeDto, Is.SameAs(nodeDto.Object));
            Assert.That(entry.CacheUtc, Is.EqualTo(cacheUtc).Within(TimeSpan.FromSeconds(1)));
            Assert.That(entry.RetryDescriptor, Is.SameAs(retryDescriptor));
            Assert.That(entry.Status, Is.EqualTo(EntryStatus.Pending));
            Assert.That(entry.IsRoot, Is.True);
        }

        [Test]
        public void Create_EmptyIds_Throws()
        {
            var nodeDto = Mock.Of<IJobNodeDto>();

            Assert.Throws<ArgumentException>(() =>
                CacheLeaseEntry.Create(
                    Guid.Empty,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    nodeDto,
                    DateTime.UtcNow));

            Assert.Throws<ArgumentException>(() =>
                CacheLeaseEntry.Create(
                    Guid.NewGuid(),
                    Guid.Empty,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    nodeDto,
                    DateTime.UtcNow));

            Assert.Throws<ArgumentException>(() =>
                CacheLeaseEntry.Create(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.Empty,
                    Guid.NewGuid(),
                    nodeDto,
                    DateTime.UtcNow));
        }

        [Test]
        public void MarkLeased_SetsStatusToLeased()
        {
            var entry = CreateDefaultEntry();

            entry.MarkLeased();

            Assert.That(entry.Status, Is.EqualTo(EntryStatus.Leased));
        }

        [Test]
        public void MarkAck_UpdatesPayloadJsonAndStatus()
        {
            // Arrange
            var nodeDto = new Mock<IJobNodeDto>();
            nodeDto.SetupGet(n => n.RetryDescriptor).Returns(Mock.Of<IRetryPolicyDescriptor>());

            var entry = CacheLeaseEntry.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                nodeDto.Object,
                DateTime.UtcNow);

            var payload = new Mock<ISerializedPayload>();
            payload.SetupGet(p => p.JsonOutput).Returns("{\"result\":1}");

            // Act
            entry.MarkAck(payload.Object);

            // Assert
            nodeDto.Verify(n => n.UpdatePayloadJson("{\"result\":1}"), Times.Once);
            Assert.That(entry.Status, Is.EqualTo(EntryStatus.Acknownledged));
        }

        [Test]
        public void MarkAck_WithNullPayload_Throws()
        {
            var entry = CreateDefaultEntry();

            Assert.Throws<ArgumentNullException>(() => entry.MarkAck(null!));
        }

        [Test]
        public void MarkFailed_SetsStatusToFailed()
        {
            var entry = CreateDefaultEntry();

            entry.MarkFailed();

            Assert.That(entry.Status, Is.EqualTo(EntryStatus.Failed));
        }

        [Test]
        public void MarkCanceled_SetsStatusToCanceled()
        {
            var entry = CreateDefaultEntry();

            entry.MarkCanceled();

            Assert.That(entry.Status, Is.EqualTo(EntryStatus.Canceled));
        }

        [Test]
        public void MarkRemoved_SetsStatusToRemoved()
        {
            var entry = CreateDefaultEntry();

            entry.MarkAsDeleted();

            Assert.That(entry.Status, Is.EqualTo(EntryStatus.Pending));
            Assert.That(entry.Deleted, Is.EqualTo(true));

        }

        private static CacheLeaseEntry CreateDefaultEntry()
        {
            var nodeDto = new Mock<IJobNodeDto>();
            nodeDto.SetupGet(n => n.RetryDescriptor).Returns(Mock.Of<IRetryPolicyDescriptor>());

            return (CacheLeaseEntry)CacheLeaseEntry.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                nodeDto.Object,
                DateTime.UtcNow);
        }
    }
}
