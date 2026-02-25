using Fmacias.TplQueue.Contracts;
using System;
using System.Threading;

namespace Fmacias.TplQueue.Jobs
{
    internal class DataJobRoot<TPayload> :
        DataJob<TPayload>,
        IJobAdapter,
        IDataJobRoot<TPayload>
        where TPayload : IPayload

    {
        private readonly IJobRoot _jobRoot;
        private DataJobRoot(IJobRoot jobRoot, TPayload payload)
            : base(jobRoot, payload)
        {
            _jobRoot = jobRoot;
        }
        public static DataJobRoot<TPayload> CreateRoot(IJobRoot job, TPayload payload)
        {
            return new DataJobRoot<TPayload>(job, payload);
        }
        public IQ Enqueue(IQ jobQ, CancellationToken ct)
        {
            if (jobQ == null) throw new ArgumentNullException(nameof(jobQ));
            return _jobRoot.Enqueue(jobQ, ct);
        }
    }
}
