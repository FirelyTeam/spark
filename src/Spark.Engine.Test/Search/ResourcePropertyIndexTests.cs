using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Search.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Test.Search
{
    [TestClass]
    public class ResourcePropertyIndexTests
    {
        [TestMethod]
        public void TestGetIndex()
        {
            var index = ResourcePropertyIndex.getIndex();
            Assert.IsNotNull(index);

            var pm = index.findPropertyMapping("Patient", "name");
            Assert.IsNotNull(pm);

            pm = index.findPropertyMapping("Account", "subject");
            Assert.IsNotNull(pm);
        }
    }
}
