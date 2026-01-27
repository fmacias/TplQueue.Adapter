using System;
using System.Collections.Generic;
using Fmacias.TplQueue.Contracts;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Cache.Abstract.Test
{
    [TestFixture]
    public sealed class TaskGraphDtoTests
    {
        [Test]
        public void ExtractNodes_SingleNodeGraph_ProducesSingleNodeAndInvokesCallback()
        {
            // Arrange
            var retrySerializer = new Mock<IRetryPolicySerializable>(MockBehavior.Loose);
            var payloadSerializer = new Mock<IJsonUniversalPayloadSerializer>(MockBehavior.Loose);

            payloadSerializer
                .Setup(s => s.Serialize(It.IsAny<object>(), It.IsAny<Type>()))
                .Returns("{}");
            var rootId = Guid.NewGuid();

            // Root mock (also an IPayloadCarrier)
            Mock<IPayloadJobRoot<IPayloadCommand>> root = GetRootGraphMock(rootId);

            var rootAsCarrier = root.As<IPayloadCarrierJob>();
            rootAsCarrier.Setup(c => c.GetPayloadDependencies()).Returns(Array.Empty<IPayloadCarrierJob>());
            rootAsCarrier.SetupGet(c => c.PayloadType).Returns(typeof(string));
            rootAsCarrier.Setup(c => c.GetPayload()).Returns("payload");
            Func<IRetryPolicy> retryPolicyFactory = () => Mock.Of<IRetryPolicy>();
            rootAsCarrier.Setup(c => c.GetRetryPolicyFactory()).Returns(retryPolicyFactory);

            // GetRetryPolicyFactory is not configured; MockBehavior.Loose will return default,
            // and Mock<IRetryPolicySerializer> will accept it.

            var dto = (TaskGraphDto)TaskGraphDto.Create(payloadSerializer.Object, root.Object, isFifo: false);

  
            var callbackNodes = new List<(IJobNodeDto Node, Guid RootId)>();

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

        private static Mock<IPayloadJobRoot<IPayloadCommand>> GetRootGraphMock(Guid rootId)
        {
            var root = new Mock<IPayloadJobRoot<IPayloadCommand>>(MockBehavior.Loose);
            root.SetupGet(r => r.Id).Returns(rootId);
            root.SetupGet(r => r.Name).Returns("root");
            return root;
        }

        [Test]
        public void ExtractNodes_LinearGraph_ProducesCorrectParentChildRelationship()
        {
            var retrySerializer = new Mock<IRetryPolicySerializable>(MockBehavior.Loose);
            var payloadSerializer = new Mock<IJsonUniversalPayloadSerializer>(MockBehavior.Loose);

            payloadSerializer
                .Setup(s => s.Serialize(It.IsAny<object>(), It.IsAny<Type>()))
                .Returns("{}");

            var rootId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var root = new Mock<IPayloadJobRoot<IPayloadCommand>>(MockBehavior.Loose);
            var child = new Mock<IPayloadCarrierJob>(MockBehavior.Loose);
            child.SetupGet(c => c.Id).Returns(childId);
            child.SetupGet(c => c.Name).Returns("child");
            child.Setup(c => c.GetPayloadDependencies()).Returns(Array.Empty<IPayloadCarrierJob>());
            child.SetupGet(c => c.PayloadType).Returns(typeof(string));
            child.Setup(c => c.GetPayload()).Returns("payload-child");
            Func<IRetryPolicy> childRetryPolicyFactory = () => Mock.Of<IRetryPolicy>();
            child.Setup(c => c.GetRetryPolicyFactory()).Returns(childRetryPolicyFactory);

            root.SetupGet(r => r.Id).Returns(rootId);
            root.SetupGet(r => r.Name).Returns("root");
            root.Setup(c => c.GetPayloadDependencies()).Returns(new[] { child.Object });
            root.SetupGet(c => c.PayloadType).Returns(typeof(string));
            root.Setup(c => c.GetPayload()).Returns("payload-root");
            Func<IRetryPolicy> retryPolicyFactory = () => Mock.Of<IRetryPolicy>();
            root.Setup(c => c.GetRetryPolicyFactory()).Returns(retryPolicyFactory);
            var dto = (TaskGraphDto)TaskGraphDto.Create(payloadSerializer.Object, root.Object, isFifo: false);
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
    }
}
