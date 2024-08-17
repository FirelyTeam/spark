/* 
 * Copyright (c) 2015-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2018-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Spark.Engine.Test.Core
{
    [TestClass]
    public class ResourceVisitorTests
    {
        private readonly Regex _headTailRegex = new Regex(@"(?([^\.]*\(.*\))(?<head>[^\(]*)\((?<predicate>.*)\)(\.(?<tail>.*))?|(?<head>[^\.]*)(\.(?<tail>.*))?)");

        [TestMethod]
        public void TestHeadNoTail()
        {
            var test = "a";
            var match = _headTailRegex.Match(test);
            Assert.AreEqual("a", match.Groups["head"].Value);
            Assert.AreEqual("", match.Groups["predicate"].Value);
            Assert.AreEqual("", match.Groups["tail"].Value);
        }

        [TestMethod]
        public void TestHeadAndTailMultipleCharacters()
        {
            var test = "ax.bx.cx";
            var match = _headTailRegex.Match(test);
            Assert.AreEqual("ax", match.Groups["head"].Value);
            Assert.AreEqual("", match.Groups["predicate"].Value);
            Assert.AreEqual("bx.cx", match.Groups["tail"].Value);
        }

        [TestMethod]
        public void TestHeadWithPredicateNoTail()
        {
            var test = "a(x=y)";
            var match = _headTailRegex.Match(test);
            Assert.AreEqual("a", match.Groups["head"].Value);
            Assert.AreEqual("x=y", match.Groups["predicate"].Value);
            Assert.AreEqual("", match.Groups["tail"].Value);
        }

        [TestMethod]
        public void TestHeadAndTailNoPredicate()
        {
            var test = "a.b.c";
            var match = _headTailRegex.Match(test);
            Assert.AreEqual("a", match.Groups["head"].Value);
            Assert.AreEqual("", match.Groups["predicate"].Value);
            Assert.AreEqual("b.c", match.Groups["tail"].Value);
        }

        [TestMethod]
        public void TestHeadAndTailWithPredicate()
        {
            var test = "a(x.y=z).b.c";
            var match = _headTailRegex.Match(test);
            Assert.AreEqual("a", match.Groups["head"].Value);
            Assert.AreEqual("x.y=z", match.Groups["predicate"].Value);
            Assert.AreEqual("b.c", match.Groups["tail"].Value);
        }

        [TestMethod]
        public void TestLongerHeadAndTailWithPredicate()
        {
            var test = "ax(yx=zx).bx";
            var match = _headTailRegex.Match(test);
            Assert.AreEqual("ax", match.Groups["head"].Value);
            Assert.AreEqual("yx=zx", match.Groups["predicate"].Value);
            Assert.AreEqual("bx", match.Groups["tail"].Value);
        }

        private IFhirModel _fhirModel;
        private FhirPropertyIndex _index;
        private ResourceVisitor _sut;
        private Patient _patient;
        private int _expectedActionCounter = 0;
        private int _actualActionCounter = 0;

        [TestInitialize]
        public void TestInitialize()
        {
            _fhirModel = new FhirModel();
            _index = new FhirPropertyIndex(_fhirModel, new List<Type> { typeof(Patient), typeof(ClinicalImpression), typeof(HumanName), typeof(CodeableConcept), typeof(Coding) });
            _sut = new ResourceVisitor(_index);
            _patient = new Patient();
            _patient.Name.Add(new HumanName().WithGiven("Sjors").AndFamily("Jansen"));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Assert.AreEqual(_expectedActionCounter, _actualActionCounter);
        }

        [TestMethod]
        public void TestVisitNotExistingPathNoPredicate()
        {
            _sut.VisitByPath(_patient, ob => Assert.Fail(), "not_existing_property");
        }

        [TestMethod]
        public void TestVisitSinglePathNoPredicate()
        {
            _expectedActionCounter = 1;
            _sut.VisitByPath(_patient, ob => 
                {
                    _actualActionCounter++;
                    if (ob.GetType() != typeof(HumanName))
                        Assert.Fail();
                }, "name");
        }

        [TestMethod]
        public void TestVisitDataChoiceProperty()
        {
            _expectedActionCounter = 1;
            ClinicalImpression ci = new ClinicalImpression
            {
                Code = new CodeableConcept("test.system", "test.code")
            };
            _sut.VisitByPath(ci, ob =>
                {
                    _actualActionCounter++;
                    if (ob.ToString() != "test.system")
                        Assert.Fail();
                },
                "code.coding.system");
        }

        [TestMethod]
        public void TestVisitDataChoice_x_Property()
        {
            _expectedActionCounter = 0; //We expect 0 actions: ResourceVisitor needs not recognize this, it should be solved in processing the searchparameter at indexing time.
            Condition cd = new Condition
            {
                Onset = new FhirDateTime(2015, 6, 15)
            };
            _sut.VisitByPath(cd, ob =>
            {
                _actualActionCounter++;
                if (ob.GetType() != typeof(FhirDateTime))
                    Assert.Fail();
            },
                "onset[x]");
        }

        [TestMethod]
        public void TestVisitNestedPathNoPredicate()
        {
            _expectedActionCounter = 1;
            _sut.VisitByPath(_patient, ob => 
                {
                    _actualActionCounter++;
                    if (ob.ToString() != "Sjors")
                            Assert.Fail();
                }, "name.given");
        }

        [TestMethod]
        public void TestVisitSinglePathWithPredicateAndFollowingProperty()
        {
            _expectedActionCounter = 1;
            _patient.Name.Add(new HumanName().WithGiven("Sjimmie").AndFamily("Visser"));
            _sut.VisitByPath(_patient, ob => 
                {
                    _actualActionCounter++;
                    if (ob.ToString() != "Sjimmie")
                        Assert.Fail();
                }, "name[given=Sjimmie].given");
        }

        [TestMethod]
        public void TestVisitSinglePathWithPredicate()
        {
            _expectedActionCounter = 1;
            _patient.Name.Add(new HumanName().WithGiven("Sjimmie").AndFamily("Visser"));
            _sut.VisitByPath(_patient, ob =>
            {
                _actualActionCounter++;
                Assert.IsInstanceOfType(ob, typeof(HumanName));
                Assert.AreEqual("Sjimmie", (ob as HumanName).GivenElement.First().ToString());
            }, "name[given=Sjimmie]");
        }
    }
}
