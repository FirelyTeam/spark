/*
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using Spark.Engine.FhirResponseFactory;
using Spark.Engine.Service.FhirServiceExtensions;
using Spark.Engine.Storage;
using Spark.Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spark.Engine.Service.Abstractions
{
    public class FhirServiceBase : ExtendableWith<IFhirServiceExtension>, IFhirService
    {
        protected readonly IFhirResponseFactory _responseFactory;
        protected readonly ICompositeServiceListener _serviceListener;
        
        protected FhirServiceBase(IFhirServiceExtension[] extensions,
            IFhirResponseFactory responseFactory,
            ICompositeServiceListener serviceListener = null)
        {
            _responseFactory = responseFactory;
            _serviceListener = serviceListener;

            foreach (IFhirServiceExtension serviceExtension in extensions)
            {
                AddExtension(serviceExtension);
            }
        }

        protected async Task<Entry> StoreAsync(Entry entry)
        {
            Entry result = await GetFeature<IResourceStorageService>()
                .AddAsync(entry).ConfigureAwait(false);
            await _serviceListener.InformAsync(entry).ConfigureAwait(false);
            return result;
        }

        protected T GetFeature<T>() where T : IFhirServiceExtension
        {
            return FindExtension<T>() ??
                   throw new NotSupportedException($"Feature {typeof(T)} not supported");
        }

        protected static void ValidateKey(IKey key, bool withVersion = false)
        {
            Validate.HasTypeName(key);
            Validate.HasResourceId(key);
            if (withVersion)
            {
                Validate.HasVersion(key);
            }
            else
            {
                Validate.HasNoVersion(key);
            }
            Validate.Key(key);
        }
        
        public virtual Task<FhirResponse> AddMetaAsync(IKey key, Parameters parameters) => throw new NotImplementedException();
        public virtual Task<FhirResponse> ConditionalCreateAsync(IKey key, Resource resource, IEnumerable<Tuple<string, string>> parameters) => throw new NotImplementedException();
        public virtual Task<FhirResponse> ConditionalCreateAsync(IKey key, Resource resource, SearchParams parameters) => throw new NotImplementedException();
        public virtual Task<FhirResponse> ConditionalDeleteAsync(IKey key, IEnumerable<Tuple<string, string>> parameters) => throw new NotImplementedException();
        public virtual Task<FhirResponse> ConditionalUpdateAsync(IKey key, Resource resource, SearchParams parameters) => throw new NotImplementedException();
        public virtual Task<FhirResponse> CapabilityStatementAsync(string sparkVersion) => throw new NotImplementedException();
        public virtual Task<FhirResponse> CreateAsync(IKey key, Resource resource) => throw new NotImplementedException();
        public virtual Task<FhirResponse> DeleteAsync(IKey key) => throw new NotImplementedException();
        public virtual Task<FhirResponse> DeleteAsync(Entry entry) => throw new NotImplementedException();
        public virtual Task<FhirResponse> GetPageAsync(string snapshotKey, int index) => throw new NotImplementedException();
        public virtual Task<FhirResponse> HistoryAsync(HistoryParameters parameters) => throw new NotImplementedException();
        public virtual Task<FhirResponse> HistoryAsync(string type, HistoryParameters parameters) => throw new NotImplementedException();
        public virtual Task<FhirResponse> HistoryAsync(IKey key, HistoryParameters parameters) => throw new NotImplementedException();
        public virtual Task<FhirResponse> PutAsync(IKey key, Resource resource) => throw new NotImplementedException();
        public virtual Task<FhirResponse> PutAsync(Entry entry) => throw new NotImplementedException();
        public virtual Task<FhirResponse> ReadAsync(IKey key, ConditionalHeaderParameters parameters = null) => throw new NotImplementedException();
        public virtual Task<FhirResponse> ReadMetaAsync(IKey key) => throw new NotImplementedException();
        public virtual Task<FhirResponse> SearchAsync(string type, SearchParams searchCommand, int pageIndex = 0) => throw new NotImplementedException();
        public virtual Task<FhirResponse> TransactionAsync(IList<Entry> interactions) => throw new NotImplementedException();
        public virtual Task<FhirResponse> TransactionAsync(Bundle bundle) => throw new NotImplementedException();
        public virtual Task<FhirResponse> UpdateAsync(IKey key, Resource resource) => throw new NotImplementedException();
        public virtual Task<FhirResponse> PatchAsync(IKey key, Parameters patch) => throw new NotImplementedException();
        public virtual Task<FhirResponse> ValidateOperationAsync(IKey key, Resource resource) => throw new NotImplementedException();
        public virtual Task<FhirResponse> VersionReadAsync(IKey key) => throw new NotImplementedException();
        public virtual Task<FhirResponse> VersionSpecificUpdateAsync(IKey versionedKey, Resource resource) => throw new NotImplementedException();
        public virtual Task<FhirResponse> EverythingAsync(IKey key) => throw new NotImplementedException();
        public virtual Task<FhirResponse> DocumentAsync(IKey key) => throw new NotImplementedException();
    }
}