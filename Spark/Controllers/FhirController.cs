/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using System.Net.Http;
using System.Net.Http.Headers;
using Spark.Core;
using Spark.Service;
using Spark.Config;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Model;
using Spark.Store;
using Spark.Support;


namespace Spark.Controllers
{
    [RoutePrefix("fhir"), EnableCors(origins: "*", headers: "*", methods: "*", exposedHeaders: "*")]
    public class FhirController : ApiController
    {
        FhirService service; 

        public FhirController()
        {
            service = Factory.GetFhirService();
        }

        [HttpGet, Route("{type}/{id}")]
        public FhirResponse Read(string type, string id)
        {
            Key key = Key.CreateLocal(type, id);
            return service.Read(key);
        }
        
        [HttpGet, Route("{type}/{id}/_history/{vid}")]
        public FhirResponse VRead(string type, string id, string vid)
        {
            Key key = Key.CreateLocal(type, id, vid);
            return service.VRead(key);
        }

        [HttpPut, Route("{type}/{id}")]
        public FhirResponse Upsert(string type, string id, Resource resource)
        {
            // DSTU2: tags
            //entry.Tags = Request.GetFhirTags(); // todo: move to model binder?

            string versionid = Request.IfMatchVersionId();
            Key key = Key.CreateLocal(type, id, versionid);
            return service.Upsert(key, resource);
        }

        [HttpPost, Route("{type}")]
        public FhirResponse Create(string type, Resource resource)
        {
            //entry.Tags = Request.GetFhirTags(); // todo: move to model binder?
            Key key = Key.CreateLocal(type);
            return service.Create(key, resource);
        }

        [HttpDelete, Route("{type}/{id}")]
        public FhirResponse Delete(string type, string id)
        {
            Key key = Key.CreateLocal(type, id);
            FhirResponse response = service.Delete(key);
            return response;
        }

        [HttpDelete, Route("{type}")] 
        public FhirResponse ConditionalDelete(string type)
        {
            Key key = Key.CreateLocal(type);
            return service.ConditionalDelete(key, Request.TupledParameters());
        }

        [HttpGet, Route("{type}/{id}/_history")]
        public FhirResponse History(string type, string id)
        {
            Key key = Key.CreateLocal(type, id);
            DateTimeOffset? since = Request.GetDateParameter(FhirParameter.SINCE);
            string sortby = Request.GetParameter(FhirParameter.SORT);
            return service.History(key, since, sortby);
        }

        // ============= Validate
        [HttpPost, Route("{type}/{id}/$validate")]
        public FhirResponse Validate(string type, string id, Resource resource)
        {
            //entry.Tags = Request.GetFhirTags();
            Key key = Key.CreateLocal(type, id);
            return service.ValidateOperation(key, resource);
        }

        [HttpPost, Route("{type}/$validate")]
        public FhirResponse Validate(string type, Resource resource)
        {
            // DSTU2: tags
            //entry.Tags = Request.GetFhirTags();
            Key key = Key.CreateLocal(type);
            return service.ValidateOperation(key, resource);
        }
        
        // ============= Type Level Interactions

        [HttpGet, Route("{type}")]
        public FhirResponse Search(string type)
        {
            var parameters = Request.TupledParameters();
            int pagesize = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
            bool summary = Request.GetBooleanParameter(FhirParameter.SUMMARY) ?? false;
            string sortby = Request.GetParameter(FhirParameter.SORT);
            // On implementing _summary: this has to be done at two different abstraction layers:
            // a) The serialization (which is the formatter in WebApi2 needs to call the serializer with a _summary param
            // b) The service needs to generate self/paging links which retain the _summary parameter
            // This is all still todo ;-)
            return service.Search(type, parameters, pagesize, sortby);
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
            return service.Conformance();
        }

        [HttpOptions, Route("")]
        public FhirResponse Options()
        {
            return service.Conformance();
        }

        [HttpPost, Route("")]
        public FhirResponse Transaction(Bundle bundle)
        {
            return service.Transaction(bundle);
        }

        [HttpPost, Route("Mailbox")]
        public FhirResponse Mailbox(Bundle document)
        {
            Binary b = Request.GetBody();
            return service.Mailbox(document, b);
        }
        
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
            return service.GetSnapshot(snapshot, start, count);
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
