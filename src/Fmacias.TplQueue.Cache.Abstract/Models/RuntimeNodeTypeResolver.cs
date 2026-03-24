using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using System;

namespace Fmacias.TplQueue.Cache.Abstract.Models
{
    /// <summary>
    /// Default CLR type resolver for cached payload nodes.
    /// <para>
    /// For hardened/production scenarios, replace this implementation with a whitelist-based resolver
    /// (e.g., only allow types from specific assemblies/namespaces).
    /// </para>
    /// </summary>
    internal sealed class RuntimeNodeTypeResolver : IRuntimeNodeTypeResolver
    {
        private readonly AppDomain _appDomain;
        private RuntimeNodeTypeResolver(AppDomain? appDomain = null)
        {
            _appDomain = appDomain ?? AppDomain.CurrentDomain;
        }

        public AppDomain AppDomain => _appDomain;

        public static RuntimeNodeTypeResolver Create(AppDomain? appDomain = null)
        {
            return new RuntimeNodeTypeResolver(appDomain);
        }
        public Type Resolve(string payloadTypeName)
        {
            if (string.IsNullOrWhiteSpace(payloadTypeName))
                throw new ArgumentException("Payload type name cannot be null or whitespace.", nameof(payloadTypeName));

            if (!TypeDeserializer.TryResolveType(payloadTypeName, out var type, _appDomain))
                throw new InvalidOperationException($"Cannot resolve payload CLR type '{payloadTypeName}'.");

            return type;
        }
    }
}
