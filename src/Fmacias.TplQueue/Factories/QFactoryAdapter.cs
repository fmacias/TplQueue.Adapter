using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fmacias.TplQueue.Factories
{
    internal class QFactoryAdapter : IQFactoryAdapter
    {
        private readonly IQFactory _innerFactory;
        private readonly IReadOnlyDictionary<string, IQOptions> _queueOptionsByName;
        private readonly IReadOnlyDictionary<string, IRetryPolicyOptions> _retryPolicyOptions;
        private readonly IRetryPolicyAbstractFactory _retryPolicyFactory;

        private QFactoryAdapter(
            IQFactory factory,
            IRetryPolicyAbstractFactory retryPolicyFactory,
            IReadOnlyDictionary<string, IQOptions> options,
            IReadOnlyDictionary<string, IRetryPolicyOptions> retryPolicyOptions)
        {
            _innerFactory = factory
                ?? throw new ArgumentNullException(nameof(factory));
            _queueOptionsByName = options 
                ?? throw new ArgumentNullException(nameof(options));
            _retryPolicyFactory = retryPolicyFactory
                ?? throw new ArgumentNullException(nameof(retryPolicyFactory));
            _retryPolicyOptions = retryPolicyOptions
                ?? throw new ArgumentNullException(nameof(retryPolicyOptions));
        }
        public static QFactoryAdapter Create(
            IQFactory factory,
            IRetryPolicyAbstractFactory retryPolicyFactory,
            IReadOnlyDictionary<string, IQOptions> queueOptions, 
            IReadOnlyDictionary<string, IRetryPolicyOptions> retryPolicyOptions)
        {
            return new QFactoryAdapter(factory, retryPolicyFactory, queueOptions, retryPolicyOptions);
        }

        public IParallelQ Parallel(Guid id,
            string name,
            int maxParallelism,
            ILogger logger, Func<IRetryPolicy>? retryPolicyCreator = null)
        {
            return _innerFactory.Parallel(id, name, maxParallelism, logger, retryPolicyCreator);
        }
        /// <inheritdoc />
        public IParallelQ Parallel(
            IQOptions queueOptions,
            string name,
            ILogger logger)
        {
            if (queueOptions == null) throw new ArgumentNullException(nameof(queueOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            ValidateQueueOptions(queueOptions);

            Func<IRetryPolicy> retryPolicyCreator = () 
                => _retryPolicyFactory.PolicyByName(queueOptions.RetryPolicy, _retryPolicyOptions);

            return Parallel(queueOptions.Id, name, queueOptions.MaxParallelism, logger, retryPolicyCreator);
        }
        /// <inheritdoc />
        public IParallelQ Parallel(
            string name,
            ILogger logger)
        {
            if (TryGetQueueFromOptions(name, out var queueOptions))
            {
                return Parallel(queueOptions, name, logger);
            }

            return _innerFactory.Parallel(Guid.NewGuid(), name, Environment.ProcessorCount, logger);
        }
        public IFifoQ Fifo(Guid id, string name, ILogger logger, Func<IRetryPolicy>? retryPolicy = null)
        {
            return _innerFactory.Fifo(id, name, logger, retryPolicy);
        }

        /// <inheritdoc />
        public IFifoQ Fifo(
            IQOptions queueOptions,
            string name,
            ILogger logger)
        {
            if (queueOptions == null) throw new ArgumentNullException(nameof(queueOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            ValidateQueueOptions(queueOptions);

            Func<IRetryPolicy> retryPolicyCreator = () 
                => _retryPolicyFactory.PolicyByName(queueOptions.RetryPolicy, _retryPolicyOptions);
            
            return Fifo(queueOptions.Id, name, logger, retryPolicyCreator);
        }
        public IFifoQ Fifo(
            string name,
            ILogger logger)
        {
            if (TryGetQueueFromOptions(name, out var queueOptions))
            {
                return Fifo(queueOptions, name, logger);
            }
            Func<IRetryPolicy> retryPolicyCreator = ()
                => _retryPolicyFactory.PolicyByName(queueOptions.RetryPolicy, _retryPolicyOptions);

            return _innerFactory.Fifo(queueOptions.Id, name, logger, retryPolicyCreator);
        }
        /// <inheritdoc />
        public T GetCoreQ<T>(string name, ILogger<T> logger) where T : IQ
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Queue name cannot be null or whitespace.", nameof(name));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            var queue = CreateCoreQueue<T>(name, logger);
            return CastQ<T>(queue, name);
        }
        public ICacheQ CacheQ(ILogger<ICacheQ> logger, IDataJobCache payloadLeaseCache, IParallelQ queue)
        {
            return _innerFactory.CacheQ(logger, payloadLeaseCache, queue); 
        }

        private bool TryGetQueueFromOptions(string name, out IQOptions queueOptions)
        {
            queueOptions = null!;

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Queue name cannot be null/empty.", nameof(name));

            if (!_queueOptionsByName.TryGetValue(name, out queueOptions))
                return false;

            ValidateQueueOptions(queueOptions);

            return true;
        }

        private static void ValidateQueueOptions(IQOptions queueOptions)
        {
            if (queueOptions.MaxParallelism < 1)
                throw new ArgumentException("MaxParallelism must be >= 1.");

            if (string.IsNullOrWhiteSpace(queueOptions.RetryPolicy))
                throw new ArgumentException("RetryPolicy name is required.");
        }
        private IQ CreateCoreQueue<T>(string name, ILogger<T> logger)
            where T : IQ
        {
            var queueType = typeof(T);
            if (queueType == typeof(IFifoQ))
            {
                return Fifo(name, logger);
            }
            return Parallel(name, logger);
        }
        private static T CastQ<T>(IQ queue, string name)
            where T : IQ
        {
            if (queue is T typed)
                return typed;

            throw new InvalidCastException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Queue '{0}' is not recognized as a Core Queue '{1}'. Actual type: '{2}'.",
                    name,
                    typeof(T).FullName,
                    queue.GetType().FullName));
        }
    }
}
