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
using NUnit.Framework;
using Carable.Lazy;

namespace Carable.LazyExpiry.Tests
{

    [TestFixture]
    public class LazyExpiryTest
    {
        private static readonly DateTime future = DateTime.UtcNow.AddDays(1);
        [Test]
        public void Ctor_Null_1()
        {
            Assert.Throws<ArgumentNullException>(() => new LazyExpiry<int>(null));
        }
        [Test]
        public void NotThreadSafe()
        {
            var l2 = new LazyExpiry<int>(delegate () { return Tuple.Create(42, future); });

            Assert.AreEqual(42, l2.Value);
        }

        static int counter;

        /// <summary>
        /// This also tests that non expired values are returned
        /// </summary>
        [Test]
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

            Assert.IsFalse(failed);
            Assert.AreEqual(42, l.Value);
        }

        [Test]
        public void ModeNone()
        {
            int x;
            bool fail = true;
            LazyExpiry<int> lz = new LazyExpiry<int>(() => { if (fail) throw new Exception(); else return Tuple.Create(99, future); }, LazyThreadSafetyMode.None);
            try
            {
                x = lz.Value;
                Assert.Fail("#1");
                Console.WriteLine(x);
            }
            catch (Exception) { }

            try
            {
                x = lz.Value;
                Assert.Fail("#2");
            }
            catch (Exception) { }

            fail = false;
            x = lz.Value;
            // since lazy now can get a value, it should not fail
        }

        [Test]
        public void ModePublicationOnly()
        {
            bool fail = true;
            int invoke = 0;
            LazyExpiry<int> lz = new LazyExpiry<int>(() => { ++invoke; if (fail) throw new Exception(); else return Tuple.Create(99, future); }, LazyThreadSafetyMode.PublicationOnly);

            try
            {
                int x = lz.Value;
                Assert.Fail("#1");
                Console.WriteLine(x);
            }
            catch (Exception) { }

            try
            {
                int x = lz.Value;
                Assert.Fail("#2");
            }
            catch (Exception) { }


            Assert.AreEqual(2, invoke, "#3");
            fail = false;
            Assert.AreEqual(99, lz.Value, "#4");
            Assert.AreEqual(3, invoke, "#5");

            invoke = 0;
            bool rec = true;
            lz = new LazyExpiry<int>(() => { ++invoke; bool r = rec; rec = false; return Tuple.Create(r ? lz.Value : 88, future); }, LazyThreadSafetyMode.PublicationOnly);

            Assert.AreEqual(88, lz.Value, "#6");
            Assert.AreEqual(2, invoke, "#7");
        }

        [Test]
        public void ModeExecutionAndPublication()
        {
            int invoke = 0;
            bool fail = true;
            LazyExpiry<int> lz = new LazyExpiry<int>(() => { ++invoke; if (fail) throw new Exception(); else return Tuple.Create(99, future); }, LazyThreadSafetyMode.ExecutionAndPublication);

            try
            {
                int x = lz.Value;
                Assert.Fail("#1");
                Console.WriteLine(x);
            }
            catch (Exception) { }
            Assert.AreEqual(1, invoke, "#2");

            try
            {
                int x = lz.Value;
                Assert.Fail("#3");
            }
            catch (Exception) { }
            Assert.AreEqual(2, invoke, "#4");

            fail = false;
            { int x = lz.Value; }
            Assert.AreEqual(3, invoke, "#6"); // since two initializations failed, we get the value once it has not
        }

        static Tuple<int, DateTime> Return22()
        {
            return Tuple.Create(22, future);
        }

        [Test]
        public void Trivial_Lazy()
        {
            var x = new LazyExpiry<int>(Return22, false);
            Assert.AreEqual(22, x.Value, "#1");
        }

        [Test]
        public void Expired_lazy()
        {
            counter = 42;
            var x = new LazyExpiry<int>(() => Tuple.Create(counter++, DateTime.Now.AddDays(-1)), false);
            Assert.AreEqual(42, x.Value, "#1");
            Assert.AreEqual(43, x.Value, "#2");
            Assert.AreEqual(44, x.Value, "#3");
        }
    }
}
