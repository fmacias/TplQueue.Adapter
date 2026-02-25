using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Jobs;
using System;

namespace Fmacias.TplQueue.Factories
{
    internal class PayloadJobFactory1<TPayload> : IPayloadJobFactory1<TPayload>
        where TPayload : IPayload
    {
        private readonly IJobFactory _jobCoreFactory;
        private readonly IJobRootFactory _jobCoreRootFactory;
        private PayloadJobFactory1(IJobFactory jobCoreFactory, IJobRootFactory jobCoreRootFactory)
        {
            _jobCoreFactory = jobCoreFactory;
            _jobCoreRootFactory = jobCoreRootFactory;
        }
        public static PayloadJobFactory1<TPayload> Create(IJobFactory jobCoreFactory, IJobRootFactory jobCoreRootFactory)
        {
            return new PayloadJobFactory1<TPayload>(jobCoreFactory, jobCoreRootFactory);
        }
        public IDataJob<TPayload> CreateJob(TPayload payload, IJob job)
        {
            return DataJob<TPayload>.Create(job, payload);
        }

        public IDataJob<TPayload> CreateJob(Guid id, TPayload payload, string name = "")
        {
            throw new NotImplementedException();
        }

        public IDataJob CreateJob(IJobNodeDto dto, TPayload payload)
        {
            throw new NotImplementedException();
        }

        public IDataJobRoot<TPayload> CreateJobRoot(TPayload payload, string name = "")
        {
            throw new NotImplementedException();
        }

        public IDataJobRoot<TPayload> CreateJobRoot(TPayload payload, Func<IRetryPolicy> policy, string name = "")
        {
            throw new NotImplementedException();
        }

        public IDataJobRoot<TPayload> CreateJobRoot(TPayload payload, IRetryPolicyDescriptor retryPolicyDescriptor, string name = "")
        {
            throw new NotImplementedException();
        }

        public IDataJobRoot<TPayload> CreateJobRoot(Guid id, TPayload payload, string name = "")
        {
            throw new NotImplementedException();
        }

        public IDataJobRoot<TPayload> CreateJobRoot(Guid id, TPayload payload, IRetryPolicyDescriptor retryPolicyDescriptor, string name = "")
        {
            throw new NotImplementedException();
        }

        public IDataJobRoot CreateJobRoot(IJobNodeDto dto, IPayload payload)
        {
            throw new NotImplementedException();
        }

        public IDataJobRoot<TPayload> CreateJobRoot(Guid jobId, TPayload payload, Func<IRetryPolicy> policy, string name = "")
        {
            throw new NotImplementedException();
        }
    }
}
