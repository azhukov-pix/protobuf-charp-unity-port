﻿using System;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using NUnit.Framework;

namespace Google.Protobuf
{
    public class RepeatedFieldTest
    {
        [Test]
        public void NullValuesRejected()
        {
            var list = new RepeatedField<string>();
            Assert.Throws<ArgumentNullException>(() => list.Add((string) null));
            Assert.Throws<ArgumentNullException>(() => list.Add((IEnumerable<string>) null));
            Assert.Throws<ArgumentNullException>(() => list.Add((RepeatedField<string>)null));
            Assert.Throws<ArgumentNullException>(() => list.Contains(null));
            Assert.Throws<ArgumentNullException>(() => list.IndexOf(null));
        }

        [Test]
        public void Add_SingleItem()
        {
            var list = new RepeatedField<string>();
            list.Add("foo");
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("foo", list[0]);
        }

        [Test]
        public void Add_Sequence()
        {
            var list = new RepeatedField<string>();
            list.Add(new[] { "foo", "bar" });
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("foo", list[0]);
            Assert.AreEqual("bar", list[1]);
        }

        [Test]
        public void Add_RepeatedField()
        {
            var list = new RepeatedField<string>();
            list.Add(new RepeatedField<string> { "foo", "bar" });
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("foo", list[0]);
            Assert.AreEqual("bar", list[1]);
        }
    }
}
