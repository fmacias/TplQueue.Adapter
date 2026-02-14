using System;
using Fmacias.TplQueue.Contracts;

namespace Fmacias.TplQueue.Cache.DomainModels
{
    /// <summary>
    /// Default CLR type resolver for cached payload nodes.
    /// <para>
    /// It resolves types via <see cref="Type.GetType(string,bool)"/> using the persisted
    /// <see cref="IJobNodeDto.PayloadTypeName"/> (typically AssemblyQualifiedName).
    /// </para>
    /// <para>
    /// For hardened/production scenarios, replace this implementation with a whitelist-based resolver
    /// (e.g., only allow types from specific assemblies/namespaces).
    /// </para>
    /// </summary>
    internal sealed class DefaultNodeTypeResolver : INodeTypeResolver
    {
        private DefaultNodeTypeResolver() { }

        public static DefaultNodeTypeResolver Create()
        {
            return new DefaultNodeTypeResolver();
        }

        /// <inheritdoc />
        public Type Resolve(string payloadTypeName)
        {
            if (string.IsNullOrWhiteSpace(payloadTypeName))
                throw new ArgumentException("Payload type name cannot be null/empty.", nameof(payloadTypeName));

            var type = Type.GetType(payloadTypeName, throwOnError: false);
            if (type is null)
                throw new InvalidOperationException($"Cannot resolve CLR type from '{payloadTypeName}'.");

            return type;
        }
    }
}
