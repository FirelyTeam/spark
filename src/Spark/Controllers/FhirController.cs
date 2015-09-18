using Hl7.Fhir.Model;
using Spark.Configuration;
using Spark.Core;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Spark.Controllers
{
    [RoutePrefix("fhir"), EnableCors("*", "*", "*", "*")]
    public class FhirController : ApiController
    {
        FhirService service;

        public FhirController()
        {
            // This will be a (injected) constructor parameter in ASP.vNext.
            service = new FhirService(InfrastructureProvider.Mongo);
        }

        [HttpGet, Route("{type}/{id}")]
        public FhirResponse Read(string type, string id)
        {
            Key key = Key.Create(type, id);
            FhirResponse response = service.Read(key);

            return response;
        }

        [HttpGet, Route("{type}/{id}/_history/{vid}")]
        public FhirResponse VRead(string type, string id, string vid)
        {
            Key key = Key.Create(type, id, vid);
            return service.VersionRead(key);
        }

        [HttpPut, Route("{type}/{id}")]
        public FhirResponse Update(string type, string id, Resource resource)
        {
            string versionid = Request.IfMatchVersionId();
            Key key = Key.Create(type, id, versionid);
            return service.Update(key, resource);
        }

        [HttpPost, Route("{type}")]
        public FhirResponse Create(string type, Resource resource)
        {
            //entry.Tags = Request.GetFhirTags(); // todo: move to model binder?
            Key key = Key.Create(type);
            return service.Create(key, resource);
        }

        [HttpDelete, Route("{type}/{id}")]
        public FhirResponse Delete(string type, string id)
        {
            Key key = Key.Create(type, id);
            FhirResponse response = service.Delete(key);
            return response;
        }

        [HttpDelete, Route("{type}")]
        public FhirResponse ConditionalDelete(string type)
        {
            Key key = Key.Create(type);
            return service.ConditionalDelete(key, Request.TupledParameters());
        }

        [HttpGet, Route("{type}/{id}/_history")]
        public FhirResponse History(string type, string id)
        {
            Key key = Key.Create(type, id);
            DateTimeOffset? since = Request.GetDateParameter(FhirParameter.SINCE);
            string sortby = Request.GetParameter(FhirParameter.SORT);
            return service.History(key, since, sortby);
        }

        // ============= Validate
        [HttpPost, Route("{type}/{id}/$validate")]
        public FhirResponse Validate(string type, string id, Resource resource)
        {
            //entry.Tags = Request.GetFhirTags();
            Key key = Key.Create(type, id);
            return service.ValidateOperation(key, resource);
        }

        [HttpPost, Route("{type}/$validate")]
        public FhirResponse Validate(string type, Resource resource)
        {
            // DSTU2: tags
            //entry.Tags = Request.GetFhirTags();
            Key key = Key.Create(type);
            return service.ValidateOperation(key, resource);
        }

        // ============= Type Level Interactions

        [HttpGet, Route("{type}")]
        public FhirResponse Search(string type)
        {
            var searchparams = Request.GetSearchParams();
            //int pagesize = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
            //string sortby = Request.GetParameter(FhirParameter.SORT);

            return service.Search(type, searchparams);
        }

        [HttpPost, Route("{type}/_search")]
        public FhirResponse SearchWithOperator(string type)
        {
            // todo: get tupled parameters from post.
            return Search(type);
        }

        [HttpGet, Route("{type}/_history")]
        public FhirResponse History(string type)
        {
            DateTimeOffset? since = Request.GetDateParameter(FhirParameter.SINCE);
            string sortby = Request.GetParameter(FhirParameter.SORT);
            string summary = Request.GetParameter("_summary");
            return service.History(type, since, sortby);
        }

        // ============= Whole System Interactions

        [HttpGet, Route("metadata")]
        public FhirResponse Metadata()
        {
            return Respond.WithResource(Factory.GetSparkConformance());
        }

        [HttpOptions, Route("")]
        public FhirResponse Options()
        {
            return Respond.WithResource(Factory.GetSparkConformance());
        }

        [HttpPost, Route("")]
        public FhirResponse Transaction(Bundle bundle)
        {
            return service.Transaction(bundle);
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
            DateTimeOffset? since = Request.GetDateParameter(FhirParameter.SINCE);
            string sortby = Request.GetParameter(FhirParameter.SORT);
            return service.History(since, sortby);
        }

        [HttpGet, Route("_snapshot")]
        public FhirResponse Snapshot()
        {
            string snapshot = Request.GetParameter(FhirParameter.SNAPSHOT_ID);
            int start = Request.GetIntParameter(FhirParameter.SNAPSHOT_INDEX) ?? 0;
            int count = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
            return service.GetPage(snapshot, start, count);
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
                case "meta": return service.ReadMeta(key);
                case "meta-add": return service.AddMeta(key, parameters);
                case "meta-delete":
                case "document":
                case "$everything": // patient

                default: return Respond.WithError(HttpStatusCode.NotFound, "Unknown operation");
            }
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
