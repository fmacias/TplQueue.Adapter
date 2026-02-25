using System;
using Fmacias.TplQueue.Cache.Contracts;
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
    internal sealed class RuntimeNodeTypeResolver : IRuntimeNodeTypeResolver
    {
        private RuntimeNodeTypeResolver() { }
        public static RuntimeNodeTypeResolver Create()
        {
            return new RuntimeNodeTypeResolver();
        }
        public Type Resolve(string payloadTypeName)
        {
            if (string.IsNullOrWhiteSpace(payloadTypeName))
                throw new ArgumentException("Payload type name cannot be null or whitespace.", nameof(payloadTypeName));

            var resolved = Type.GetType(payloadTypeName);
            if (resolved == null)
                throw new InvalidOperationException($"Cannot resolve payload CLR type '{payloadTypeName}'.");

            return resolved;
        }
    }
}
