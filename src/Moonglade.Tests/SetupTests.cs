using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Setup;
using NUnit.Framework;

namespace Moonglade.Tests
{
    public class SetupTests
    {
        private string _conn;

        [SetUp]
        public void Setup()
        {
            _conn = "Server=(local);Database=moonglade-setup;Trusted_Connection=True;";
        }
    }
}
