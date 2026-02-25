using Fmacias.TplQueue.Cache.DomainModels;
using Fmacias.TplQueue.Cache.Factories;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;

namespace Fmacias.TplQueue.Cache.Abstract.Test.DomainModels
{
    [TestFixture]
    public sealed class CacheEntryTests
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
            var entry = CacheEntry.Create(
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
                CacheEntry.Create(
                    Guid.Empty,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    nodeDto,
                    DateTime.UtcNow));

            Assert.Throws<ArgumentException>(() =>
                CacheEntry.Create(
                    Guid.NewGuid(),
                    Guid.Empty,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    nodeDto,
                    DateTime.UtcNow));

            Assert.Throws<ArgumentException>(() =>
                CacheEntry.Create(
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
            nodeDto
                .SetupGet(n => n.RetryDescriptor)
                .Returns(Mock.Of<IRetryPolicyDescriptor>());

            var entry = CacheEntry.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                nodeDto.Object,
                DateTime.UtcNow);

            var payloadSerializable = new Mock<ISerializable>();
            payloadSerializable
                .Setup(p => 
                    p.Serialize(It.IsAny<IUniversalDataSerializer>())
                ).Returns("{\"result\":1}");

            // Act
            entry.MarkAck(
                payloadSerializable.Object, 
                Mock.Of<IUniversalDataSerializer>());

            // Assert
            nodeDto.Verify(n => n.UpdatePayloadJson("{\"result\":1}"), 
                Times.Once);
            Assert.That(entry.Status, Is.EqualTo(EntryStatus.Acknownledged));
        }

        [Test]
        public void MarkAck_WithNullPayload_Throws()
        {
            var entry = CreateDefaultEntry();
            Assert.Throws<ArgumentNullException>(() => entry.MarkAck(null!, Mock.Of<IUniversalDataSerializer>()));
            Assert.Throws<ArgumentNullException>(() => entry.MarkAck(Mock.Of<ISerializable>(), null!));
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

        private static CacheEntry CreateDefaultEntry()
        {
            var nodeDto = new Mock<IJobNodeDto>();
            nodeDto.SetupGet(n => n.RetryDescriptor).Returns(Mock.Of<IRetryPolicyDescriptor>());

            return (CacheEntry)CacheEntry.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                nodeDto.Object,
                DateTime.UtcNow);
        }
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
                n.PayloadTypeName == "type" &&
                n.IsFifo == true &&
                n.IsRoot == true &&
                n.RetryDescriptor == mockRetryDescriptor.Object);

            var ct = new CancellationTokenSource().Token;

            var entry = CacheEntryFactory.Create().CreateEntry(
                leaseId,
                rootId,
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
            var serializedPayloadMock = new Mock<ISerializable>(MockBehavior.Strict);
            serializedPayloadMock.Setup(o => o.Serialize(It.IsAny<IUniversalDataSerializer>())).Returns("{ }");

            entry.MarkAck(serializedPayloadMock.Object, Mock.Of<IUniversalDataSerializer>());
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

        private static ICacheEntry BuildEntry()
        {
            var leaseId = Guid.NewGuid();
            var rootId = Guid.NewGuid();
            var runnerId = Guid.NewGuid();

            var nodeDto = Mock.Of<IJobNodeDto>(n =>
                n.JobId == runnerId &&
                n.Name == "node" &&
                n.PayloadJson == "{}" &&
                n.PayloadTypeName == "type" &&
                n.IsRoot == false);

            var desc = Mock.Of<IRetryPolicyDescriptor>();
            return CacheEntryFactory.Create().CreateEntry(
                leaseId,
                rootId,
                nodeDto,
                cacheUtc: DateTime.UtcNow);
        }
    }
}
