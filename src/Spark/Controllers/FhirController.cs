using Hl7.Fhir.Model;
using Microsoft.Practices.Unity;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Service;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using Hl7.Fhir.Rest;
using Spark.Core;
using Spark.Infrastructure;
using Spark.Engine.Utility;

namespace Spark.Controllers
{
    [RoutePrefix("fhir"), EnableCors("*", "*", "*", "*")]
    [RouteDataValuesOnly]
    public class FhirController : ApiController
    {
        readonly IFhirService _fhirService;

        [InjectionConstructor]
        public FhirController(IFhirService fhirService)
        {
            // This will be a (injected) constructor parameter in ASP.vNext.
            _fhirService = fhirService;
        }

        [HttpGet, Route("{type}/{id}")]
        public FhirResponse Read(string type, string id)
        {
            ConditionalHeaderParameters parameters = new ConditionalHeaderParameters(Request);
            Key key = Key.Create(type, id);
            FhirResponse response = _fhirService.Read(key, parameters);

            return response;
        }

        [HttpGet, Route("{type}/{id}/_history/{vid}")]
        public FhirResponse VRead(string type, string id, string vid)
        {
            Key key = Key.Create(type, id, vid);
            return _fhirService.VersionRead(key);
        }

        [HttpPut, Route("{type}/{id?}")]
        public FhirResponse Update(string type, Resource resource, string id = null)
        {
            string versionid = Request.IfMatchVersionId();
            Key key = Key.Create(type, id, versionid);
            if (key.HasResourceId())
            {
                Request.TransferResourceIdIfRawBinary(resource, id);

                return _fhirService.Update(key, resource);
            }
            else
            {
                return _fhirService.ConditionalUpdate(key, resource,
                    SearchParams.FromUriParamList(Request.TupledParameters()));
            }
        }

        [HttpPost, Route("{type}")]
        public FhirResponse Create(string type, Resource resource)
        {
            Key key = Key.Create(type, resource?.Id);

            if (Request.Headers.Exists(FhirHttpHeaders.IfNoneExist))
            {
                NameValueCollection searchQueryString =
                    HttpUtility.ParseQueryString(
                        Request.Headers.First(h => h.Key == FhirHttpHeaders.IfNoneExist).Value.Single());
                IEnumerable<Tuple<string, string>> searchValues =
                    searchQueryString.Keys.Cast<string>()
                        .Select(k => new Tuple<string, string>(k, searchQueryString[k]));


                return _fhirService.ConditionalCreate(key, resource, SearchParams.FromUriParamList(searchValues));
            }

            //entry.Tags = Request.GetFhirTags(); // todo: move to model binder?

            return _fhirService.Create(key, resource);
        }

        [HttpDelete, Route("{type}/{id}")]
        public FhirResponse Delete(string type, string id)
        {
            Key key = Key.Create(type, id);
            FhirResponse response = _fhirService.Delete(key);
            return response;
        }

        [HttpDelete, Route("{type}")]
        public FhirResponse ConditionalDelete(string type)
        {
            Key key = Key.Create(type);
            return _fhirService.ConditionalDelete(key, Request.TupledParameters());
        }

        [HttpGet, Route("{type}/{id}/_history")]
        public FhirResponse History(string type, string id)
        {
            Key key = Key.Create(type, id);
            var parameters = new HistoryParameters(Request);
            return _fhirService.History(key, parameters);
        }

        // ============= Validate
        [HttpPost, Route("{type}/{id}/$validate")]
        public FhirResponse Validate(string type, string id, Resource resource)
        {
            //entry.Tags = Request.GetFhirTags();
            Key key = Key.Create(type, id);
            return _fhirService.ValidateOperation(key, resource);
        }

        [HttpPost, Route("{type}/$validate")]
        public FhirResponse Validate(string type, Resource resource)
        {
            // DSTU2: tags
            //entry.Tags = Request.GetFhirTags();
            Key key = Key.Create(type);
            return _fhirService.ValidateOperation(key, resource);
        }

        // ============= Type Level Interactions

        [HttpGet, Route("{type}")]
        public FhirResponse Search(string type)
        {
            int start = FhirParameterParser.ParseIntParameter(Request.GetParameter(FhirParameter.SNAPSHOT_INDEX)) ?? 0;
            var searchparams = Request.GetSearchParams();
            //int pagesize = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
            //string sortby = Request.GetParameter(FhirParameter.SORT);

            return _fhirService.Search(type, searchparams, start);
        }

        [HttpPost, Route("{type}/_search")]
        public FhirResponse SearchWithOperator(string type)
        {
            // TODO: start index should be retrieved from the body.
            int start = FhirParameterParser.ParseIntParameter(Request.GetParameter(FhirParameter.SNAPSHOT_INDEX)) ?? 0;
            SearchParams searchparams = Request.GetSearchParamsFromBody();

            return _fhirService.Search(type, searchparams, start);
        }

        [HttpGet, Route("{type}/_history")]
        public FhirResponse History(string type)
        {
            var parameters = new HistoryParameters(Request);
            return _fhirService.History(type, parameters);
        }

        // ============= Whole System Interactions

        [HttpGet, Route("metadata")]
        public FhirResponse Metadata()
        {
            return _fhirService.CapabilityStatement(Settings.Version);
        }

        [HttpOptions, Route("")]
        public FhirResponse Options()
        {
            return _fhirService.CapabilityStatement(Settings.Version);
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

        // ============= Tag Interactions

        /*
        [HttpGet, Route("_tags")]
        public TagList AllTags()
        {
            return service.TagsFromServer();
        }

        [HttpGet, Route("{type}/_tags")]
        public TagList ResourceTags(string type)
        {
            return service.TagsFromResource(type);
        }

        [HttpGet, Route("{type}/{id}/_tags")]
        public TagList InstanceTags(string type, string id)
        {
            return service.TagsFromInstance(type, id);
        }

        [HttpGet, Route("{type}/{id}/_history/{vid}/_tags")]
        public HttpResponseMessage HistoryTags(string type, string id, string vid)
        {
            TagList tags = service.TagsFromHistory(type, id, vid);
            return Request.CreateResponse(HttpStatusCode.OK, tags);
        }

        [HttpPost, Route("{type}/{id}/_tags")]
        public HttpResponseMessage AffixTag(string type, string id, TagList taglist)
        {
            service.AffixTags(type, id, taglist != null ? taglist.Category : null);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost, Route("{type}/{id}/_history/{vid}/_tags")]
        public HttpResponseMessage AffixTag(string type, string id, string vid, TagList taglist)
        {
            service.AffixTags(type, id, vid, taglist != null ? taglist.Category : null);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost, Route("{type}/{id}/_tags/_delete")]
        public HttpResponseMessage DeleteTags(string type, string id, TagList taglist)
        {
            service.RemoveTags(type, id, taglist != null ? taglist.Category : null);
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }

        [HttpPost, Route("{type}/{id}/_history/{vid}/_tags/_delete")]
        public HttpResponseMessage DeleteTags(string type, string id, string vid, TagList taglist)
        {
            service.RemoveTags(type, id, vid, taglist != null ? taglist.Category : null);
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }
        */

    }

}
