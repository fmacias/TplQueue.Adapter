using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Snapshots;
using Moq;
using NUnit.Framework;

namespace Fmacias.TplQueue.Observers.Test.Observers
{
    [TestFixture]
    public class JobEventSnapshotMapperTests
    {
        private static readonly DateTime Timestamp = new(2026, 10, 13, 12, 30, 0, DateTimeKind.Utc);
        private IJobEventSnapshotFactory _mapper = null!;

        [SetUp]
        public void SetUp()
        {
            _mapper = JobEventSnapshotMapper.Create();
        }

        [Test]
        public void Capture_OrdinaryJob_CopiesEventAndExecutionMetadata()
        {
            // Arrange
            var info = new JobInfoStub
            {
                Name = "Import",
                IsCompleted = true,
                ExecutionStart = Timestamp.AddSeconds(-2),
                ExecutionEnd = Timestamp,
                ExecutionTime = TimeSpan.FromSeconds(2),
                Status = TaskStatus.RanToCompletion,
                CrossQueueId = Guid.NewGuid()
            };
            var source = CreateEvent(info);
            source.Setup(value => value.RetryCount).Returns(3);

            // Act
            var snapshot = _mapper.Capture(source.Object);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Status, Is.EqualTo(JobEventStatus.Successed));
                Assert.That(snapshot.Timestamp, Is.EqualTo(Timestamp));
                Assert.That(snapshot.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(snapshot.RetryCount, Is.EqualTo(3));
                Assert.That(snapshot.ExecutionChannel, Is.Null);
                Assert.That(snapshot.JobInfo.Id, Is.EqualTo(info.Id));
                Assert.That(snapshot.JobInfo.Name, Is.EqualTo(info.Name));
                Assert.That(snapshot.JobInfo.IsCompleted, Is.True);
                Assert.That(snapshot.JobInfo.ExecutionStart, Is.EqualTo(info.ExecutionStart));
                Assert.That(snapshot.JobInfo.ExecutionEnd, Is.EqualTo(info.ExecutionEnd));
                Assert.That(snapshot.JobInfo.ExecutionTime, Is.EqualTo(info.ExecutionTime));
                Assert.That(snapshot.JobInfo.Status, Is.EqualTo(info.Status));
                Assert.That(snapshot.JobInfo.CrossQueueId, Is.EqualTo(info.CrossQueueId));
                Assert.That(snapshot.JobInfo.Dependencies, Is.Empty);
                Assert.That(snapshot.HasException, Is.False);
                Assert.That(snapshot.Diagnostic, Is.SameAs(JobExceptionDiagnostic.None));
            });
        }

        [TestCase(null)]
        [TestCase(0)]
        [TestCase(7)]
        public void Capture_ExecutionEvent_CopiesOptionalChannel(int? channel)
        {
            var source = CreateEvent(new JobInfoStub());
            source.As<IJobExecutionEvent>().Setup(value => value.ExecutionChannel).Returns(channel);

            var snapshot = _mapper.Capture(source.Object);

            Assert.That(snapshot.ExecutionChannel, Is.EqualTo(channel));
        }

        [Test]
        public void Capture_NestedDependencies_PreservesOrderAndDetachesMutableSources()
        {
            // Arrange
            var leaf = new JobInfoStub { Name = "Leaf" };
            var branch = new JobInfoStub { Name = "Branch", Dependencies = new[] { leaf } };
            var sibling = new JobInfoStub { Name = "Sibling" };
            var dependencies = new List<IJobInfo> { branch, sibling };
            var root = new JobInfoStub { Name = "Root", Dependencies = dependencies };

            // Act
            var snapshot = _mapper.Capture(CreateEvent(root).Object);
            root.Name = "Changed root";
            leaf.Name = "Changed leaf";
            branch.Dependencies = Array.Empty<IJobInfo>();
            dependencies.Clear();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.JobInfo.Name, Is.EqualTo("Root"));
                Assert.That(snapshot.JobInfo.Dependencies.Select(item => item.Id),
                    Is.EqualTo(new[] { branch.Id, sibling.Id }));
                Assert.That(snapshot.JobInfo.Dependencies[0].Dependencies[0].Name, Is.EqualTo("Leaf"));
                Assert.That(snapshot.JobInfo.Dependencies[0].Dependencies[0].Id, Is.EqualTo(leaf.Id));
            });
            var collection = (IList<JobInfoSnapshot>)snapshot.JobInfo.Dependencies;
            Assert.That(collection.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => collection.Clear());
        }

        [Test]
        public void Capture_SharedDependency_ReusesDetachedNodeWithoutReportingCycle()
        {
            var shared = new JobInfoStub();
            var left = new JobInfoStub { Dependencies = new[] { shared } };
            var right = new JobInfoStub { Dependencies = new[] { shared } };
            var root = new JobInfoStub { Dependencies = new[] { left, right, shared } };

            var snapshot = _mapper.Capture(CreateEvent(root).Object);

            var capturedShared = snapshot.JobInfo.Dependencies[2];
            Assert.Multiple(() =>
            {
                Assert.That(capturedShared.Id, Is.EqualTo(shared.Id));
                Assert.That(snapshot.JobInfo.Dependencies[0].Dependencies[0], Is.SameAs(capturedShared));
                Assert.That(snapshot.JobInfo.Dependencies[1].Dependencies[0], Is.SameAs(capturedShared));
            });
        }

        [Test]
        public void Capture_EqualButDistinctJobs_DoesNotMergeTheirMetadata()
        {
            var first = new EqualJobInfoStub { Name = "First" };
            var second = new EqualJobInfoStub { Id = first.Id, Name = "Second" };
            var root = new JobInfoStub { Dependencies = new[] { first, second } };

            var snapshot = _mapper.Capture(CreateEvent(root).Object);

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.JobInfo.Dependencies.Select(item => item.Name),
                    Is.EqualTo(new[] { "First", "Second" }));
                Assert.That(snapshot.JobInfo.Dependencies[0], Is.Not.SameAs(snapshot.JobInfo.Dependencies[1]));
            });
        }

        [Test]
        public void Capture_RepeatedStatus_CapturesEveryEventIndependently()
        {
            var info = new JobInfoStub { Name = "First" };
            var firstEvent = CreateEvent(info);
            var secondEvent = CreateEvent(info);
            secondEvent.Setup(value => value.Timestamp).Returns(Timestamp.AddSeconds(1));
            secondEvent.Setup(value => value.RetryCount).Returns(1);

            var first = _mapper.Capture(firstEvent.Object);
            info.Name = "Second";
            var second = _mapper.Capture(secondEvent.Object);

            Assert.Multiple(() =>
            {
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(second.JobInfo, Is.Not.SameAs(first.JobInfo));
                Assert.That(second.Status, Is.EqualTo(first.Status));
                Assert.That(first.JobInfo.Name, Is.EqualTo("First"));
                Assert.That(second.JobInfo.Name, Is.EqualTo("Second"));
                Assert.That(first.Timestamp, Is.EqualTo(Timestamp));
                Assert.That(second.Timestamp, Is.EqualTo(Timestamp.AddSeconds(1)));
                Assert.That(first.RetryCount, Is.Zero);
                Assert.That(second.RetryCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void Capture_NullSource_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => _mapper.Capture(null!));

            Assert.That(exception!.ParamName, Is.EqualTo("source"));
        }

        [Test]
        public void Capture_NullJobInfo_ThrowsArgumentException()
        {
            var source = CreateEvent(null!);

            var exception = Assert.Throws<ArgumentException>(() => _mapper.Capture(source.Object));

            Assert.That(exception!.ParamName, Is.EqualTo("source"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Capture_NullDependencyCollection_ThrowsInvalidOperationException(bool nested)
        {
            var invalid = new JobInfoStub { Dependencies = null! };
            var root = nested ? new JobInfoStub { Dependencies = new[] { invalid } } : invalid;

            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.Capture(CreateEvent(root).Object));

            Assert.That(exception!.Message, Does.Contain(invalid.Id.ToString()));
        }

        [Test]
        public void Capture_NullDependencyElement_ThrowsInvalidOperationException()
        {
            var info = new JobInfoStub { Dependencies = new IJobInfo[] { null! } };

            var exception = Assert.Throws<InvalidOperationException>(
                () => _mapper.Capture(CreateEvent(info).Object));

            Assert.That(exception!.Message, Does.Contain(info.Id.ToString()));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Capture_DependencyCycle_ThrowsAndDoesNotPoisonLaterCapture(bool indirect)
        {
            // Arrange
            var root = new JobInfoStub();
            var child = new JobInfoStub { Dependencies = new[] { root } };
            root.Dependencies = new[] { indirect ? child : root };
            var source = CreateEvent(root);

            // Act / Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _mapper.Capture(source.Object));
            Assert.That(exception!.Message, Does.Contain(root.Id.ToString()));
            root.Dependencies = Array.Empty<IJobInfo>();
            Assert.That(_mapper.Capture(source.Object).JobInfo.Dependencies, Is.Empty);
        }

        [Test]
        public void Capture_SourceGetterThrows_PropagatesOriginalFailure()
        {
            var failure = new InvalidOperationException("metadata unavailable");
            var source = CreateEvent(new JobInfoStub());
            source.Setup(value => value.JobInfo).Throws(failure);

            var exception = Assert.Throws<InvalidOperationException>(() => _mapper.Capture(source.Object));

            Assert.That(exception, Is.SameAs(failure));
        }

        [Test]
        public void Capture_Exception_CopiesDiagnosticsAndInnerException()
        {
            // Arrange
            var inner = new ArgumentException("Invalid input");
            var error = new DiagnosticException(inner);
            var source = CreateEvent(new JobInfoStub(), error);

            // Act
            var snapshot = _mapper.Capture(source.Object);
            error.Source = "changed source";
            error.HelpLink = "changed link";

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.HasException, Is.True);
                Assert.That(snapshot.Diagnostic.TypeName, Is.EqualTo(typeof(DiagnosticException).FullName));
                Assert.That(snapshot.Diagnostic.Message, Is.EqualTo("Failure"));
                Assert.That(snapshot.Diagnostic.StackTrace, Is.EqualTo("at Example.Run()"));
                Assert.That(snapshot.Diagnostic.Source, Is.EqualTo("worker"));
                Assert.That(snapshot.Diagnostic.HelpLink, Is.EqualTo("https://example.test/help"));
                Assert.That(snapshot.Diagnostic.HResult, Is.EqualTo(123));
                Assert.That(snapshot.Diagnostic.InnerExceptions, Has.Count.EqualTo(1));
                Assert.That(snapshot.Diagnostic.InnerExceptions[0].TypeName, Is.EqualTo(typeof(ArgumentException).FullName));
                Assert.That(snapshot.Diagnostic.InnerExceptions[0].Message, Is.EqualTo("Invalid input"));
                Assert.That(snapshot.Diagnostic.InnerExceptions[0].InnerExceptions, Is.Empty);
            });
        }

        [Test]
        public void Capture_NullExceptionStrings_UsesEmptyDiagnosticsStrings()
        {
            var error = new Mock<Exception>();
            error.SetupGet(value => value.Message).Returns((string)null!);
            error.SetupGet(value => value.StackTrace).Returns((string)null!);
            error.SetupGet(value => value.Source).Returns((string)null!);
            error.SetupGet(value => value.HelpLink).Returns((string)null!);

            var diagnostic = _mapper.Capture(CreateEvent(new JobInfoStub(), error.Object).Object).Diagnostic;

            Assert.Multiple(() =>
            {
                Assert.That(diagnostic.HasException, Is.True);
                Assert.That(diagnostic.Message, Is.Empty);
                Assert.That(diagnostic.StackTrace, Is.Empty);
                Assert.That(diagnostic.Source, Is.Empty);
                Assert.That(diagnostic.HelpLink, Is.Empty);
            });
        }

        [Test]
        public void Capture_AggregateException_PreservesAllChildrenAndSharedDiagnostics()
        {
            var shared = new InvalidOperationException("shared");
            var branch = new Exception("branch", shared);
            var error = new AggregateException("aggregate", shared, branch, shared);

            var diagnostic = _mapper.Capture(CreateEvent(new JobInfoStub(), error).Object).Diagnostic;

            Assert.Multiple(() =>
            {
                Assert.That(diagnostic.InnerExceptions, Has.Count.EqualTo(3));
                Assert.That(diagnostic.InnerExceptions.Select(item => item.Message),
                    Is.EqualTo(new[] { "shared", "branch", "shared" }));
                Assert.That(diagnostic.InnerExceptions[2], Is.SameAs(diagnostic.InnerExceptions[0]));
                Assert.That(diagnostic.InnerExceptions[1].InnerExceptions[0], Is.SameAs(diagnostic.InnerExceptions[0]));
            });
            var children = (IList<JobExceptionDiagnostic>)diagnostic.InnerExceptions;
            Assert.That(children.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => children.Clear());
        }

        [Test]
        public void Capture_SameExceptionInSeparateCaptures_DoesNotReuseDiagnosticGraph()
        {
            var source = CreateEvent(new JobInfoStub(), new Exception("outer", new Exception("inner")));

            var first = _mapper.Capture(source.Object);
            var second = _mapper.Capture(source.Object);

            Assert.Multiple(() =>
            {
                Assert.That(second.Diagnostic, Is.Not.SameAs(first.Diagnostic));
                Assert.That(second.Diagnostic.InnerExceptions[0], Is.Not.SameAs(first.Diagnostic.InnerExceptions[0]));
            });
        }

        [Test]
        public void Capture_DataJobAndExceptionWithRuntimeReferences_RetainsOnlyMetadataTransitively()
        {
            // Arrange: runtime references are reachable from both source graphs.
            var payload = new object();
            var child = new DataJobInfoStub { Payload = payload, Name = "Data child" };
            var root = new DataJobInfoStub { Payload = payload, Dependencies = new[] { child } };
            var error = new Exception("outer", new Exception("inner"));
            var source = CreateEvent(root, error);
            error.Data["event"] = source.Object;
            error.InnerException!.Data["payload"] = payload;
            error.Data["task"] = Task.CompletedTask;
            error.Data["handler"] = (Action)(() => GC.KeepAlive(payload));

            // Act
            var snapshot = _mapper.Capture(source.Object);

            // Assert: inspect stored fields, not only public properties, without GC timing assumptions.
            Assert.That(snapshot.JobInfo.Dependencies[0].Name, Is.EqualTo("Data child"));
            AssertMetadataOnlyGraph(snapshot, new HashSet<object>(ReferenceEqualityComparer.Instance));
        }

        private static Mock<IJobEvent> CreateEvent(IJobInfo info, Exception? error = null)
        {
            var source = new Mock<IJobEvent>();
            source.Setup(value => value.JobInfo).Returns(info);
            source.Setup(value => value.Status).Returns(JobEventStatus.Successed);
            source.Setup(value => value.Timestamp).Returns(Timestamp);
            source.Setup(value => value.Exception).Returns(error);
            return source;
        }

        private static void AssertMetadataOnlyGraph(object? value, HashSet<object> visited)
        {
            if (value == null || !visited.Add(value))
                return;

            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is Guid ||
                value is DateTime || value is TimeSpan)
                return;

            var isCollection = type.IsArray ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>));
            Assert.That(value is JobEventSnapshot || value is JobInfoSnapshot ||
                value is JobExceptionDiagnostic || isCollection, Is.True,
                $"Snapshot retains unexpected runtime type {type.FullName}.");

            if (value is Array array)
            {
                foreach (var item in array)
                    AssertMetadataOnlyGraph(item, visited);
                return;
            }

            for (var current = type; current != null; current = current.BaseType)
            {
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    AssertMetadataOnlyGraph(field.GetValue(value), visited);
            }
        }

        private class JobInfoStub : IJobInfo
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Name { get; set; } = "Job";
            public bool IsCompleted { get; set; }
            public DateTime ExecutionStart { get; set; }
            public TimeSpan ExecutionTime { get; set; }
            public DateTime ExecutionEnd { get; set; }
            public TaskStatus Status { get; set; }
            public Guid CrossQueueId { get; set; }
            public IReadOnlyCollection<IJobInfo> Dependencies { get; set; } = Array.Empty<IJobInfo>();
        }

        private sealed class EqualJobInfoStub : JobInfoStub
        {
            public override bool Equals(object? obj) => obj is EqualJobInfoStub other && other.Id == Id;
            public override int GetHashCode() => Id.GetHashCode();
        }

        private sealed class DataJobInfoStub : JobInfoStub, IDataJobInfo
        {
            public object Payload { get; set; } = null!;
            public string PayloadHandlerKey => throw new AssertionException("Payload metadata must not be read.");
            public string Serialize(IUniversalDataSerializer serializer) =>
                throw new AssertionException("Payload must not be serialized.");
        }

        private sealed class DiagnosticException : Exception
        {
            public DiagnosticException(Exception inner) : base("Failure", inner)
            {
                Source = "worker";
                HelpLink = "https://example.test/help";
                HResult = 123;
            }

            public override string StackTrace => "at Example.Run()";
        }
    }
}
