/* 
 * Copyright (c) 2015-2018, Furore (info@furore.com) and contributors
 * Copyright (c) 2019-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Engine.Extensions;
using Spark.Engine.Interfaces;

namespace Spark.Engine.FhirResponseFactory
{

    public class FhirResponseFactory : IFhirResponseFactory
    {
        private readonly IFhirResponseInterceptorRunner _interceptorRunner;
        private readonly ILocalhost _localhost;

        public FhirResponseFactory(ILocalhost localhost, IFhirResponseInterceptorRunner interceptorRunner)
        {
            _localhost = localhost;
            _interceptorRunner = interceptorRunner;
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
                response = _interceptorRunner.RunInterceptors(entry, parameters);
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
            Bundle bundle = _localhost.CreateBundle(bundleType).Append(interactions);
            return Respond.WithBundle(bundle);
        }

        public FhirResponse GetFhirResponse(IEnumerable<Tuple<Entry, FhirResponse>> responses, Bundle.BundleType bundleType)
        {
            Bundle bundle = _localhost.CreateBundle(bundleType);
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