using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Extensions;

namespace Spark.Engine.Test.Extensions
{
    [TestClass]
    public class OperationOutcomeInnerErrorsTest
    {
        [TestMethod]
        public void AddAllInnerErrorsTest()
        {
            OperationOutcome outcome;

            try
            {
                try
                {
                    try
                    {
                        throw new Exception("Third error level");
                    }
                    catch (Exception e3)
                    {
                        throw new Exception("Second error level", e3);
                    }
                }
                catch (Exception e2)
                {
                    throw new Exception("First error level", e2);
                }
            }
            catch (Exception e1)
            {
                outcome = new OperationOutcome().AddAllInnerErrors(e1);
            }

            Assert.IsTrue(outcome.Issue.FindIndex(i => i.Diagnostics.Equals("Exception: First error level")) == 0, "First error level should be at index 0");
            Assert.IsTrue(outcome.Issue.FindIndex(i => i.Diagnostics.Equals("Exception: Second error level")) == 1, "Second error level should be at index 1");
            Assert.IsTrue(outcome.Issue.FindIndex(i => i.Diagnostics.Equals("Exception: Third error level")) == 2, "Third error level should be at index 2");
        }
    }
}
