using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Spark.Engine;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Service;
using Spark.Engine.Utility;

namespace Spark.Web.Controllers
{
    [Route("fhir"), ApiController, EnableCors]
    public class FhirController : ControllerBase
    {
        private readonly IAsyncFhirService _fhirService;
        private readonly SparkSettings _settings;

        public FhirController(IAsyncFhirService fhirService, SparkSettings settings)
        {
            _fhirService = fhirService ?? throw new ArgumentNullException(nameof(fhirService));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        [HttpGet("{type}/{id}")]
        public async Task<ActionResult<FhirResponse>> Read(string type, string id)
        {
            ConditionalHeaderParameters parameters = new ConditionalHeaderParameters(Request);
            Key key = Key.Create(type, id);
            var response = await _fhirService.ReadAsync(key, parameters).ConfigureAwait(false);
            return new ActionResult<FhirResponse>(response);
        }

        [HttpGet("{type}/{id}/_history/{vid}")]
        public async Task<FhirResponse> VRead(string type, string id, string vid)
        {
            Key key = Key.Create(type, id, vid);
            return await _fhirService.VersionReadAsync(key).ConfigureAwait(false);
        }

        [HttpPut("{type}/{id?}")]
        public async Task<ActionResult<FhirResponse>> Update(string type, Resource resource, string id = null)
        {
            string versionId = Request.GetTypedHeaders().IfMatch?.FirstOrDefault()?.Tag.Buffer;
            Key key = Key.Create(type, id, versionId);
            if(key.HasResourceId())
            {
                Request.TransferResourceIdIfRawBinary(resource, id);

                return new ActionResult<FhirResponse>(await _fhirService.UpdateAsync(key, resource).ConfigureAwait(false));
            }
            else
            {
                return new ActionResult<FhirResponse>(await _fhirService.ConditionalUpdateAsync(key, resource,
                    SearchParams.FromUriParamList(Request.TupledParameters())).ConfigureAwait(false));
            }
        }

        [HttpPost("{type}")]
        public async Task<FhirResponse> Create(string type, Resource resource)
        {
            Key key = Key.Create(type, resource?.Id);

            if (Request.Headers.ContainsKey(FhirHttpHeaders.IfNoneExist))
            {
                NameValueCollection searchQueryString = HttpUtility.ParseQueryString(Request.GetTypedHeaders().IfNoneExist());
                IEnumerable<Tuple<string, string>> searchValues =
                    searchQueryString.Keys.Cast<string>()
                        .Select(k => new Tuple<string, string>(k, searchQueryString[k]));

                return await _fhirService.ConditionalCreateAsync(key, resource, SearchParams.FromUriParamList(searchValues)).ConfigureAwait(false);
            }

            return await _fhirService.CreateAsync(key, resource).ConfigureAwait(false);
        }

        [HttpDelete("{type}/{id}")]
        public async Task<FhirResponse> Delete(string type, string id)
        {
            Key key = Key.Create(type, id);
            FhirResponse response = await _fhirService.DeleteAsync(key).ConfigureAwait(false);
            return response;
        }

        [HttpDelete("{type}")]
        public async Task<FhirResponse> ConditionalDelete(string type)
        {
            Key key = Key.Create(type);
            return await _fhirService.ConditionalDeleteAsync(key, Request.TupledParameters()).ConfigureAwait(false);
        }

        [HttpGet("{type}/{id}/_history")]
        public async Task<FhirResponse> History(string type, string id)
        {
            Key key = Key.Create(type, id);
            var parameters = new HistoryParameters(Request);
            return await _fhirService.HistoryAsync(key, parameters).ConfigureAwait(false);
        }

        // ============= Validate

        [HttpPost("{type}/{id}/$validate")]
        public async Task<FhirResponse> Validate(string type, string id, Resource resource)
        {
            Key key = Key.Create(type, id);
            return await _fhirService.ValidateOperationAsync(key, resource).ConfigureAwait(false);
        }

        [HttpPost("{type}/$validate")]
        public async Task<FhirResponse> Validate(string type, Resource resource)
        {
            Key key = Key.Create(type);
            return await _fhirService.ValidateOperationAsync(key, resource).ConfigureAwait(false);
        }

        // ============= Type Level Interactions

        [HttpGet("{type}")]
        public async Task<FhirResponse> Search(string type)
        {
            int start = FhirParameterParser.ParseIntParameter(Request.GetParameter(FhirParameter.SNAPSHOT_INDEX)) ?? 0;
            var searchparams = Request.GetSearchParams();
            //int pagesize = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
            //string sortby = Request.GetParameter(FhirParameter.SORT);

            return await _fhirService.SearchAsync(type, searchparams, start).ConfigureAwait(false);
        }

        [HttpPost("{type}/_search")]
        public async Task<FhirResponse> SearchWithOperator(string type)
        {
            // TODO: start index should be retrieved from the body.
            int start = FhirParameterParser.ParseIntParameter(Request.GetParameter(FhirParameter.SNAPSHOT_INDEX)) ?? 0;
            SearchParams searchparams = Request.GetSearchParamsFromBody();

            return await _fhirService.SearchAsync(type, searchparams, start).ConfigureAwait(false);
        }

        [HttpGet("{type}/_history")]
        public async Task<FhirResponse> History(string type)
        {
            var parameters = new HistoryParameters(Request);
            return await _fhirService.HistoryAsync(type, parameters).ConfigureAwait(false);
        }

        // ============= Whole System Interactions

        [HttpGet, Route("metadata")]
        public async Task<FhirResponse> Metadata()
        {
            return await _fhirService.CapabilityStatementAsync(_settings.Version).ConfigureAwait(false);
        }

        [HttpOptions, Route("")]
        public async Task<FhirResponse> Options()
        {
            return await _fhirService.CapabilityStatementAsync(_settings.Version).ConfigureAwait(false);
        }

        [HttpPost, Route("")]
        public async Task<FhirResponse> Transaction(Bundle bundle)
        {
            return await _fhirService.TransactionAsync(bundle).ConfigureAwait(false);
        }

        //[HttpPost, Route("Mailbox")]
        //public FhirResponse Mailbox(Bundle document)
        //{
        //    Binary b = Request.GetBody();
        //    return service.Mailbox(document, b);
        //}

        [HttpGet, Route("_history")]
        public async Task<FhirResponse> History()
        {
            var parameters = new HistoryParameters(Request);
            return await _fhirService.HistoryAsync(parameters).ConfigureAwait(false);
        }

        [HttpGet, Route("_snapshot")]
        public async Task<FhirResponse> Snapshot()
        {
            string snapshot = Request.GetParameter(FhirParameter.SNAPSHOT_ID);
            int start = FhirParameterParser.ParseIntParameter(Request.GetParameter(FhirParameter.SNAPSHOT_INDEX)) ?? 0;
            return await _fhirService.GetPageAsync(snapshot, start).ConfigureAwait(false);
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
        public async Task<FhirResponse> InstanceOperation(string type, string id, string operation, Parameters parameters)
        {
            Key key = Key.Create(type, id);
            switch (operation.ToLower())
            {
                case "meta": return await _fhirService.ReadMetaAsync(key).ConfigureAwait(false);
                case "meta-add": return await _fhirService.AddMetaAsync(key, parameters).ConfigureAwait(false);
                case "meta-delete":

                default: return Respond.WithError(HttpStatusCode.NotFound, "Unknown operation");
            }
        }

        [HttpPost, HttpGet, Route("{type}/{id}/$everything")]
        public async Task<FhirResponse> Everything(string type, string id = null)
        {
            Key key = Key.Create(type, id);
            return await _fhirService.EverythingAsync(key).ConfigureAwait(false);
        }

        [HttpPost, HttpGet, Route("{type}/$everything")]
        public async Task<FhirResponse> Everything(string type)
        {
            Key key = Key.Create(type);
            return await _fhirService.EverythingAsync(key).ConfigureAwait(false);
        }

        [HttpPost, HttpGet, Route("Composition/{id}/$document")]
        public async Task<FhirResponse> Document(string id)
        {
            Key key = Key.Create("Composition", id);
            return await _fhirService.DocumentAsync(key).ConfigureAwait(false);
        }
    }
}