using Fmacias.TplQueue.Contracts;
using Fmacias.TplQueue.Defaults;
using System;

namespace Fmacias.TplQueue
{
    /// <summary>
    /// Default facade-owned CLR type resolver for cache hydration.
    /// </summary>
    internal sealed class DefaultTypeResolver : ITypeResolver
    {
        private readonly AppDomain _appDomain;

        private DefaultTypeResolver(AppDomain? appDomain = null)
        {
            _appDomain = appDomain ?? AppDomain.CurrentDomain;
        }

        public static DefaultTypeResolver Create(AppDomain? appDomain = null)
        {
            return new DefaultTypeResolver(appDomain);
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
