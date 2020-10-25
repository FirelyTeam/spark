using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Web;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Spark.Engine;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Utility;
using Spark.Service;

namespace Spark.Web.Controllers
{
    [Route("fhir"), ApiController, EnableCors]
    public class FhirController : ControllerBase
    {
        private readonly IFhirService _fhirService;
        private readonly SparkSettings _settings;

        public FhirController(IFhirService fhirService, SparkSettings settings)
        {
            _fhirService = fhirService ?? throw new ArgumentNullException(nameof(fhirService));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        [HttpGet("{type}/{id}")]
        public ActionResult<FhirResponse> Read(string type, string id)
        {
            ConditionalHeaderParameters parameters = new ConditionalHeaderParameters(Request);
            Key key = Key.Create(type, id);
            return new ActionResult<FhirResponse>(_fhirService.Read(key, parameters));
        }

        [HttpGet("{type}/{id}/_history/{vid}")]
        public FhirResponse VRead(string type, string id, string vid)
        {
            Key key = Key.Create(type, id, vid);
            return _fhirService.VersionRead(key);
        }

        [HttpPut("{type}/{id?}")]
        public ActionResult<FhirResponse> Update(string type, Resource resource, string id = null)
        {
            string versionId = Request.GetTypedHeaders().IfMatch?.FirstOrDefault()?.Tag.Buffer;
            Key key = Key.Create(type, id, versionId);
            if(key.HasResourceId())
            {
                Request.TransferResourceIdIfRawBinary(resource, id);

                return new ActionResult<FhirResponse>(_fhirService.Update(key, resource));
            }
            else
            {
                return new ActionResult<FhirResponse>(_fhirService.ConditionalUpdate(key, resource,
                    SearchParams.FromUriParamList(Request.TupledParameters())));
            }
        }

        [HttpPost("{type}")]
        public FhirResponse Create(string type, Resource resource)
        {
            Key key = Key.Create(type, resource?.Id);

            if (Request.Headers.ContainsKey(FhirHttpHeaders.IfNoneExist))
            {
                NameValueCollection searchQueryString = HttpUtility.ParseQueryString(Request.GetTypedHeaders().IfNoneExist());
                IEnumerable<Tuple<string, string>> searchValues =
                    searchQueryString.Keys.Cast<string>()
                        .Select(k => new Tuple<string, string>(k, searchQueryString[k]));

                return _fhirService.ConditionalCreate(key, resource, SearchParams.FromUriParamList(searchValues));
            }

            return _fhirService.Create(key, resource);
        }

        [HttpDelete("{type}/{id}")]
        public FhirResponse Delete(string type, string id)
        {
            Key key = Key.Create(type, id);
            FhirResponse response = _fhirService.Delete(key);
            return response;
        }

        [HttpDelete("{type}")]
        public FhirResponse ConditionalDelete(string type)
        {
            Key key = Key.Create(type);
            return _fhirService.ConditionalDelete(key, Request.TupledParameters());
        }

        [HttpGet("{type}/{id}/_history")]
        public FhirResponse History(string type, string id)
        {
            Key key = Key.Create(type, id);
            var parameters = new HistoryParameters(Request);
            return _fhirService.History(key, parameters);
        }

        // ============= Validate

        [HttpPost("{type}/{id}/$validate")]
        public FhirResponse Validate(string type, string id, Resource resource)
        {
            Key key = Key.Create(type, id);
            return _fhirService.ValidateOperation(key, resource);
        }

        [HttpPost("{type}/$validate")]
        public FhirResponse Validate(string type, Resource resource)
        {
            Key key = Key.Create(type);
            return _fhirService.ValidateOperation(key, resource);
        }

        // ============= Type Level Interactions

        [HttpGet("{type}")]
        public FhirResponse Search(string type)
        {
            int start = FhirParameterParser.ParseIntParameter(Request.GetParameter(FhirParameter.SNAPSHOT_INDEX)) ?? 0;
            var searchparams = Request.GetSearchParams();
            //int pagesize = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
            //string sortby = Request.GetParameter(FhirParameter.SORT);

            return _fhirService.Search(type, searchparams, start);
        }

        [HttpPost("{type}/_search")]
        public FhirResponse SearchWithOperator(string type)
        {
            // TODO: start index should be retrieved from the body.
            int start = FhirParameterParser.ParseIntParameter(Request.GetParameter(FhirParameter.SNAPSHOT_INDEX)) ?? 0;
            SearchParams searchparams = Request.GetSearchParamsFromBody();

            return _fhirService.Search(type, searchparams, start);
        }

        [HttpGet("{type}/_history")]
        public FhirResponse History(string type)
        {
            var parameters = new HistoryParameters(Request);
            return _fhirService.History(type, parameters);
        }

        // ============= Whole System Interactions

        [HttpGet, Route("metadata")]
        public FhirResponse Metadata()
        {
            return _fhirService.CapabilityStatement(_settings.Version);
        }

        [HttpOptions, Route("")]
        public FhirResponse Options()
        {
            return _fhirService.CapabilityStatement(_settings.Version);
        }

        [HttpPost, Route("")]
        public FhirResponse Transaction(Bundle bundle)
        {
            return _fhirService.Transaction(bundle);
        }

        //[HttpPost, Route("Mailbox")]
        //public FhirResponse Mailbox(Bundle document)
        //{
        //    Binary b = Request.GetBody();
        //    return service.Mailbox(document, b);
        //}

        [HttpGet, Route("_history")]
        public FhirResponse History()
        {
            var parameters = new HistoryParameters(Request);
            return _fhirService.History(parameters);
        }

        [HttpGet, Route("_snapshot")]
        public FhirResponse Snapshot()
        {
            string snapshot = Request.GetParameter(FhirParameter.SNAPSHOT_ID);
            int start = FhirParameterParser.ParseIntParameter(Request.GetParameter(FhirParameter.SNAPSHOT_INDEX)) ?? 0;
            return _fhirService.GetPage(snapshot, start);
        }

        // Operations

        [HttpPost, Route("${operation}")]
        public FhirResponse ServerOperation(string operation)
        {
            switch (operation.ToLower())
            {
                case "error": throw new Exception("This error is for testing purposes");
                default: return Respond.WithError(HttpStatusCode.NotFound, "Unknown operation");
            }
        }

        [HttpPost, Route("{type}/{id}/${operation}")]
        public FhirResponse InstanceOperation(string type, string id, string operation, Parameters parameters)
        {
            Key key = Key.Create(type, id);
            switch (operation.ToLower())
            {
                case "meta": return _fhirService.ReadMeta(key);
                case "meta-add": return _fhirService.AddMeta(key, parameters);
                case "meta-delete":

                default: return Respond.WithError(HttpStatusCode.NotFound, "Unknown operation");
            }
        }

        [HttpPost, HttpGet, Route("{type}/{id}/$everything")]
        public FhirResponse Everything(string type, string id = null)
        {
            Key key = Key.Create(type, id);
            return _fhirService.Everything(key);
        }

        [HttpPost, HttpGet, Route("{type}/$everything")]
        public FhirResponse Everything(string type)
        {
            Key key = Key.Create(type);
            return _fhirService.Everything(key);
        }

        [HttpPost, HttpGet, Route("Composition/{id}/$document")]
        public FhirResponse Document(string id)
        {
            Key key = Key.Create("Composition", id);
            return _fhirService.Document(key);
        }
    }
}