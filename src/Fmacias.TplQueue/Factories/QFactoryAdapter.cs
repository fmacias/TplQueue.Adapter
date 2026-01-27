using Fmacias.TplQueue.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fmacias.TplQueue.Factories
{
    internal class QFactoryAdapter : IQFactoryAdapter
    {
        private readonly IQFactoryCore _innerFactory;
        private readonly IReadOnlyDictionary<string, IQOptions> _queueOptionsByName;
        private readonly IRetryPolicyFactory _retryPolicyFactory;

        private QFactoryAdapter(
            IQFactoryCore factory,
            IReadOnlyDictionary<string, IQOptions> options,
            IRetryPolicyFactory retryPolicyFactory)
        {
            _innerFactory = factory
                ?? throw new ArgumentNullException(nameof(factory));
            _queueOptionsByName = options 
                ?? throw new ArgumentNullException(nameof(options));
            _retryPolicyFactory = retryPolicyFactory
                ?? throw new ArgumentNullException(nameof(retryPolicyFactory));
        }
        public static QFactoryAdapter Create(
            IQFactoryCore factory,
            IReadOnlyDictionary<string, IQOptions> queueOptions,
            IRetryPolicyFactory retryPolicyFactory)
        {
            return new QFactoryAdapter(factory, queueOptions, retryPolicyFactory);
        }

        public IParallelQ CreateParallel(string name, 
            int maxParallelism, 
            ILogger logger, 
            Func<IRetryPolicy>? retryPolicyCreator = null)
        {
            return _innerFactory.CreateParallel(name, maxParallelism, logger, retryPolicyCreator);
        }
        /// <inheritdoc />
        public IParallelQ CreateParallel(
            IQOptions queueOptions,
            string name,
            ILogger logger)
        {
            if (queueOptions == null) throw new ArgumentNullException(nameof(queueOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            ValidateQueueOptions(queueOptions);

            IRetryPolicy retryPolicyCreator() => _retryPolicyFactory.Create(queueOptions.RetryPolicy);
            return CreateParallel(name, queueOptions.MaxParallelism, logger, retryPolicyCreator);
        }
        /// <inheritdoc />
        public IParallelQ CreateParallel(
            string name,
            ILogger logger)
        {
            IQOptions queueOptions = GetQueueFromOptions(name);
            return CreateParallel(queueOptions, name, logger);
        }
        public IFifoQ CreateFifo(string name, ILogger logger, Func<IRetryPolicy>? retryPolicy = null)
        {
            return _innerFactory.CreateFifo(name, logger, retryPolicy);
        }

        /// <inheritdoc />
        public IFifoQ CreateFifo(
            IQOptions queueOptions,
            string name,
            ILogger logger)
        {
            if (queueOptions == null) throw new ArgumentNullException(nameof(queueOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            ValidateQueueOptions(queueOptions);

            var retryPolicyCreator = () => _retryPolicyFactory.Create(queueOptions.RetryPolicy);
            return CreateFifo(name, logger, retryPolicyCreator);
        }
        public IFifoQ CreateFifo(
            string name,
            ILogger logger)
        {
            IQOptions queueOptions = GetQueueFromOptions(name);
            return CreateFifo(queueOptions, name, logger);
        }
        /// <inheritdoc />
        public T GetCoreQ<T>(string name, ILogger<T> logger) where T : class, IJobQ
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Queue name cannot be null or whitespace.", nameof(name));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            var queue = CreateCoreQueue<T>(name, logger);
            return CastDispatcher<T>(queue, name);
        }

        private IQOptions GetQueueFromOptions(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Queue name cannot be null/empty.", nameof(name));

            if (!_queueOptionsByName.TryGetValue(name, out var queueOptions))
                throw new KeyNotFoundException($"Queue '{name}' not found.");

            ValidateQueueOptions(queueOptions);

            return queueOptions;
        }

        private static void ValidateQueueOptions(IQOptions queueOptions)
        {
            if (queueOptions.MaxParallelism < 1)
                throw new ArgumentException("MaxParallelism must be >= 1.");

            if (string.IsNullOrWhiteSpace(queueOptions.RetryPolicy))
                throw new ArgumentException("RetryPolicy key is required.");
        }
        private IJobQ CreateCoreQueue<T>(string name, ILogger<T> logger)
            where T : class, IJobQ
        {
            var queueType = typeof(T);
            if (queueType == typeof(IFifoQ))
            {
                return CreateFifo(name, logger);
            }
            return CreateParallel(name, logger);
        }
        private static T CastDispatcher<T>(IJobQ queue, string name)
            where T : class, IJobQ
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
