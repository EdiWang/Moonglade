using Moonglade.Setup;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Tests
{
    [TestFixture]
    public class SetupHelperTests
    {
        [Test]
        public void TestInstanceCreationInvalidConnectionString()
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                var helper = new SetupHelper(null);
            });
        }
    }
}
