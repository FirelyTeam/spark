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
using Spark.Http;
using Spark.Config;

using Hl7.Fhir.Rest;
using Hl7.Fhir.Model;


namespace Spark.Controllers
{
    [RoutePrefix("fhir")]
    [EnableCors("*","*","*","*")]
    public class FhirController : ApiController
    {
        FhirService service; 
        public FhirController()
        {
            // todo: in case we have more Fhir controllers, we want each to have a different endpoint (base).
            // Currently we only have one global base. But how do we get the base, before we have a request context.
            // We want to inject the base into the FhirService.
            
            service = DependencyCoupler.Inject<FhirService>();
        }

        // ============= Instance Level Interactions
       
        [HttpGet, Route("{type}/{id}")] 
        public HttpResponseMessage Read(string type, string id)
        {
            Key key = Key.CreateLocal(type, id);
            Response response = service.Read(key);
            
            return Request.CreateFhirResponse(response);
        }
        
        [HttpGet, Route("{type}/{id}/_history/{vid}")]
        public HttpResponseMessage VRead(string type, string id, string vid)
        {
            Key key = Key.CreateLocal(type, id, vid);
            Response response = service.VRead(key);
            return Request.CreateFhirResponse(response);
        }

        [HttpPut, Route("{type}/{id}")]
        public HttpResponseMessage Upsert(string type, string id, Resource resource)
        {
            Key key = Key.CreateLocal(type, id);

            // DSTU2: tags
            //entry.Tags = Request.GetFhirTags(); // todo: move to model binder?
            Response response = service.Upsert(key, resource);
            return Request.CreateFhirResponse(response);
        }

        [HttpPost, Route("{type}")]
        public HttpResponseMessage Create(string type, Resource resource)
        {
            //entry.Tags = Request.GetFhirTags(); // todo: move to model binder?
            Key key = Key.CreateLocal(type);
            Response response = service.Create(key, resource);
            return Request.CreateFhirResponse(response);
        }
        
        [Route("{type}/{id}")]
        public HttpResponseMessage Delete(string type, string id)
        {
            Key key = Key.CreateLocal(type, id);
            service.Delete(key);
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }

        [HttpGet, Route("{type}/{id}/_history")]
        public HttpResponseMessage History(string type, string id)
        {
            Key key = Key.CreateLocal(type, id);
            DateTimeOffset? since = Request.GetDateParameter(FhirParameter.SINCE);
            string sortby = Request.GetParameter(FhirParameter.SORT);

            Response response = service.History(key, since, sortby);
            return Request.CreateFhirResponse(response);
        }


        // ============= Validate
        [HttpPost, Route("{type}/_validate/{id}")]
        public HttpResponseMessage Validate(string type, string id, Resource resource)
        {
            //entry.Tags = Request.GetFhirTags();
            Key key = Key.CreateLocal(type, id);
            Response response = service.Validate(key, resource);
            return Request.CreateFhirResponse(response);
        }

        [HttpPost, Route("{type}/_validate")]
        public HttpResponseMessage Validate(string type, Resource resource)
        {
            // DSTU2: tags
            //entry.Tags = Request.GetFhirTags();
            Key key = Key.CreateLocal(type);
            Response response = service.Validate(key, resource);
            return Request.CreateFhirResponse(response);
        }
        
        // ============= Type Level Interactions

        /*
        // According to the spec, this interaction should not exist, so I commented it.
        [HttpPost, Route("{type}/{id}")]
        public HttpResponseMessage Create(string type, string id, ResourceEntry entry)
        {
            entry.Tags = Request.GetFhirTags(); // todo: move to model binder?

            ResourceEntry newentry = service.Create(type, entry, id);
            return Request.StatusResponse(newentry, HttpStatusCode.Created);
        }
        */

        [HttpGet, Route("{type}")]
        public HttpResponseMessage Search(string type)
        {
            var parameters = Request.TupledParameters();
            int pagesize = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
            bool summary = Request.GetBooleanParameter(FhirParameter.SUMMARY) ?? false;
            string sortby = Request.GetParameter(FhirParameter.SORT);
            // On implementing _summary: this has to be done at two different abstraction layers:
            // a) The serialization (which is the formatter in WebApi2 needs to call the serializer with a _summary param
            // b) The service needs to generate self/paging links which retain the _summary parameter
            // This is all still todo ;-)
            Response response = service.Search(type, parameters, pagesize, sortby);
            return Request.CreateFhirResponse(response);
        }

        [HttpGet, Route("{type}/_search")]
        public HttpResponseMessage SearchWithOperator(string type)
        {
            return Search(type);
        }

        [HttpGet, Route("{type}/_history")]
        public HttpResponseMessage History(string type)
        {
            DateTimeOffset? since = Request.GetDateParameter(FhirParameter.SINCE);
            string sortby = Request.GetParameter(FhirParameter.SORT);
            Response response = service.History(type, since, sortby);
            return Request.CreateResponse(response);
        }

        // ============= Whole System Interactions

        [HttpGet, Route("metadata")]
        public HttpResponseMessage Metadata()
        {
            Response response = service.Conformance();
            return Request.CreateFhirResponse(response);
        }

        [HttpOptions, Route("")]
        public HttpResponseMessage Options()
        {
            Response response = service.Conformance();
            return Request.CreateFhirResponse(response);
        }

        [HttpPost, Route("")]
        public HttpResponseMessage Transaction(Bundle bundle)
        {
            Response response = service.Transaction(bundle);
            return Request.CreateResponse(response);
        }

        [HttpPost, Route("Mailbox")]
        public HttpResponseMessage Mailbox(Bundle document)
        {
            Binary b = Request.GetBody();
            Response response = service.Mailbox(document, b);
            return Request.CreateResponse(response);
        }
        
        [HttpGet, Route("_history")]
        public HttpResponseMessage History()
        {
            DateTimeOffset? since = Request.GetDateParameter(FhirParameter.SINCE);
            string sortby = Request.GetParameter(FhirParameter.SORT);
            Response response = service.History(since, sortby);
            return Request.CreateFhirResponse(response);
        }

        [HttpGet, Route("_snapshot")]
        public HttpResponseMessage Snapshot()
        {
            string snapshot = Request.GetParameter(FhirParameter.SNAPSHOT_ID);
            int start = Request.GetIntParameter(FhirParameter.SNAPSHOT_INDEX) ?? 0; 
            int count = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
            Response response = service.GetSnapshot(snapshot, start, count);
            return Request.CreateResponse(response);
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
