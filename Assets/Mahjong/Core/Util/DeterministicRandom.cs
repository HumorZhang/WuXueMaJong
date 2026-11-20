using System;
using System.Collections.Generic;

namespace WuXueMaJong.Core
{
    /// <summary>可复现的轻量随机数（xorshift32），用于洗牌与 AI 平局决策。</summary>
    public sealed class DeterministicRandom
    {
        private uint _state;

        public DeterministicRandom(long seed)
        {
            _state = unchecked((uint)seed);
            if (_state == 0)
            {
                _state = 0x9E3779B9u;
            }

            for (int i = 0; i < 4; i++)
            {
                NextUInt();
            }
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            return (int)(NextUInt() % (uint)maxExclusive);
        }

        public double NextDouble() => NextUInt() / (double)uint.MaxValue;

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
