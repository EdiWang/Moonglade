﻿using Microsoft.AspNetCore.Mvc;
using Moonglade.Web.Configuration;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Configuration
{
    [TestFixture]
    public class ConfigureApiBehaviorTests
    {
        [Test]
        public void BlogApiBehavior_OK()
        {
            var target = new ApiBehaviorOptions();
            ConfigureApiBehavior.BlogApiBehavior(target);

            Assert.IsNotNull(target.InvalidModelStateResponseFactory);
        }
    }
}
