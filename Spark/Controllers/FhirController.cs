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
using Hl7.Fhir.Model;
using Spark.Service;
using System.Web.Http.Description;
using System.Net.Http;
using Spark.Config;
using Spark.Support;
using System.Diagnostics;
using Hl7.Fhir.Rest;
using System.Web.Http;
using Spark.Core;
using Spark.Http;
using System.Web.Http.Cors;
using System.Net.Http.Headers;
using System.IO;

namespace Spark.Controllers
{
    [RoutePrefix("fhir")]
    [EnableCors("*","*","*","*")]
    public class FhirController : ApiController
    {
        IFhirService service; 
        public FhirController()
        {
            service = DependencyCoupler.Inject<IFhirService>();
        }


        // ============= Instance Level Interactions
       
        [HttpGet, Route("{type}/{id}")] 
        public HttpResponseMessage Read(string type, string id)
        {
            ResourceEntry entry = service.Read(type, id);
            return Request.ResourceResponse(entry);
        }
        
        [HttpGet, Route("{type}/{id}/_history/{vid}")]
        public HttpResponseMessage VRead(string type, string id, string vid)
        {           
            ResourceEntry entry = service.VRead(type, id, vid);
            return Request.ResourceResponse(entry);
        }

        [HttpPut, Route("{type}/{id}")]
        public HttpResponseMessage Update(string type, string id, ResourceEntry entry)
        {
            entry.Tags = Request.GetFhirTags(); // todo: move to model binder?

            // ballot: Update is a mix between name from CRUD (only update no create) and functionality from Rest PUT (Create or update)
            ResourceEntry newEntry = service.Update(type, id, entry, null);

            if (newEntry != null)
            {
                return Request.StatusResponse(newEntry, HttpStatusCode.OK);
            }
            else
            {
                newEntry = service.Create(type, entry, id);
                return Request.StatusResponse(newEntry, HttpStatusCode.Created);
            }
        }

        [HttpPost, Route("{type}")]
        public HttpResponseMessage Create(string type, ResourceEntry entry)
        {
            entry.Tags = Request.GetFhirTags(); // todo: move to model binder?

            ResourceEntry newentry = service.Create(type, entry, null);
            return Request.StatusResponse(newentry, HttpStatusCode.Created);
        }
        
        [Route("{type}/{id}")]
        public HttpResponseMessage Delete(string type, string id)
        {
            service.Delete(type, id);
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }

        [HttpGet, Route("{type}/{id}/_history")]
        public Bundle History(string type, string id)
        {
            DateTimeOffset? since = Request.GetDateParameter(FhirParameter.SINCE);
            return service.History(type, id, since);
        }

        
        // ============= Type Level Interactions

        [HttpPost, Route("{type}/{id}")]
        public HttpResponseMessage Create(string type, string id, ResourceEntry entry)
        {
            entry.Tags = Request.GetFhirTags(); // todo: move to model binder?

            ResourceEntry newentry = service.Create(type, entry, id);
            return Request.StatusResponse(newentry, HttpStatusCode.Created);
        }


        [HttpGet, Route("{type}")]
        public Bundle Search(string type)
        {
            var parameters = Request.TupledParameters();
            int pagesize = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
            
            return service.Search(type, parameters, pagesize);
        }

        [HttpGet, Route("{type}/_search")]
        public Bundle SearchWithOperator(string type)
        {
            return Search(type);
        }

        [HttpGet, Route("{type}/_history")]
        public Bundle History(string type)
        {
            DateTimeOffset? since = Request.GetDateParameter(FhirParameter.SINCE);
            return service.History(type, since);
        }

        [HttpPost, Route("{type}/_validate")]
        public OperationOutcome Validate()
        {
            throw new NotImplementedException("Still to do");
        }


        // ============= Whole System Interactions

        [HttpGet, Route("metadata")]
        public ResourceEntry Metadata()
        {
            return service.Conformance();
        }

        [HttpOptions, Route("")]
        public ResourceEntry Options()
        {
            return service.Conformance();
        }

        [HttpPost, Route("")]
        public Bundle Transaction(Bundle bundle)
        {
            return service.Transaction(bundle);
        }

        [HttpPost, Route("Mailbox")]
        public Bundle Mailbox(Bundle document)
        {
            Binary b = Request.GetBody();
            return service.Mailbox(document, b);
        }
        
        [HttpGet, Route("_history")]
        public Bundle History()
        {
            DateTimeOffset? since = Request.GetDateParameter(FhirParameter.SINCE);
            return service.History(since);
        }

        [HttpGet, Route("_snapshot")]
        public Bundle Snapshot()
        {
            string snapshot = Request.Parameter(FhirParameter.SNAPSHOT_ID);
            int start = Request.GetIntParameter(FhirParameter.SNAPSHOT_INDEX) ?? 0; 
            int count = Request.GetIntParameter(FhirParameter.COUNT) ?? Const.DEFAULT_PAGE_SIZE;
            return service.GetSnapshot(snapshot, start, count);
        }


        // ============= Tag Interactions

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
        public TagList HistoryTags(string type, string id, string vid)
        {
            return service.TagsFromHistory(type, id, vid);
        }

        [HttpPost, Route("{type}/{id}/_tags")]
        public HttpResponseMessage AffixTag(string type, string id, TagList taglist)
        {
            service.AffixTags(type, id,taglist != null ? taglist.Category : null);
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


        // ============= Validate

        [HttpPost, Route("{type}/_validate")]
        public HttpResponseMessage Validate(string type, ResourceEntry entry)
        {
            service.Validate(type, entry);
            return Request.CreateResponse(HttpStatusCode.OK);
        }
        

       
    }

  
}
