using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hl7.Fhir.Model;
using Spark.Engine.Service;
using Spark.Engine.Core;
using System.Collections.Generic;
using Spark.Engine.Search;
using Spark.Engine.Model;
using Spark.Search;
using static Hl7.Fhir.Model.ModelInfo;
using Spark.Engine.Interfaces;
using Moq;
using Hl7.Fhir.Serialization;

namespace Spark.Engine.Test.Service
{
    [TestClass]
    public class IndexServiceTests
    {

        private IFhirModel _fhirModel;
        private FhirPropertyIndex _propIndex;
        private ResourceVisitor _resourceVisitor;
        private ElementIndexer _elementIndexer;

        private IndexService sut;

        [TestInitialize]
        public void TestInitialize()
        {
            var spPatientName = new SearchParamDefinition() { Resource = "Patient", Name = "name", Description = @"A portion of either family or given name of the patient", Type = SearchParamType.String, Path = new string[] { "Patient.name", } };
            var searchParameters = new List<SearchParamDefinition> { spPatientName };
            var resources = new Dictionary<Type, string> { { typeof(Patient), "Patient" }, { typeof(HumanName), "HumanName" } };
            var enums = new List<Type>();
            //CK: I use real objects: saves me a lot of mocking and provides for a bit of integration testing.
            _fhirModel = new FhirModel(resources, searchParameters, enums);
            _propIndex = new FhirPropertyIndex(_fhirModel, new List<Type> { typeof(Patient), typeof(HumanName) });
            _resourceVisitor = new ResourceVisitor(_propIndex);
            _elementIndexer = new ElementIndexer(_fhirModel);

            var _indexStoreMock = new Mock<IIndexStore>();
            //_indexStoreMock.Setup(ixs => ixs.Save(It.IsAny<IndexValue>))

            sut = new IndexService(_fhirModel, _propIndex, _resourceVisitor, _elementIndexer, _indexStoreMock.Object);
        }

        [TestMethod]
        public void TestIndexResourceSimple()
        {
            var patient = new Patient();
            patient.Name.Add(new HumanName().WithGiven("Adriaan").AndFamily("Bestevaer"));

            IKey patientKey = new Key("http://localhost/", "Patient", "001", "v02");

            IndexValue result = sut.IndexResource(patient, patientKey);

            Assert.AreEqual("root", result.Name);
            Assert.AreEqual(5, result.Values.Count, "Expected 1 result for searchparameter 'name' and 4 for meta info");
            Assert.IsInstanceOfType(result.Values[0], typeof(IndexValue));
            var first = (IndexValue)result.Values[0];
            Assert.AreEqual("name", first.Name);
            Assert.AreEqual(2, first.Values.Count);
            Assert.IsInstanceOfType(first.Values[0], typeof(StringValue));
            Assert.IsInstanceOfType(first.Values[1], typeof(StringValue));
        }

        [TestMethod]
        public void TestIndexResourcePatientComplete()
        {
            _fhirModel = new FhirModel(); //For this test I want all available types and searchparameters.
            _propIndex = new FhirPropertyIndex(_fhirModel);
            _resourceVisitor = new ResourceVisitor(_propIndex);
            _elementIndexer = new ElementIndexer(_fhirModel);
            var _indexStoreMock = new Mock<IIndexStore>();
            //_indexStoreMock.Setup(ixs => ixs.Save(It.IsAny<IndexValue>))

            sut = new IndexService(_fhirModel, _propIndex, _resourceVisitor, _elementIndexer, _indexStoreMock.Object);

            var patientResource = FhirParser.ParseResourceFromJson(examplePatientJson);

            IKey patientKey = new Key("http://localhost/", "Patient", "001", null);

            IndexValue result = sut.IndexResource(patientResource, patientKey);

            Assert.IsNotNull(result);
        }

        private string examplePatientJson = @"
{
  ""resourceType"" : ""Patient"",
  ""meta"" : {
    ""lastUpdated"" : ""2015-11-26T14:17:15Z""
  },
  ""text"" : {
    ""status"" : ""additional"",
    ""div"" : ""<div xmlns=\""http://www.w3.org/1999/xhtml\""><p>This is supposed to be the narrative</p></div>""
  },
  ""identifier"" : [
    {
      ""use"" : ""usual"",
      ""system"" : ""http://unique.namepace/details"",
      ""value"" : ""unique-id""
    }
  ],
  ""active"" : true,
  ""name"" : [
    {
      ""use"" : ""usual"",
      ""text"" : ""Jan van Jansen"",
      ""family"" : [
        ""Jansen""
      ],
      ""given"" : [
        ""Jan""
      ],
      ""prefix"" : [
        ""van""
      ]
},
    {
      ""use"" : ""usual"",
      ""text"" : ""Piet de Brug"",
      ""family"" : [
        ""Brug""
      ],
      ""given"" : [
        ""Piet""
      ],
      ""prefix"" : [
        ""de""
      ]
}
  ],
  ""telecom"" : [
    {
      ""system"" : ""phone"",
      ""value"" : ""555-555-5555"",
      ""use"" : ""work""
    }
  ],
  ""gender"" : ""male"",
  ""birthDate"" : ""2015-11-26T00:00:00+01:00"",
  ""deceasedBoolean"" : false,
  ""address"" : [
    {
      ""use"" : ""home"",
      ""text"" : ""13 Boring St, Erewhon, 5555 (New Zealand)"",
      ""line"" : [
        ""13 Boring St""
      ],
      ""city"" : ""Erewhon"",
      ""postalCode"" : ""5555"",
      ""country"" : ""New Zealand""
    }
  ],
  ""maritalStatus"" : {
    ""coding"" : [
      {
        ""system"" : ""http://hl7.org/fhir/marital-status"",
        ""code"" : ""U"",
        ""display"" : ""Unmarried""
      }
    ],
    ""text"" : ""text representation""
  },
  ""multipleBirthInteger"" : 1,
  ""photo"" : [
    {
      ""contentType"" : ""text/plain"",
      ""language"" : ""en-US"",
      ""url"" : ""http://somewhere""
    }
  ],
  ""contact"" : [
    {
      ""relationship"" : [
        {
          ""coding"" : [
            {
              ""system"" : ""http://hl7.org/fhir/patient-contact-relationship"",
              ""code"" : ""emergency"",
              ""display"" : ""Emergency""
            }
          ],
          ""text"" : ""text representation""
        }
      ],
      ""name"" : {
        ""use"" : ""usual"",
        ""text"" : ""prefix given family"",
        ""family"" : [
          ""Pietersen""
        ],
        ""given"" : [
          ""Brenda""
        ],
      },
      ""telecom"" : [
        {
          ""system"" : ""phone"",
          ""value"" : ""555-555-4444"",
          ""use"" : ""work""
        }
      ],
      ""address"" : {
        ""use"" : ""home"",
        ""text"" : ""13 Boring St, Erewhon, 5555 (New Zealand)"",
        ""line"" : [
          ""13 Boring St""
        ],
        ""city"" : ""Erewhon"",
        ""postalCode"" : ""5555"",
        ""country"" : ""New Zealand""
      },
      ""gender"" : ""female"",
      ""organization"" : {
        ""reference"" : ""Organization/001"",
        ""display"" : ""Pietersen's hospital""
      },
      ""period"" : {
        ""start"" : ""2010-11-26T15:17:15+01:00"",
        ""end"" : ""2015-11-26T15:17:15+01:00""
      }
    }
  ],
  ""animal"" : {
    ""species"" : {
      ""coding"" : [
        {
          ""system"" : ""http://hl7.org/fhir/animal-species"",
          ""code"" : ""canislf"",
          ""display"" : ""Dog""
        }
      ],
      ""text"" : ""text representation""
    },
    ""breed"" : {
      ""coding"" : [
        {
          ""system"" : ""http://hl7.org/fhir/animal-breed"",
          ""code"" : ""gsd"",
          ""display"" : ""German Shepherd Dog""
        }
      ],
      ""text"" : ""text representation""
    },
    ""genderStatus"" : {
      ""coding"" : [
        {
          ""system"" : ""http://hl7.org/fhir/animal-genderstatus"",
          ""code"" : ""neutered"",
          ""display"" : ""Neutered""
        }
      ],
      ""text"" : ""text representation""
    }
  },
  ""communication"" : [
    {
      ""language"" : {
        ""coding"" : [
          {
            ""system"" : ""http://system.id"",
            ""code"" : ""code"",
            ""display"" : ""display""
          }
        ],
        ""text"" : ""text representation""
      },
      ""preferred"" : false
    }
  ],
  ""careProvider"" : [
    {
      ""reference"" : ""CareProvider/001"",
      ""display"" : ""Jansen's careprovider""
    }
  ],
  ""managingOrganization"" : {
    ""reference"" : ""Organization/002"",
    ""display"" : ""Jansen's hospital""
  }
}
";
    }
}
