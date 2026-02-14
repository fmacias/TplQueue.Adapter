using Fmacias.TplQueue.Contracts;
using System;
using System.Threading;

namespace Fmacias.TplQueue.Jobs
{
    internal class PayloadJobRoot<TPayload> :
        PayloadJob<TPayload>,
        IJobAdapter,
        IPayloadJobRoot<TPayload>
        where TPayload : IPayload

    {
        private readonly IJobRoot _jobRoot;
        private PayloadJobRoot(IJobRoot jobRoot, TPayload payload)
            : base(jobRoot, payload)
        {
            _jobRoot = jobRoot;
        }
        public static PayloadJobRoot<TPayload> CreateRoot(IJobRoot job, TPayload payload)
        {
            return new PayloadJobRoot<TPayload>(job, payload);
        }
        public IJobQ Enqueue(IJobQ jobQ, CancellationToken ct)
        {
            if (jobQ == null) throw new ArgumentNullException(nameof(jobQ));
            return _jobRoot.Enqueue(jobQ, ct);
        }
    }
}
