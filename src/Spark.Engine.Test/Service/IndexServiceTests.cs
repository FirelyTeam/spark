using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using System.Collections.Generic;
using Spark.Engine.Search;
using Spark.Engine.Model;
using Spark.Search;
using static Hl7.Fhir.Model.ModelInfo;
using Moq;
using Hl7.Fhir.Serialization;
using System.Linq;
using Spark.Engine.Service.FhirServiceExtensions;
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

            var spPatientName = new SearchParamDefinition() { Resource = "Patient", Name = "name", Description = new Markdown(@"A portion of either family or given name of the patient"), Type = SearchParamType.String, Path = new string[] { "Patient.name", } };
            var searchParameters = new List<SearchParamDefinition> { spPatientName };
            var resources = new Dictionary<Type, string> { { typeof(Patient), "Patient" }, { typeof(HumanName), "HumanName" } };
            //CK: I use real objects: saves me a lot of mocking and provides for a bit of integration testing.
            _fhirModel = new FhirModel(resources, searchParameters);
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
            Assert.AreEqual(1, result.NonInternalValues().Count(), "Expected 1 non-internal result for searchparameter 'name'");
            var first = result.NonInternalValues().First();
            Assert.AreEqual("name", first.Name);
            Assert.AreEqual(2, first.Values.Count);
            Assert.IsInstanceOfType(first.Values[0], typeof(StringValue));
            Assert.IsInstanceOfType(first.Values[1], typeof(StringValue));
        }

        [TestMethod]
        public void TestIndexResourcePatientComplete()
        {
            FhirJsonParser parser = new FhirJsonParser();
            var patientResource = parser.Parse<Resource>(examplePatientJson);

            IKey patientKey = new Key("http://localhost/", "Patient", "001", null);

            IndexValue result = sutFull.IndexResource(patientResource, patientKey);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestIndexResourceAppointmentComplete()
        {
            FhirJsonParser parser = new FhirJsonParser();
            var appResource = parser.Parse<Resource>(exampleAppointmentJson);

            IKey appKey = new Key("http://localhost/", "Appointment", "2docs", null);

            IndexValue result = sutFull.IndexResource(appResource, appKey);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestIndexResourceCareplanWithContainedGoal()
        {
            FhirJsonParser parser = new FhirJsonParser();
            var cpResource = parser.Parse<Resource>(carePlanWithContainedGoal);

            IKey cpKey = new Key("http://localhost/", "Careplan", "f002", null);

            IndexValue result = sutFull.IndexResource(cpResource, cpKey);

            Assert.IsNotNull(result);
        }


        [TestMethod]
        public void TestIndexResourceObservation()
        {
            FhirJsonParser parser = new FhirJsonParser();
            var obsResource = parser.Parse<Resource>(exampleObservationJson);

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
    ""serviceType"":{
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
  ""generalPractitioner"" : [
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

        private string carePlanWithContainedGoal = @"{
          ""resourceType"": ""CarePlan"",
          ""id"": ""f201"",
          ""text"": {
            ""status"": ""generated"",
            ""div"": ""\u003cdiv xmlns\u003d\""http://www.w3.org/1999/xhtml\""\u003e\u003cp\u003e\u003cb\u003eGenerated Narrative with Details\u003c/b\u003e\u003c/p\u003e\u003cp\u003e\u003cb\u003eid\u003c/b\u003e: f201\u003c/p\u003e\u003cp\u003e\u003cb\u003econtained\u003c/b\u003e: , \u003c/p\u003e\u003cp\u003e\u003cb\u003estatus\u003c/b\u003e: draft\u003c/p\u003e\u003cp\u003e\u003cb\u003eintent\u003c/b\u003e: proposal\u003c/p\u003e\u003cp\u003e\u003cb\u003esubject\u003c/b\u003e: \u003ca\u003eRoel\u003c/a\u003e\u003c/p\u003e\u003cp\u003e\u003cb\u003eperiod\u003c/b\u003e: 11/03/2013 --\u0026gt; 13/03/2013\u003c/p\u003e\u003cp\u003e\u003cb\u003ecareTeam\u003c/b\u003e: id: careteam\u003c/p\u003e\u003cp\u003e\u003cb\u003eaddresses\u003c/b\u003e: \u003ca\u003eRoel\u0027s renal insufficiency\u003c/a\u003e\u003c/p\u003e\u003cp\u003e\u003cb\u003egoal\u003c/b\u003e: id: goal; lifecycleStatus: completed; Achieved \u003cspan\u003e(Details : {http://terminology.hl7.org/CodeSystem/goal-achievement code \u0027achieved\u0027 \u003d \u0027Achieved\u0027, given as \u0027Achieved\u0027})\u003c/span\u003e; Re-established renal function with at least healthy nutrients. \u003cspan\u003e(Details )\u003c/span\u003e\u003c/p\u003e\u003cblockquote\u003e\u003cp\u003e\u003cb\u003eactivity\u003c/b\u003e\u003c/p\u003e\u003ch3\u003eDetails\u003c/h3\u003e\u003ctable\u003e\u003ctr\u003e\u003ctd\u003e-\u003c/td\u003e\u003ctd\u003e\u003cb\u003eKind\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e\u003cb\u003eCode\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e\u003cb\u003eStatus\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e\u003cb\u003eDoNotPerform\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e\u003cb\u003eScheduled[x]\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e\u003cb\u003eProduct[x]\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e\u003cb\u003eDailyAmount\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\u003ctr\u003e\u003ctd\u003e*\u003c/td\u003e\u003ctd\u003eNutritionOrder\u003c/td\u003e\u003ctd\u003ePotassium supplementation \u003cspan\u003e(Details : {SNOMED CT code \u0027284093001\u0027 \u003d \u0027Potassium supplementation\u0027, given as \u0027Potassium supplementation\u0027})\u003c/span\u003e\u003c/td\u003e\u003ctd\u003ecompleted\u003c/td\u003e\u003ctd\u003efalse\u003c/td\u003e\u003ctd\u003edaily\u003c/td\u003e\u003ctd\u003e\u003ca\u003ePotassium\u003c/a\u003e\u003c/td\u003e\u003ctd\u003e80 mmol\u003cspan\u003e (Details: SNOMED CT code 258718000 \u003d \u0027millimole\u0027)\u003c/span\u003e\u003c/td\u003e\u003c/tr\u003e\u003c/table\u003e\u003c/blockquote\u003e\u003cblockquote\u003e\u003cp\u003e\u003cb\u003eactivity\u003c/b\u003e\u003c/p\u003e\u003ch3\u003eDetails\u003c/h3\u003e\u003ctable\u003e\u003ctr\u003e\u003ctd\u003e-\u003c/td\u003e\u003ctd\u003e\u003cb\u003eKind\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e\u003cb\u003eCode\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e\u003cb\u003eStatus\u003c/b\u003e\u003c/td\u003e\u003ctd\u003e\u003cb\u003eDoNotPerform\u003c/b\u003e\u003c/td\u003e\u003c/tr\u003e\u003ctr\u003e\u003ctd\u003e*\u003c/td\u003e\u003ctd\u003eServiceRequest\u003c/td\u003e\u003ctd\u003eEchography of kidney \u003cspan\u003e(Details : {SNOMED CT code \u0027306005\u0027 \u003d \u0027Echography of kidney\u0027, given as \u0027Echography of kidney\u0027})\u003c/span\u003e\u003c/td\u003e\u003ctd\u003ecompleted\u003c/td\u003e\u003ctd\u003efalse\u003c/td\u003e\u003c/tr\u003e\u003c/table\u003e\u003c/blockquote\u003e\u003c/div\u003e""
          },
          ""contained"": [
            {
              ""resourceType"": ""CareTeam"",
              ""id"": ""careteam"",
              ""participant"": [
                {
                  ""role"": [
                    {
                      ""coding"": [
                        {
                          ""system"": ""http://snomed.info/sct"",
                          ""code"": ""425268008"",
                          ""display"": ""Review of care plan""
                        }
                      ]
                    }
                  ],
                  ""member"": {
                    ""reference"": ""Practitioner/f201"",
                    ""display"": ""Dokter Bronsig""
                  }
                },
                {
                  ""role"": [
                    {
                      ""coding"": [
                        {
                          ""system"": ""http://snomed.info/sct"",
                          ""code"": ""229774002"",
                          ""display"": ""Carer""
                        }
                      ]
                    }
                  ],
                  ""member"": {
                    ""reference"": ""Practitioner/f204"",
                    ""display"": ""Nurse Carla Espinosa""
                  }
                }
              ]
            },
            {
              ""resourceType"": ""Goal"",
              ""id"": ""goal"",
              ""lifecycleStatus"": ""completed"",
              ""achievementStatus"": {
                ""coding"": [
                  {
                    ""system"": ""http://terminology.hl7.org/CodeSystem/goal-achievement"",
                    ""code"": ""achieved"",
                    ""display"": ""Achieved""
                  }
                ],
                ""text"": ""Achieved""
              },
              ""description"": {
                ""text"": ""Re-established renal function with at least healthy nutrients.""
              },
              ""subject"": {
                ""reference"": ""Patient/f201"",
                ""display"": ""Roel""
              }
            }
          ],
          ""status"": ""draft"",
          ""intent"": ""proposal"",
          ""subject"": {
            ""reference"": ""Patient/f201"",
            ""display"": ""Roel""
          },
          ""period"": {
            ""start"": ""2013-03-11"",
            ""end"": ""2013-03-13""
          },
          ""careTeam"": [
            {
              ""reference"": ""#careteam""
            }
          ],
          ""addresses"": [
            {
              ""reference"": ""Condition/f204"",
              ""display"": ""Roel\u0027s renal insufficiency""
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
                ""kind"": ""NutritionOrder"",
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
                ""doNotPerform"": false,
                ""scheduledString"": ""daily"",
                ""productReference"": {
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
                ""kind"": ""ServiceRequest"",
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
                ""doNotPerform"": false
              }
            }
          ],
          ""meta"": {
            ""tag"": [
              {
                ""system"": ""http://terminology.hl7.org/CodeSystem/v3-ActReason"",
                ""code"": ""HTEST"",
                ""display"": ""test health data""
              }
            ]
          }
        }";

    }

    public  static class IndexValueTestExtensions
    {
        public static IEnumerable<IndexValue> NonInternalValues(this IndexValue root)
        {
            return root.IndexValues().Where(v => !v.Name.StartsWith("internal_"));
        }

    }
}
