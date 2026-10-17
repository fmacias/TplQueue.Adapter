using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Snapshots;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fmacias.TplQueue.Observers
{
    /// <summary>
    /// Converts observed events into detached, immutable snapshot DTOs.
    /// Each capture uses independent traversal state.
    /// </summary>
    internal sealed class JobEventSnapshotMapper : IJobEventSnapshotFactory
    {
        private JobEventSnapshotMapper()
        {
        }
        public static IJobEventSnapshotFactory Create()
        {
            return new JobEventSnapshotMapper();
        }
        /// <summary>
        /// Captures one event, including its job dependencies and exception diagnostics.
        /// The source metadata must remain stable during capture.
        /// </summary>
        public JobEventSnapshot Capture(IJobEvent source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var status = source.Status;
            var timestamp = source.Timestamp;
            var retryCount = source.RetryCount;
            var jobInfo = source.JobInfo;
            var exception = source.Exception;
            var executionChannel = CaptureExecutionChannel(source);

            if (jobInfo == null)
                throw new ArgumentException("JobInfo cannot be null.", nameof(source));

            var capturedJobInfo = CaptureJobInfo(jobInfo);
            var capturedDiagnostic = exception == null
                ? JobExceptionDiagnostic.None
                : CaptureExceptionDiagnostic(exception);
            
            return new JobEventSnapshot(
                status,
                timestamp,
                retryCount,
                executionChannel,
                capturedJobInfo,
                capturedDiagnostic);
        }

        /// <summary>Reads the optional queue execution channel.</summary>
        private static int? CaptureExecutionChannel(IJobEvent source)
        {
            var executionEvent = source as IJobExecutionEvent;
            return executionEvent == null
                ? (int?)null
                : executionEvent.ExecutionChannel;
        }

        /// <summary>Starts a job graph copy with per-capture traversal state.</summary>
        private static JobInfoSnapshot CaptureJobInfo(IJobInfo source)
        {
            var completed = new Dictionary<IJobInfo, JobInfoSnapshot>(
                ReferenceIdentityComparer<IJobInfo>.Instance);
            var visiting = new HashSet<IJobInfo>(
                ReferenceIdentityComparer<IJobInfo>.Instance);

            return CopyJobInfoRecursive(source, completed, visiting);
        }

        /// <summary>Copies one job node, detecting cycles and reusing completed nodes.</summary>
        private static JobInfoSnapshot CopyJobInfoRecursive(
            IJobInfo source,
            Dictionary<IJobInfo, JobInfoSnapshot> completed,
            HashSet<IJobInfo> visiting)
        {
            if (visiting.Contains(source))
                throw new InvalidOperationException(
                    $"Cyclic IJobInfo dependency graph at JobId '{source.Id}'.");

            JobInfoSnapshot existing;
            if (completed.TryGetValue(source, out existing))
                return existing;

            visiting.Add(source);
            try
            {
                var id = source.Id;
                var name = source.Name;
                var isCompleted = source.IsCompleted;
                var executionStart = source.ExecutionStart;
                var executionTime = source.ExecutionTime;
                var executionEnd = source.ExecutionEnd;
                var status = source.Status;
                var crossQueueId = source.CrossQueueId;
                var sourceDependencies = source.Dependencies;

                var dependencies = CopyDependencies(
                    sourceDependencies, id, completed, visiting);

                var snapshot = new JobInfoSnapshot(
                    id,
                    name,
                    isCompleted,
                    executionStart,
                    executionTime,
                    executionEnd,
                    status,
                    crossQueueId,
                    dependencies);

                completed.Add(source, snapshot);
                return snapshot;
            }
            finally
            {
                visiting.Remove(source);
            }
        }

        /// <summary>Validates and copies the dependencies of one job node.</summary>
        private static List<JobInfoSnapshot> CopyDependencies(
            IEnumerable<IJobInfo> sourceDependencies,
            Guid ownerId,
            Dictionary<IJobInfo, JobInfoSnapshot> completed,
            HashSet<IJobInfo> visiting)
        {
            if (sourceDependencies == null)
                throw new InvalidOperationException(
                    $"IJobInfo.Dependencies cannot be null (JobId '{ownerId}').");

            var copies = new List<JobInfoSnapshot>();
            foreach (var child in sourceDependencies)
            {
                if (child == null)
                    throw new InvalidOperationException(
                        $"IJobInfo contains a null dependency (JobId '{ownerId}').");

                copies.Add(CopyJobInfoRecursive(child, completed, visiting));
            }

            return copies;
        }

        /// <summary>Starts an exception diagnostic copy or returns the no-error DTO.</summary>
        private static JobExceptionDiagnostic CaptureExceptionDiagnostic(Exception source)
        {
            if (source == null)
                return JobExceptionDiagnostic.None;

            var completed = new Dictionary<Exception, JobExceptionDiagnostic>(
                ReferenceIdentityComparer<Exception>.Instance);
            var visiting = new HashSet<Exception>(
                ReferenceIdentityComparer<Exception>.Instance);

            return CopyExceptionDiagnosticRecursive(source, completed, visiting);
        }

        /// <summary>Copies one exception into scalar diagnostics and nested diagnostics.</summary>
        private static JobExceptionDiagnostic CopyExceptionDiagnosticRecursive(
            Exception source,
            Dictionary<Exception, JobExceptionDiagnostic> completed,
            HashSet<Exception> visiting)
        {
            if (visiting.Contains(source))
                throw new InvalidOperationException("Cyclic exception graph.");

            JobExceptionDiagnostic existing;
            if (completed.TryGetValue(source, out existing))
                return existing;

            visiting.Add(source);
            try
            {
                var type = source.GetType();
                var typeName = type.FullName ?? type.Name;
                var message = source.Message ?? string.Empty;
                var stackTrace = source.StackTrace ?? string.Empty;
                var origin = source.Source ?? string.Empty;
                var helpLink = source.HelpLink ?? string.Empty;
                var hResult = source.HResult;

                var innerCopies = CopyInnerExceptions(source, completed, visiting);

                var snapshot = new JobExceptionDiagnostic(
                    true,
                    typeName,
                    message,
                    stackTrace,
                    origin,
                    helpLink,
                    hResult,
                    innerCopies);

                completed.Add(source, snapshot);
                return snapshot;
            }
            finally
            {
                visiting.Remove(source);
            }
        }

        /// <summary>Copies all inner exceptions, including AggregateException children.</summary>
        private static List<JobExceptionDiagnostic> CopyInnerExceptions(
            Exception source,
            Dictionary<Exception, JobExceptionDiagnostic> completed,
            HashSet<Exception> visiting)
        {
            var copies = new List<JobExceptionDiagnostic>();
            var aggregate = source as AggregateException;

            if (aggregate != null)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    if (inner == null)
                        throw new InvalidOperationException(
                            "AggregateException contains a null inner exception.");

                    copies.Add(CopyExceptionDiagnosticRecursive(inner, completed, visiting));
                }
            }
            else
            {
                var inner = source.InnerException;
                if (inner != null)
                    copies.Add(CopyExceptionDiagnosticRecursive(inner, completed, visiting));
            }

            return copies;
        }

        /// <summary>Compares source graph nodes by reference identity.</summary>
        private sealed class ReferenceIdentityComparer<T> : IEqualityComparer<T>
            where T : class
        {
            internal static readonly ReferenceIdentityComparer<T> Instance =
                new ReferenceIdentityComparer<T>();

            private ReferenceIdentityComparer() { }

            public bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(T value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }
    }
}
