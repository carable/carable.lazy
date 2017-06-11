//
// LazyTest.cs - NUnit Test Cases for Lazy
//
// Author:
//	Zoltan Varga (vargaz@gmail.com)
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Threading;
using Xunit;
using Carable.Lazy;

namespace Carable.Lazy.Tests
{

    public class LazyExpiryTest
    {
        private static readonly DateTime future = DateTime.UtcNow.AddDays(1);
        [Fact]
        public void Ctor_Null_1()
        {
            Assert.Throws<ArgumentNullException>(() => new LazyExpiry<int>(null));
        }
        [Fact]
        public void NotThreadSafe()
        {
            var l2 = new LazyExpiry<int>(delegate () { return Tuple.Create(42, future); });

            Assert.Equal(42, l2.Value);
        }

        static int counter;

        /// <summary>
        /// This also tests that non expired values are returned
        /// </summary>
        [Fact]
        public void EnsureSingleThreadSafeExecution()
        {
            counter = 42;
            bool started = false;
            var l = new LazyExpiry<int>(delegate () { return Tuple.Create(counter++, future); }, true);
            bool failed = false;
            object monitor = new object();
            var threads = new Thread[4];
            for (int i = 0; i < threads.Length; ++i)
            {
                threads[i] = new Thread(delegate ()
                {
                    lock (monitor)
                    {
                        if (!started)
                        {
                            if (!Monitor.Wait(monitor, 2000))
                                failed = true;
                        }
                    }
                    int val = l.Value;
                });
            }
            for (int i = 0; i < threads.Length; ++i)
                threads[i].Start();
            lock (monitor)
            {
                started = true;
                Monitor.PulseAll(monitor);
            }

            for (int i = 0; i < threads.Length; ++i)
                threads[i].Join();

            Assert.False(failed);
            Assert.Equal(42, l.Value);
        }

        [Fact]
        public void ModeNone()
        {
            int x;
            bool fail = true;
            LazyExpiry<int> lz = new LazyExpiry<int>(() => { if (fail) throw new TestException(); else return Tuple.Create(99, future); }, LazyThreadSafetyMode.None);
            try
            {
                x = lz.Value;
                throw new FailException("#1");
            }
            catch (TestException) { }

            try
            {
                x = lz.Value;
                throw new FailException("#2");
            }
            catch (TestException) { }

            fail = false;
            x = lz.Value;
            // since lazy now can get a value, it should not fail
        }

        [Fact]
        public void ModePublicationOnly()
        {
            bool fail = true;
            int invoke = 0;
            LazyExpiry<int> lz = new LazyExpiry<int>(() => { ++invoke; if (fail) throw new TestException(); else return Tuple.Create(99, future); }, LazyThreadSafetyMode.PublicationOnly);

            try
            {
                int x = lz.Value;
                throw new FailException("#1");
            }
            catch (TestException) { }

            try
            {
                int x = lz.Value;
                throw new FailException("#2");
            }
            catch (TestException) { }


            Assert.True(2== invoke, "#3");
            fail = false;
            Assert.True(99== lz.Value, "#4");
            Assert.True(3== invoke, "#5");

            invoke = 0;
            bool rec = true;
            lz = new LazyExpiry<int>(() => { ++invoke; bool r = rec; rec = false; return Tuple.Create(r ? lz.Value : 88, future); }, LazyThreadSafetyMode.PublicationOnly);

            Assert.True(88== lz.Value, "#6");
            Assert.True(2== invoke, "#7");
        }

        [Fact]
        public void ModeExecutionAndPublication()
        {
            int invoke = 0;
            bool fail = true;
            LazyExpiry<int> lz = new LazyExpiry<int>(() => { ++invoke; if (fail) throw new TestException(); else return Tuple.Create(99, future); }, LazyThreadSafetyMode.ExecutionAndPublication);

            try
            {
                int x = lz.Value;
                throw new FailException("#1");
            }
            catch (TestException) { }
            Assert.True(1== invoke, "#2");

            try
            {
                int x = lz.Value;
                throw new FailException("#3");
            }
            catch (TestException) { }
            Assert.True(2== invoke, "#4");

            fail = false;
            { int x = lz.Value; }
            Assert.True(3== invoke, "#6"); // since two initializations failed, we get the value once it has not
        }

        static Tuple<int, DateTime> Return22()
        {
            return Tuple.Create(22, future);
        }

        [Fact]
        public void Trivial_Lazy()
        {
            var x = new LazyExpiry<int>(Return22, false);
            Assert.True(22== x.Value, "#1");
        }

        [Fact]
        public void Expired_lazy()
        {
            counter = 42;
            var x = new LazyExpiry<int>(() => Tuple.Create(counter++, DateTime.Now.AddDays(-1)), false);
            Assert.True(42== x.Value, "#1");
            Assert.True(43== x.Value, "#2");
            Assert.True(44== x.Value, "#3");
        }
    }
}
