﻿using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Interfaces;
using Spark.Service;

namespace Spark.Engine.FhirResponseFactory
{
    public interface IFhirResponseFactory
    {
        //FhirResponse GetFhirResponse(Entry entry, Key key = null, params object[] parameters);
        //FhirResponse GetFhirResponse(Entry entry, Key key = null, IEnumerable<object> parameters = null);
        FhirResponse GetFhirResponse(Entry entry, IKey key = null, IEnumerable<object> parameters = null);
        FhirResponse GetFhirResponse(Entry entry, IKey key = null, params object[] parameters);
        //FhirResponse GetMetadataResponse(Entry entry, Key key = null);
        FhirResponse GetMetadataResponse(Entry entry, IKey key = null);
        FhirResponse GetFhirResponse(IList<Entry> interactions, Bundle.BundleType bundleType);
        FhirResponse GetFhirResponse(Bundle bundle);
        FhirResponse GetFhirResponse(IEnumerable<Tuple<Entry, FhirResponse>> responses, Bundle.BundleType bundleType);
    }

    public class FhirResponseFactory : IFhirResponseFactory
    {
        private readonly IFhirResponseInterceptorRunner interceptorRunner;
        private readonly ILocalhost localhost;

        public FhirResponseFactory(ILocalhost localhost, IFhirResponseInterceptorRunner interceptorRunner)
        {
            this.localhost = localhost;
            this.interceptorRunner = interceptorRunner;
        }

        public FhirResponse GetFhirResponse(Entry entry, IKey key = null, IEnumerable<object> parameters = null)
        {
            if (entry == null)
            {
                return Respond.NotFound(key);
            }
            if (entry.IsDeleted())
            {
                return Respond.Gone(entry);
            }

            FhirResponse response = null;

            if (parameters != null)
            {
                response = interceptorRunner.RunInterceptors(entry, parameters);
            }

            return response ?? Respond.WithResource(entry);
        }

        public FhirResponse GetFhirResponse(Entry entry, IKey key = null, params object[] parameters)
        {
            return GetFhirResponse(entry, key, parameters.ToList());
        }

        public FhirResponse GetMetadataResponse(Entry entry, IKey key = null)
        {
            if (entry == null)
            {
                return Respond.NotFound(key);
            }
            else if (entry.IsDeleted())
            {
                return Respond.Gone(entry);
            }

            return Respond.WithMeta(entry);
        }

        public FhirResponse GetFhirResponse(IList<Entry> interactions, Bundle.BundleType bundleType)
        {
            Bundle bundle = localhost.CreateBundle(bundleType).Append(interactions);
            return Respond.WithBundle(bundle);
        }

        public FhirResponse GetFhirResponse(IEnumerable<Tuple<Entry, FhirResponse>> responses, Bundle.BundleType bundleType)
        {
            Bundle bundle = localhost.CreateBundle(bundleType);
            foreach (Tuple<Entry, FhirResponse> response in responses)
            {
                bundle.Append(response.Item1, response.Item2);
            }
      
            return Respond.WithBundle(bundle);
        }

        public FhirResponse GetFhirResponse(Bundle bundle)
        {
            return Respond.WithBundle(bundle);
        }
    }
}