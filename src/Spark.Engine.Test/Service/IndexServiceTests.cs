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
using Moq;
using Hl7.Fhir.Serialization;
using System.Linq;
using Spark.Engine.Store.Interfaces;

namespace Spark.Engine.Test.Service
{
    [TestClass]
    public class IndexServiceTests
    {


        private IndexService sutLimited;
        private IndexService sutFull;

        [TestInitialize]
        public void TestInitialize()
        {

            IFhirModel _fhirModel;
            FhirPropertyIndex _propIndex;
            ResourceVisitor _resourceVisitor;
            ElementIndexer _elementIndexer;
            var _indexStoreMock = new Mock<IIndexStore>();

            var spPatientName = new SearchParamDefinition() { Resource = "Patient", Name = "name", Description = @"A portion of either family or given name of the patient", Type = SearchParamType.String, Path = new string[] { "Patient.name", } };
            var searchParameters = new List<SearchParamDefinition> { spPatientName };
            var resources = new Dictionary<Type, string> { { typeof(Patient), "Patient" }, { typeof(HumanName), "HumanName" } };
            var enums = new List<Type>();
            //CK: I use real objects: saves me a lot of mocking and provides for a bit of integration testing.
            _fhirModel = new FhirModel(resources, searchParameters, enums);
            _propIndex = new FhirPropertyIndex(_fhirModel, new List<Type> { typeof(Patient), typeof(HumanName) });
            _resourceVisitor = new ResourceVisitor(_propIndex);
            _elementIndexer = new ElementIndexer(_fhirModel);

            //_indexStoreMock.Setup(ixs => ixs.Save(It.IsAny<IndexValue>))

            sutLimited = new IndexService(_fhirModel, _propIndex, _resourceVisitor, _elementIndexer, _indexStoreMock.Object);

            _fhirModel = new FhirModel(); //For this test I want all available types and searchparameters.
            _propIndex = new FhirPropertyIndex(_fhirModel);
            _resourceVisitor = new ResourceVisitor(_propIndex);
            _elementIndexer = new ElementIndexer(_fhirModel);

            sutFull = new IndexService(_fhirModel, _propIndex, _resourceVisitor, _elementIndexer, _indexStoreMock.Object);
        }

        [TestMethod]
        public void TestIndexResourceSimple()
        {
            var patient = new Patient();
            patient.Name.Add(new HumanName().WithGiven("Adriaan").AndFamily("Bestevaer"));

            IKey patientKey = new Key("http://localhost/", "Patient", "001", "v02");

            IndexValue result = sutLimited.IndexResource(patient, patientKey);

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
            var patientResource = FhirParser.ParseResourceFromJson(examplePatientJson);

            IKey patientKey = new Key("http://localhost/", "Patient", "001", null);

            IndexValue result = sutFull.IndexResource(patientResource, patientKey);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestIndexResourceAppointmentComplete()
        {
            var appResource = FhirParser.ParseResourceFromJson(exampleAppointmentJson);

            IKey appKey = new Key("http://localhost/", "Appointment", "2docs", null);

            IndexValue result = sutFull.IndexResource(appResource, appKey);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestIndexResourceCareplanWithContainedGoal()
        {
            var cpResource = FhirParser.ParseResourceFromJson(careplanWithContainedGoal);

            IKey cpKey = new Key("http://localhost/", "Careplan", "f002", null);

            IndexValue result = sutFull.IndexResource(cpResource, cpKey);

            Assert.IsNotNull(result);
        }


        [TestMethod]
        public void TestIndexResourceObservation()
        {
            var obsResource = FhirParser.ParseResourceFromJson(exampleObservationJson);

            IKey cpKey = new Key("http://localhost/", "Observation", "blood-pressure", null);

            IndexValue result = sutFull.IndexResource(obsResource, cpKey);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestIndexWithPath_x_()
        {
            Condition cd = new Condition();
            cd.Onset = new FhirDateTime(2015, 6, 15);

            IKey cdKey = new Key("http://localhost/", "Condition", "test", null);

            IndexValue result = sutFull.IndexResource(cd, cdKey);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Values.Where(iv => (iv as IndexValue).Name == "onset"));
        }

        private string exampleObservationJson = @"{""bodySite"":{""coding"":[{""code"":""368209003"",""display"":""Right arm"",""system"":""http://snomed.info/sct""}]},""code"":{""coding"":[{""code"":""55284-4"",""display"":""Blood pressure systolic & diastolic"",""system"":""http://loinc.org""}]},""component"":[{""code"":{""coding"":[{""code"":""8480-6"",""display"":""Systolic blood pressure"",""system"":""http://loinc.org""},{""code"":""271649006"",""display"":""Systolic blood pressure"",""system"":""http://snomed.info/sct""},{""code"":""bp-s"",""display"":""Systolic Blood pressure"",""system"":""http://acme.org/devices/clinical-codes""}]},""valueQuantity"":{""unit"":""mm[Hg]"",""value"":107}},{""code"":{""coding"":[{""code"":""8462-4"",""display"":""Diastolic blood pressure"",""system"":""http://loinc.org""}]},""valueQuantity"":{""unit"":""mm[Hg]"",""value"":60}}],""effectiveDateTime"":""2012-09-17"",""id"":""blood-pressure"",""identifier"":[{""system"":""urn:ietf:rfc:3986"",""value"":""urn:uuid:187e0c12-8dd2-67e2-99b2-bf273c878281""}],""interpretation"":{""coding"":[{""code"":""L"",""display"":""Below low normal"",""system"":""http://hl7.org/fhir/v2/0078""}],""text"":""low""},""meta"":{""lastUpdated"":""2014-01-30T22:35:23+11:00""},""performer"":[{""reference"":""Practitioner/example""}],""resourceType"":""Observation"",""status"":""final"",""subject"":{""reference"":""Patient/example""},""text"":{""div"":""<div><p><b>Generated Narrative with Details</b></p><p><b>id</b>: blood-pressure</p><p><b>meta</b>: </p><p><b>identifier</b>: urn:uuid:187e0c12-8dd2-67e2-99b2-bf273c878281</p><p><b>status</b>: final</p><p><b>code</b>: Blood pressure systolic &amp; diastolic <span>(Details : {LOINC code '55284-4' = 'Blood pressure systolic and diastolic', given as 'Blood pressure systolic &amp; diastolic'})</span></p><p><b>subject</b>: <a>Patient/example</a></p><p><b>effective</b>: 17/09/2012</p><p><b>performer</b>: <a>Practitioner/example</a></p><p><b>interpretation</b>: low <span>(Details : {http://hl7.org/fhir/v2/0078 code 'L' = 'Low', given as 'Below low normal'})</span></p><p><b>bodySite</b>: Right arm <span>(Details : {SNOMED CT code '368209003' = '368209003', given as 'Right arm'})</span></p><blockquote><p><b>component</b></p><p><b>code</b>: Systolic blood pressure <span>(Details : {LOINC code '8480-6' = 'Systolic blood pressure', given as 'Systolic blood pressure'}; {SNOMED CT code '271649006' = '271649006', given as 'Systolic blood pressure'}; {http://acme.org/devices/clinical-codes code 'bp-s' = '??', given as 'Systolic Blood pressure'})</span></p><p><b>value</b>: 107 mm[Hg]</p></blockquote><blockquote><p><b>component</b></p><p><b>code</b>: Diastolic blood pressure <span>(Details : {LOINC code '8462-4' = 'Diastolic blood pressure', given as 'Diastolic blood pressure'})</span></p><p><b>value</b>: 60 mm[Hg]</p></blockquote></div>"",""status"":""generated""}}";

        private string exampleAppointmentJson = @"
{
    ""resourceType"":""Appointment"",
    ""id"":""2docs"",
    ""text"":{
        ""status"":""generated"",
        ""div"":""<div xmlns=\""http://www.w3.org/1999/xhtml\"">Brian MRI results discussion</div>""},
    ""status"":""booked"",
    ""type"":{
        ""coding"":[
            {
                ""code"":""52"",
                ""display"":""General Discussion""
            }
        ]},
    ""priority"":5,
    ""description"":""Discussion about Peter Chalmers MRI results"",
    ""start"":""2013-12-09T09:00:00+00:00"",
    ""end"":""2013-12-09T11:00:00+00:00"",
    ""comment"":""Clarify the results of the MRI to ensure context of test was correct"",
    ""participant"":
        [
            {
                ""actor"":
                    {
                        ""reference"":""Patient/example"",
                        ""display"":""Peter James Chalmers""
                    },
                ""required"":""information-only"",
                ""status"":""accepted""
            },
            {
                ""actor"":
                {
                    ""reference"":""Practitioner/example"",
                    ""display"":""Dr Adam Careful""
                },
                ""required"":""required"",
                ""status"":""accepted""},
            {
                ""actor"":
                {
                    ""reference"":""Practitioner/f202"",
                    ""display"":""Luigi Maas""
                },
                ""required"":""required"",
                ""status"":""accepted""},
            {
                ""actor"":
                {
                    ""display"":""Phone Call""
                },
                ""required"":""information-only"",
                ""status"":""accepted""
            }
        ]
}";


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

        private string careplanWithContainedGoal = @"
{
  ""resourceType"": ""CarePlan"",
  ""id"": ""f201"",
  ""text"": {
    ""status"": ""generated"",
    ""div"": ""<div><p><b>Generated Narrative with Details</b></p><p><b>id</b>: f201</p><p><b>contained</b>: </p><p><b>subject</b>: <a>Roel</a></p><p><b>status</b>: draft</p><p><b>period</b>: 11/03/2013 --&gt; 13/03/2013</p><p><b>modified</b>: 11/03/2013</p><p><b>addresses</b>: <a>Roel's renal insufficiency</a></p><blockquote><p><b>participant</b></p><p><b>role</b>: Review of care plan <span>(Details : {SNOMED CT code '425268008' = '425268008', given as 'Review of care plan'})</span></p><p><b>member</b>: <a>Dokter Bronsig</a></p></blockquote><blockquote><p><b>participant</b></p><p><b>role</b>: Carer <span>(Details : {SNOMED CT code '229774002' = '229774002', given as 'Carer'})</span></p><p><b>member</b>: <a>Nurse Carla Espinosa</a></p></blockquote><p><b>goal</b>: id: goal; Roel; description: Re-established renal function with at least healthy nutrients.; status: achieved</p><blockquote><p><b>activity</b></p><h3>Details</h3><table><tr><td>-</td><td><b>Category</b></td><td><b>Code</b></td><td><b>Status</b></td><td><b>Prohibited</b></td><td><b>Scheduled[x]</b></td><td><b>Product[x]</b></td><td><b>DailyAmount</b></td></tr><tr><td>*</td><td>Diet <span>(Details : {http://hl7.org/fhir/care-plan-activity-category code 'diet' = 'Diet)</span></td><td>Potassium supplementation <span>(Details : {SNOMED CT code '284093001' = '284093001', given as 'Potassium supplementation'})</span></td><td>completed</td><td>false</td><td>daily</td><td><a>Potassium</a></td><td>80 mmol<span> (Details: SNOMED CT code 258718000 = '258718000')</span></td></tr></table></blockquote><blockquote><p><b>activity</b></p><h3>Details</h3><table><tr><td>-</td><td><b>Category</b></td><td><b>Code</b></td><td><b>Status</b></td><td><b>Prohibited</b></td></tr><tr><td>*</td><td>Observation <span>(Details : {http://hl7.org/fhir/care-plan-activity-category code 'observation' = 'Observation)</span></td><td>Echography of kidney <span>(Details : {SNOMED CT code '306005' = '306005', given as 'Echography of kidney'})</span></td><td>completed</td><td>false</td></tr></table></blockquote></div>""
  },
  ""contained"": [
    {
      ""resourceType"": ""Goal"",
      ""id"": ""goal"",
      ""subject"": {
        ""reference"": ""Patient/f201"",
        ""display"": ""Roel""
      },
      ""description"": ""Re-established renal function with at least healthy nutrients."",
      ""status"": ""achieved""
    }
  ],
  ""subject"": {
    ""reference"": ""Patient/f201"",
    ""display"": ""Roel""
  },
  ""status"": ""draft"",
  ""period"": {
    ""fhir_comments"": [
      ""  This careplan is \""ended\"", but was never closed in the EHR, wherefore the status is \""planned\""  "",
      ""  Period is less than nine days because the careplan requires adjustments after evaluation  ""
    ],
    ""start"": ""2013-03-11"",
    ""end"": ""2013-03-13""
  },
  ""modified"": ""2013-03-11"",
  ""addresses"": [
    {
      ""reference"": ""Condition/f204"",
      ""display"": ""Roel's renal insufficiency""
    }
  ],
  ""participant"": [
    {
      ""role"": {
        ""coding"": [
          {
            ""system"": ""http://snomed.info/sct"",
            ""code"": ""425268008"",
            ""display"": ""Review of care plan""
          }
        ]
      },
      ""member"": {
        ""reference"": ""Practitioner/f201"",
        ""display"": ""Dokter Bronsig""
      }
    },
    {
      ""role"": {
        ""coding"": [
          {
            ""system"": ""http://snomed.info/sct"",
            ""code"": ""229774002"",
            ""display"": ""Carer""
          }
        ]
      },
      ""member"": {
        ""reference"": ""Practitioner/f204"",
        ""display"": ""Nurse Carla Espinosa""
      }
    }
  ],
  ""goal"": [
    {
      ""reference"": ""#goal""
    }
  ],
  ""activity"": [
    {
      ""detail"": {
        ""fhir_comments"": [
          ""  Potassium supplement  ""
        ],
        ""category"": {
          ""coding"": [
            {
              ""system"": ""http://hl7.org/fhir/care-plan-activity-category"",
              ""code"": ""diet""
            }
          ]
        },
        ""code"": {
          ""coding"": [
            {
              ""system"": ""http://snomed.info/sct"",
              ""code"": ""284093001"",
              ""display"": ""Potassium supplementation""
            }
          ]
        },
        ""status"": ""completed"",
        ""prohibited"": false,
        ""scheduledString"": ""daily"",
        ""productReference"": {
          ""fhir_comments"": [
            ""  TODO Isn't <performer> redundant when considering <participant> that was defined before?  ""
          ],
          ""reference"": ""Substance/f203"",
          ""display"": ""Potassium""
        },
        ""dailyAmount"": {
          ""value"": 80,
          ""unit"": ""mmol"",
          ""system"": ""http://snomed.info/sct"",
          ""code"": ""258718000""
        }
      }
    },
    {
      ""detail"": {
        ""fhir_comments"": [
          ""  Echo of the kidney  ""
        ],
        ""category"": {
          ""coding"": [
            {
              ""system"": ""http://hl7.org/fhir/care-plan-activity-category"",
              ""code"": ""observation""
            }
          ]
        },
        ""code"": {
          ""coding"": [
            {
              ""system"": ""http://snomed.info/sct"",
              ""code"": ""306005"",
              ""display"": ""Echography of kidney""
            }
          ]
        },
        ""status"": ""completed"",
        ""prohibited"": false
      }
    }
  ]
}";

    }
}
