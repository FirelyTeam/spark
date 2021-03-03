﻿using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spark.Engine.Service
{
    public interface IAsyncFhirService
    {
        Task<FhirResponse> AddMetaAsync(IKey key, Parameters parameters);
        Task<FhirResponse> ConditionalCreateAsync(IKey key, Resource resource, IEnumerable<Tuple<string, string>> parameters);
        Task<FhirResponse> ConditionalCreateAsync(IKey key, Resource resource, SearchParams parameters);
        Task<FhirResponse> ConditionalDeleteAsync(IKey key, IEnumerable<Tuple<string, string>> parameters);
        Task<FhirResponse> ConditionalUpdateAsync(IKey key, Resource resource, SearchParams parameters);
        Task<FhirResponse> CapabilityStatementAsync(string sparkVersion);
        Task<FhirResponse> CreateAsync(IKey key, Resource resource);
        Task<FhirResponse> DeleteAsync(IKey key);
        Task<FhirResponse> DeleteAsync(Entry entry);
        Task<FhirResponse> GetPageAsync(string snapshotKey, int index);
        Task<FhirResponse> HistoryAsync(HistoryParameters parameters);
        Task<FhirResponse> HistoryAsync(string type, HistoryParameters parameters);
        Task<FhirResponse> HistoryAsync(IKey key, HistoryParameters parameters);
        Task<FhirResponse> MailboxAsync(Bundle bundle, Binary body);
        Task<FhirResponse> PutAsync(IKey key, Resource resource);
        Task<FhirResponse> PutAsync(Entry entry);
        Task<FhirResponse> ReadAsync(IKey key, ConditionalHeaderParameters parameters = null);
        Task<FhirResponse> ReadMetaAsync(IKey key);
        Task<FhirResponse> SearchAsync(string type, SearchParams searchCommand, int pageIndex = 0);
        Task<FhirResponse> TransactionAsync(IList<Entry> interactions);
        Task<FhirResponse> TransactionAsync(Bundle bundle);
        Task<FhirResponse> UpdateAsync(IKey key, Resource resource);
        Task<FhirResponse> ValidateOperationAsync(IKey key, Resource resource);
        Task<FhirResponse> VersionReadAsync(IKey key);
        Task<FhirResponse> VersionSpecificUpdateAsync(IKey versionedKey, Resource resource);
        Task<FhirResponse> EverythingAsync(IKey key);
        Task<FhirResponse> DocumentAsync(IKey key);
    }
}
