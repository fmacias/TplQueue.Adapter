using System;
using System.Security.Cryptography;

namespace Fmacias.TplQueue.RetryPolicies
{
    /// <summary>
    /// Crypto-based jitter helpers for delay randomization.
    /// </summary>
    internal sealed class JitterUtil : IDisposable
    {
        private readonly RandomNumberGenerator _randomNumberGenerator;
        private bool _disposed;

        private JitterUtil()
        {
            _randomNumberGenerator = RandomNumberGenerator.Create();
        }

        public static JitterUtil Create()
        {
            return new JitterUtil();
        }

        /// <summary>
        /// Returns baseMs adjusted by +/- percent; clamped to at least 1ms.
        /// Example: baseMs=1000, percent=0.10 => range roughly [900..1100].
        /// </summary>
        public int JitterMs(int baseMs, double percent)
        {
            ThrowIfDisposed();

            if (baseMs < 1) baseMs = 1;
            if (percent <= 0 || double.IsNaN(percent) || double.IsInfinity(percent)) return baseMs;

            double centered = NextRandomDouble() - 0.5; // [-0.5, +0.5)
            double factor = 1.0 + centered * (percent * 2); // [1-p, 1+p)
            int next = (int)Math.Round(baseMs * factor);

            return next < 1 ? 1 : next;
        }

        /// <summary>
        /// NextDouble() in [0,1) using 53 bits of randomness from a crypto RNG.
        /// </summary>
        public double NextRandomDouble()
        {
            ThrowIfDisposed();

            var bytes = new byte[8];
           _randomNumberGenerator.GetBytes(bytes);

            ulong ul = BitConverter.ToUInt64(bytes, 0) >> 11; // keep top 53 bits
            return ul / (double)(1UL << 53);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _randomNumberGenerator.Dispose();
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(JitterUtil));
            }
        }
    }
}
