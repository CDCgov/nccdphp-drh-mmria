using System;
using Xunit;

namespace test;

public sealed class UnitTest1
{
    public sealed class Class1
    {
        [Fact]
        public void PassingTest()
        {
            Assert.Equal(4, Add(2, 2));
        }
    /*
        [Fact]
        public void FailingTest()
        {
            Assert.Equal(5, Add(2, 2));
        }
    */
        int Add(int x, int y)
        {
            return x + y;
        }
    }
}
