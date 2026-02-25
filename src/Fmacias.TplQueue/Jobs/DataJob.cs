using Fmacias.TplQueue.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Jobs
{
    internal class DataJob<TPayload> :
        IJobAdapter,
        IDataJob<TPayload>
        where TPayload : IPayload
    {
        protected readonly IJob InnerJob;
        protected readonly TPayload PayloadDto;
        
        protected DataJob(IJob job, TPayload payload)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            InnerJob = job;
            PayloadDto = payload;
        }
        public static DataJob<TPayload> Create(IJob job, TPayload payload)
        {
            return new DataJob<TPayload>(job, payload);
        }
        public Guid Id => InnerJob.Id;

        public string Name => InnerJob.Name;

        public bool IsCompleted => InnerJob.IsCompleted;

        public DateTime ExecutionStart => InnerJob.ExecutionStart;

        public TimeSpan ExecutionTime => InnerJob.ExecutionTime;

        public DateTime ExecutionEnd => InnerJob.ExecutionEnd;

        public TaskStatus Status => InnerJob.Status;

        public IReadOnlyCollection<IJobInfo> Dependencies => InnerJob.Dependencies;

        public TPayload Payload => PayloadDto;

        /// <summary>
        /// Returns the runtime payload CLR type.
        /// Runtime type must be preserved so cached nodes can be rehydrated with the
        /// concrete payload type instead of an interface/base contract.
        /// </summary>
        public Type PayloadType => PayloadDto?.GetType()
                                   ?? throw new InvalidOperationException("Payload is null.");

        public IJob After(params IJob[] previousTasks)
        {
            return InnerJob.After(previousTasks);
        }

        public IJobInfo CopyInfo()
        {
            return DataJobInfoSnapshot.Create(InnerJob.CopyInfo(), Payload);
        }

        public IJob GetInnerJob()
        {
            return InnerJob;
        }

        public IJobInfo[] GetJobInfoDependencies()
        {
            return InnerJob.GetJobInfoDependencies();
        }

        public IJob[] GetJobsBatch()
        {
            return InnerJob.GetJobsBatch();
        }

        public object GetPayload()
        {
            return Payload;
        }

        public IReadOnlyList<IDataJob> GetDependentDataJobs()
        {
            return [..InnerJob.Dependencies.OfType<IDataJob>()] ;
        }

        public Func<IRetryPolicy> GetRetryPolicyFactory()
        {
            return InnerJob.GetRetryPolicyFactory();
        }

        public void SetRoot(IJobRoot jobRoot)
        {
            InnerJob.SetRoot(jobRoot);
        }
        public async Task WaitUntilFinishedAsync()
        {
            await InnerJob.WaitUntilFinishedAsync().ConfigureAwait(false);
        }
        public string Serialize(IUniversalDataSerializer serializer)
        {
            return serializer.Serialize(Payload, Payload.GetType());
        }
    }
}
