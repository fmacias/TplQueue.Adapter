using System;
using System.Collections.Generic;
using Fmacias.TplQueue.Cache.Abstract.Helpers;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Cache.Abstract.Test.Helpers
{
    [TestFixture]
    public sealed class TaskGraphDtoTests
    {
        [Test]
        public void ExtractNodes_NullCallback_ThrowsArgumentNullException()
        {
            // Arrange
            var payloadSerializer = new Mock<IUniversalDataSerializer>(MockBehavior.Loose);
            payloadSerializer
                .Setup(s => s.Serialize(It.IsAny<object>(), It.IsAny<Type>()))
                .Returns("{}");

            var payload = new DummyPayload("task-graph/root");
            var root = GetRootGraphMock(Guid.NewGuid());
            root.Setup(c => c.GetDependentDataJobs()).Returns(Array.Empty<IDataJob>());
            root.As<IDataJobInfo>().SetupGet(c => c.PayloadHandlerKey).Returns(payload.PayloadId);
            root.Setup(c => c.GetPayload()).Returns(payload);
            root.As<ISerializable>()
                .Setup(s => s.Serialize(It.IsAny<IUniversalDataSerializer>()))
                .Returns("{}");
            root.Setup(c => c.GetRetryPolicyFactory()).Returns(() => NoRetryPolicy.Create());

            var dto = JobGraphDto.Create(payloadSerializer.Object, root.Object, isFifo: false);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => dto.ExtractNodes(null!));
        }

        [Test]
        public void ExtractNodes_SingleNodeGraph_ProducesSingleNodeAndInvokesCallback()
        {
            // Arrange
            var payloadSerializer = new Mock<IUniversalDataSerializer>(MockBehavior.Loose);

            payloadSerializer
                .Setup(s => s.Serialize(It.IsAny<object>(), It.IsAny<Type>()))
                .Returns("{}");
            var rootId = Guid.NewGuid();

            // Root mock (also an IPayloadCarrier)
            Mock<IDataJobRoot<IPayload>> root = GetRootGraphMock(rootId);
            var payload = new DummyPayload("task-graph/root");

            var dataJobNode = root.As<IDataJobNode>();
            dataJobNode.Setup(c => c.GetDependentDataJobs()).Returns(Array.Empty<IDataJob>());
            dataJobNode.SetupGet(c => c.PayloadType).Returns(typeof(DummyPayload));
            root.As<IDataJobInfo>().SetupGet(c => c.PayloadHandlerKey).Returns(payload.PayloadId);
            dataJobNode.Setup(c => c.GetPayload()).Returns(payload);
            root.As<ISerializable>()
                .Setup(s => s.Serialize(It.IsAny<IUniversalDataSerializer>()))
                .Returns("{}");
            Func<IRetryPolicy> retryPolicyFactory = () => NoRetryPolicy.Create();
            dataJobNode.Setup(c => c.GetRetryPolicyFactory()).Returns(retryPolicyFactory);

            // GetRetryPolicyFactory is not configured; MockBehavior.Loose will return default,
            // and Mock<IRetryPolicySerializer> will accept it.

            var dto = (JobGraphDto)JobGraphDto.Create(payloadSerializer.Object, root.Object, isFifo: false);

  
            var callbackNodes = new List<(IJobNodeRecord Node, Guid RootId)>();

            // Act
            var nodes = dto.ExtractNodes(edgedNodeCallBack: (node, rid) => callbackNodes.Add((node, rid)));

            // Assert
            Assert.AreEqual(1, nodes.Count);
            Assert.That(nodes, Has.Count.EqualTo(1));
            Assert.That(callbackNodes, Has.Count.EqualTo(1));

            var single = nodes[0];
            Assert.That(single.JobId, Is.EqualTo(rootId));
            Assert.That(single.ParentJobId, Is.EqualTo(Guid.Empty));
            Assert.That(single.IsRoot, Is.True);
            Assert.That(single.PayloadJson, Is.EqualTo("{}"));
            Assert.That(callbackNodes[0].RootId, Is.EqualTo(rootId));
        }

        private static Mock<IDataJobRoot<IPayload>> GetRootGraphMock(Guid rootId)
        {
            var root = new Mock<IDataJobRoot<IPayload>>(MockBehavior.Loose);
            root.SetupGet(r => r.Id).Returns(rootId);
            root.SetupGet(r => r.Name).Returns("root");
            return root;
        }

        [Test]
        public void ExtractNodes_LinearGraph_ProducesCorrectParentChildRelationship()
        {
            var payloadSerializer = new Mock<IUniversalDataSerializer>(MockBehavior.Loose);

            payloadSerializer
                .Setup(s => s.Serialize(It.IsAny<object>(), It.IsAny<Type>()))
                .Returns("{}");

            var rootId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var root = new Mock<IDataJobRoot<IPayload>>(MockBehavior.Loose);
            var child = new Mock<IDataJob>(MockBehavior.Loose);
            var childPayload = new DummyPayload("task-graph/child");
            var rootPayload = new DummyPayload("task-graph/root");
            child.SetupGet(c => c.Id).Returns(childId);
            child.SetupGet(c => c.Name).Returns("child");
            child.Setup(c => c.GetDependentDataJobs()).Returns(Array.Empty<IDataJob>());
            child.SetupGet(c => c.PayloadType).Returns(typeof(DummyPayload));
            child.As<IDataJobInfo>().SetupGet(c => c.PayloadHandlerKey).Returns(childPayload.PayloadId);
            child.Setup(c => c.GetPayload()).Returns(childPayload);
            child.As<ISerializable>()
                .Setup(s => s.Serialize(It.IsAny<IUniversalDataSerializer>()))
                .Returns("{}");
            Func<IRetryPolicy> childRetryPolicyFactory = () => NoRetryPolicy.Create();
            child.Setup(c => c.GetRetryPolicyFactory()).Returns(childRetryPolicyFactory);

            root.SetupGet(r => r.Id).Returns(rootId);
            root.SetupGet(r => r.Name).Returns("root");
            root.Setup(c => c.GetDependentDataJobs()).Returns(new[] { child.Object });
            root.As<IDataJobNode>().SetupGet(c => c.PayloadType).Returns(typeof(DummyPayload));
            root.As<IDataJobInfo>().SetupGet(c => c.PayloadHandlerKey).Returns(rootPayload.PayloadId);
            root.As<IDataJobNode>().Setup(c => c.GetPayload()).Returns(rootPayload);
            root.As<ISerializable>()
                .Setup(s => s.Serialize(It.IsAny<IUniversalDataSerializer>()))
                .Returns("{}");
            Func<IRetryPolicy> retryPolicyFactory = () => NoRetryPolicy.Create();
            root.Setup(c => c.GetRetryPolicyFactory()).Returns(retryPolicyFactory);
            var dto = (JobGraphDto)JobGraphDto.Create(payloadSerializer.Object, root.Object, isFifo: false);
            var nodes = dto.ExtractNodes((n, rid) => { });
            Assert.That(nodes, Has.Count.EqualTo(2));
            var rootNode = AssertSingle(nodes, n => n.JobId == rootId);
            var childNode = AssertSingle(nodes, n => n.JobId == childId);

            Assert.That(rootNode.IsRoot, Is.True);
            Assert.That(rootNode.ParentJobId, Is.EqualTo(Guid.Empty));
            Assert.That(childNode.IsRoot, Is.False);
            Assert.That(childNode.ParentJobId, Is.EqualTo(rootId));
        }

        private static IJobNodeDto AssertSingle(
            IReadOnlyList<IJobNodeDto> nodes,
            Predicate<IJobNodeDto> predicate)
        {
            var matches = new List<IJobNodeDto>();
            foreach (var n in nodes)
            {
                if (predicate(n))
                {
                    matches.Add(n);
                }
            }

            Assert.That(matches, Has.Count.EqualTo(1),
                "Expected exactly one node to match the predicate.");
            return matches[0];
        }

        private sealed class DummyPayload : IPayload
        {
            public DummyPayload(string payloadId)
            {
                PayloadId = payloadId;
            }

            public string PayloadId { get; }
            public DateTime CollectionTime => DateTime.UtcNow;
        }
    }
}
