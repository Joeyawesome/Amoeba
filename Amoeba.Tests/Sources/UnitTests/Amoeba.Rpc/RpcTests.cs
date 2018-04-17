using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Amoeba.Rpc;
using Amoeba.Service;
using Omnius.Base;
using Xunit;
using Xunit.Abstractions;

namespace Amoeba.Tests
{
    [Trait("Category", "Amoeba.Rpc")]
    public class RpcTests : TestsBase
    {
        private readonly Random _random = new Random();

        public RpcTests(ITestOutputHelper output) : base(output)
        {

        }

        [Fact]
        public void ConnectTest()
        {

        }
    }
}
