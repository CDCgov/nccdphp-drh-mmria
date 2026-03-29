#nullable enable

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mmria.case_version.v260120;
using mmria.common.couchdb;
using mmria.common.getset;
using mmria.common.SharedLibraries.Case.DAL;
using mmria.common.utils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace mmria_server.tests.Tests;

[TestFixture]
public sealed class CaseSerializationContractTests
{
    [Test]
    public void DeserializeMmriaCase_ParsesScalarTimeValues()
    {
        var result = CaseJsonSerialization.DeserializeMmriaCase(CreateScalarCaseJson());

        Assert.That(result.birth_certificate_infant_fetal_section, Is.Not.Null.And.Count.EqualTo(1));
        Assert.That(result.birth_certificate_infant_fetal_section[0].record_identification, Is.Not.Null);
        Assert.That(result.birth_certificate_infant_fetal_section[0].record_identification.time_of_delivery, Is.EqualTo(TimeOnly.Parse("07:48:00")));
        Assert.That(result.er_visit_and_hospital_medical_records, Is.Not.Null.And.Count.EqualTo(1));
        Assert.That(result.er_visit_and_hospital_medical_records[0].basic_admission_and_discharge_information.date_of_hospital_admission.time_of_admission, Is.EqualTo(TimeOnly.Parse("08:16:00")));
    }

    [Test]
    public void DeserializeMmriaCase_ParsesLegacyArrayShapedTimeFields()
    {
        var result = CaseJsonSerialization.DeserializeMmriaCase(CreateLegacyArrayCaseJson());

        Assert.That(result.birth_certificate_infant_fetal_section, Is.Not.Null.And.Count.EqualTo(1));
        Assert.That(result.birth_certificate_infant_fetal_section[0].record_identification.time_of_delivery, Is.EqualTo(TimeOnly.Parse("07:48")));
        Assert.That(result.er_visit_and_hospital_medical_records, Is.Not.Null.And.Count.EqualTo(1));
        Assert.That(result.er_visit_and_hospital_medical_records[0].basic_admission_and_discharge_information.date_of_hospital_admission.time_of_admission, Is.EqualTo(TimeOnly.Parse("08:16")));
        Assert.That(result.er_visit_and_hospital_medical_records[0].basic_admission_and_discharge_information.date_of_hospital_discharge.time_of_discharge, Is.EqualTo(TimeOnly.Parse("08:39")));
    }

    [Test]
    public void DeserializeMmriaCase_DegradesMalformedObjectStringTimeValuesToNull()
    {
        var result = CaseJsonSerialization.DeserializeMmriaCase(CreateMalformedStringCaseJson());

        Assert.That(result.other_medical_office_visits, Is.Not.Null.And.Count.EqualTo(1));
        Assert.That(result.other_medical_office_visits[0].visit.date_of_medical_office_visit.arrival_time, Is.Null);
    }

    [Test]
    public void SerializeMmriaCase_WritesScalarTimeStringsWithoutLegacyArrayShape()
    {
        var caseDoc = CaseJsonSerialization.DeserializeMmriaCase(CreateScalarCaseJson());

        var json = CaseJsonSerialization.SerializeMmriaCase(caseDoc);
        var payload = JObject.Parse(json);

        Assert.That(payload.ToString(), Does.Not.Contain("\"Item1\""));
        Assert.That(payload.ToString(), Does.Not.Contain("\"Item2\""));
        Assert.That(payload["birth_certificate_infant_fetal_section"]?[0]?["record_identification"]?["time_of_delivery"]?.Type, Is.EqualTo(JTokenType.String));
        Assert.That(payload["birth_certificate_infant_fetal_section"]?[0]?["record_identification"]?["time_of_delivery"]?.ToString(), Is.EqualTo("07:48:00"));
        Assert.That(payload["er_visit_and_hospital_medical_records"]?[0]?["basic_admission_and_discharge_information"]?["date_of_hospital_discharge"]?["time_of_discharge"]?.ToString(), Is.EqualTo("08:39:00"));
    }

    [Test]
    public async Task CaseDalGetCaseAsync_UsesSharedCompatibilityDeserializer()
    {
        var handler = new RecordingHttpMessageHandler(request =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            return Task.FromResult(CreateJsonResponse(CreateLegacyArrayCaseJson()));
        });

        using var httpClient = new HttpClient(handler);
        var couchDbClient = new CouchDbHttpClient(new FixedHttpClientFactory(httpClient));
        var dal = new CaseDAL(couchDbClient);

        var result = await dal.GetCaseAsync("case-1", CreateDbConfig());

        Assert.That(result, Is.Not.Null);
        Assert.That(result.birth_certificate_infant_fetal_section[0].record_identification.time_of_delivery, Is.EqualTo(TimeOnly.Parse("07:48")));
    }

    [Test]
    public async Task CaseDalUpdateCaseAsync_UsesSharedCanonicalSerializer()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new RecordingHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = request.Content == null ? null : await request.Content.ReadAsStringAsync();
            return CreateJsonResponse(@"{ ""ok"": true, ""id"": ""case-1"", ""rev"": ""2-test"" }");
        });

        using var httpClient = new HttpClient(handler);
        var couchDbClient = new CouchDbHttpClient(new FixedHttpClientFactory(httpClient));
        var dal = new CaseDAL(couchDbClient);
        var caseDoc = CaseJsonSerialization.DeserializeMmriaCase(CreateLegacyArrayCaseJson());

        var result = await dal.UpdateCaseAsync("case-1", caseDoc, CreateDbConfig());

        Assert.That(result.ok, Is.True);
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Method, Is.EqualTo(HttpMethod.Put));
        Assert.That(capturedBody, Is.Not.Null.And.Not.Empty);
        Assert.That(capturedBody, Does.Contain("\"time_of_delivery\":\"07:48:00\""));
        Assert.That(capturedBody, Does.Contain("\"time_of_admission\":\"08:16:00\""));
        Assert.That(capturedBody, Does.Not.Contain("\"Item1\""));
        Assert.That(capturedBody, Does.Not.Contain("\"Item2\""));
        Assert.That(capturedBody, Does.Not.Contain("\"hour\""));
        Assert.That(capturedBody, Does.Not.Contain("\"minute\""));
    }

    private static DBConfigurationDetail CreateDbConfig()
    {
        return new DBConfigurationDetail
        {
            url = "https://couch.example",
            prefix = "",
            user_name = "tester",
            user_value = "secret"
        };
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static string CreateScalarCaseJson()
    {
        return """
        {
          "_id": "case-1",
          "birth_certificate_infant_fetal_section": [
            {
              "record_identification": {
                "time_of_delivery": "07:48:00"
              }
            }
          ],
          "er_visit_and_hospital_medical_records": [
            {
              "basic_admission_and_discharge_information": {
                "date_of_hospital_admission": {
                  "time_of_admission": "08:16:00"
                },
                "date_of_hospital_discharge": {
                  "time_of_discharge": "08:39:00"
                }
              }
            }
          ]
        }
        """;
    }

    private static string CreateLegacyArrayCaseJson()
    {
        return """
        {
          "_id": "case-1",
          "birth_certificate_infant_fetal_section": [
            {
              "record_identification": {
                "time_of_delivery": [
                  { "Item1": 0, "Item2": "7:48" }
                ]
              }
            }
          ],
          "er_visit_and_hospital_medical_records": [
            {
              "basic_admission_and_discharge_information": {
                "date_of_hospital_admission": {
                  "time_of_admission": [
                    { "Item1": 0, "Item2": "8:16" }
                  ]
                },
                "date_of_hospital_discharge": {
                  "time_of_discharge": [
                    { "Item1": 0, "Item2": "8:39" }
                  ]
                }
              }
            }
          ]
        }
        """;
    }

    private static string CreateMalformedStringCaseJson()
    {
        return """
        {
          "_id": "case-1",
          "other_medical_office_visits": [
            {
              "visit": {
                "date_of_medical_office_visit": {
                  "arrival_time": "[object Object]"
                }
              }
            }
          ]
        }
        """;
    }

    private sealed class FixedHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FixedHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name)
        {
            return _client;
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responder;

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _responder(request);
        }
    }
}
