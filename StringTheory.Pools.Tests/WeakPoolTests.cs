using System;
using System.Collections.Generic;
using Xunit;

namespace StringTheory.Pools.Tests
{
    public sealed class WeakPoolTests
    {
        [Fact]
        public void InternReturnsPooledInstance()
        {
            var pool = new WeakPool<string>(StringComparer.Ordinal);

            var canon = new string('5', 5);

            Assert.Same(canon, pool.Intern(canon));
            Assert.Same(canon, pool.Intern(new string('5', 5)));

            Assert.Equal(1, pool.Count);
        }

        [Fact]
        public void InternAfterGCReturnsNewInstance()
        {
            var pool = new WeakPool<string>(StringComparer.Ordinal);

            ScopedIntern();

            GC.Collect();

            Assert.Equal(1, pool.Count);

            var newValue = new string('5', 5);
            Assert.Same(newValue, pool.Intern(newValue));

            Assert.Equal(1, pool.Count);

            void ScopedIntern() => pool.Intern(new string('5', 5));
        }

        [Fact]
        public void InternMultipleToSameBucket()
        {
            var pool = new WeakPool<string>(new CollisionComparer<string>());

            var s1 = new string('1', 1);
            var s2 = new string('2', 2);
            var s3 = new string('3', 3);

            pool.Intern(s1);
            pool.Intern(s2);
            pool.Intern(s3);

            Assert.Equal(3, pool.Count);

            Assert.Same(s1, pool.Intern(new string('1', 1)));
            Assert.Same(s2, pool.Intern(new string('2', 2)));
            Assert.Same(s3, pool.Intern(new string('3', 3)));

            Assert.Equal(3, pool.Count);
        }

        [Fact]
        public void InternMultipleToSameBucketWorksAfterGC()
        {
            var pool = new WeakPool<string>(new CollisionComparer<string>());

            ScopedIntern(1);
            ScopedIntern(2);
            ScopedIntern(3);

            Assert.Equal(3, pool.Count);

            GC.Collect();

            Assert.Equal(3, pool.Count);
            var s1 = Create(1);
            Assert.Same(s1, pool.Intern(s1));
            Assert.Equal(1, pool.Count);

            string Create(int i) => new string((char)('0' + i), i);
            void ScopedIntern(int i) => pool.Intern(Create(i));
        }

        [Fact]
        public void InternViaReadOnlySpanOfCharacters()
        {
            var pool = new WeakStringPool();
            var s = new string('9', 9);
            var out1 = pool.Intern(s);
            var out2 = pool.InternSpan(s.AsSpan());

            Assert.Same(s, out1);
            Assert.Same(s, out2);

            var q = new string('0', 50);

            Assert.Same(
                pool.InternSpan(q.AsSpan(0, 10)),
                pool.InternSpan(q.AsSpan(10, 10)));
        }

        private sealed class CollisionComparer<T> : IEqualityComparer<T> where T : class
        {
            public bool Equals(T x, T y) => object.Equals(x, y);
            public int GetHashCode(T obj) => 123;
        }
    }
}
