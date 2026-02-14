using Fmacias.TplQueue.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fmacias.TplQueue.Jobs
{
    internal class PayloadJobInfoSnapshot : IPayloadJobInfo
    {
        private readonly IJobInfo _jobInfo;
        private readonly IPayload _payload;
        private PayloadJobInfoSnapshot(IJobInfo jobInf, IPayload payload)
        {
            _jobInfo = jobInf ?? throw new ArgumentNullException(nameof(jobInf));
            _payload = payload ?? throw new ArgumentNullException(nameof(payload));
        }
        public static IPayloadJobInfo Create(IJobInfo jobInfo, IPayload payload)
        {
            return new PayloadJobInfoSnapshot(jobInfo, payload);
        }

        public string Serialize(IUniversalPayloadSerializer serializer)
        {
            return serializer.Serialize(_payload, _payload.GetType());
        }

        public Guid Id => _jobInfo.Id;

        public string Name => _jobInfo.Name;

        public bool IsCompleted => _jobInfo.IsCompleted;

        public DateTime ExecutionStart => _jobInfo.ExecutionStart;

        public TimeSpan ExecutionTime => _jobInfo.ExecutionTime;

        public DateTime ExecutionEnd => _jobInfo.ExecutionEnd;

        public TaskStatus Status => _jobInfo.Status;

        public IReadOnlyCollection<IJobInfo> Dependencies => _jobInfo.Dependencies;

        public IPayload Payload => _payload;
    }
}
