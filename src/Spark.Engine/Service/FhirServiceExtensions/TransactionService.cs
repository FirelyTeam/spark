/* 
 * Copyright (c) 2016, Furore (info@furore.com) and contributors
 * Copyright (c) 2021, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/spark/stu3/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Service;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class TransactionService : ITransactionService
    {
        private readonly ILocalhost _localhost;
        private readonly ITransfer _transfer;
        private readonly ISearchService _searchService;

        public TransactionService(ILocalhost localhost, ITransfer transfer, ISearchService searchService)
        {
            _localhost = localhost;
            _transfer = transfer;
            _searchService = searchService;
        }

        public FhirResponse HandleTransaction(ResourceManipulationOperation operation, IInteractionHandler interactionHandler)
        {
            return HandleOperation(operation, interactionHandler);
        }

        public Task<FhirResponse> HandleTransactionAsync(ResourceManipulationOperation operation, IInteractionHandler interactionHandler)
        {
            return HandleOperationAsync(operation, interactionHandler);
        }

        public IList<Tuple<Entry, FhirResponse>> HandleTransaction(Bundle bundle, IInteractionHandler interactionHandler)
        {
            if (interactionHandler == null)
            {
                throw new InvalidOperationException("Unable to run transaction operation");
            }

            var entries = new List<Entry>();
            Mapper<string, IKey> mapper = new Mapper<string, IKey>();

            foreach (var operation in bundle.Entry.Select(e => ResourceManipulationOperationFactory.GetManipulationOperation(e, _localhost, _searchService)))
            {
                IList<Entry> atomicOperations = operation.GetEntries().ToList();
                AddMappingsForOperation(mapper, operation, atomicOperations);
                entries.AddRange(atomicOperations);
            }

            return HandleTransaction(entries, interactionHandler, mapper);
        }

        public async Task<IList<Tuple<Entry, FhirResponse>>> HandleTransactionAsync(Bundle bundle, IInteractionHandler interactionHandler)
        {
            if (interactionHandler == null)
            {
                throw new InvalidOperationException("Unable to run transaction operation");
            }

            var entries = new List<Entry>();
            Mapper<string, IKey> mapper = new Mapper<string, IKey>();

            foreach (var task in bundle.Entry.Select(e => ResourceManipulationOperationFactory.GetManipulationOperationAsync(e, _localhost, _searchService)))
            {
                var operation = await task.ConfigureAwait(false);
                IList<Entry> atomicOperations = operation.GetEntries().ToList();
                AddMappingsForOperation(mapper, operation, atomicOperations);
                entries.AddRange(atomicOperations);
            }

            return await HandleTransactionAsync(entries, interactionHandler, mapper).ConfigureAwait(false);
        }

        public IList<Tuple<Entry, FhirResponse>> HandleTransaction(IList<Entry> interactions, IInteractionHandler interactionHandler)
        {
            return interactionHandler == null
                ? throw new InvalidOperationException("Unable to run transaction operation")
                : HandleTransaction(interactions, interactionHandler, null);
        }

        public async Task<IList<Tuple<Entry, FhirResponse>>> HandleTransactionAsync(IList<Entry> interactions, IInteractionHandler interactionHandler)
        {
            return interactionHandler == null
                ? throw new InvalidOperationException("Unable to run transaction operation")
                : await HandleTransactionAsync(interactions, interactionHandler, null).ConfigureAwait(false);
        }

        public FhirResponse HandleOperation(ResourceManipulationOperation operation, IInteractionHandler interactionHandler, Mapper<string, IKey> mapper = null)
        {
            IList<Entry> interactions = operation.GetEntries().ToList();
            if (mapper != null)
                _transfer.Internalize(interactions, mapper);

            FhirResponse response = null;
            foreach (Entry interaction in interactions)
            {
                response = MergeFhirResponse(response, interactionHandler.HandleInteraction(interaction));
                if (!response.IsValid) throw new Exception();
                interaction.Resource = response.Resource;
            }

            _transfer.Externalize(interactions);

            return response;
        }

        public async Task<FhirResponse> HandleOperationAsync(ResourceManipulationOperation operation, IInteractionHandler interactionHandler, Mapper<string, IKey> mapper = null)
        {
            IList<Entry> interactions = operation.GetEntries().ToList();
            if(mapper != null)
            _transfer.Internalize(interactions, mapper);

            FhirResponse response = null;
            foreach (Entry interaction in interactions)
            {
                response = MergeFhirResponse(response, await interactionHandler.HandleInteractionAsync(interaction).ConfigureAwait(false));
                if (!response.IsValid) throw new Exception();
                interaction.Resource = response.Resource;
            }

            _transfer.Externalize(interactions);

            return response;
        }

        private FhirResponse MergeFhirResponse(FhirResponse previousResponse, FhirResponse response)
        {
            //CCR: How to handle responses?
            //Currently we assume that all FhirResponses from one ResourceManipulationOperation should be equivalent - kind of hackish
            if (previousResponse == null)
                return response;
            if (!response.IsValid)
                return response;
            if(response.StatusCode != previousResponse.StatusCode)
                throw new Exception("Incompatible responses");
            if (response.Key != null && previousResponse.Key != null && response.Key.Equals(previousResponse.Key) == false)
                throw new Exception("Incompatible responses");
            if((response.Key != null && previousResponse.Key== null) || (response.Key == null && previousResponse.Key != null))
                throw new Exception("Incompatible responses");
            return response;
        }

        private void AddMappingsForOperation(Mapper<string, IKey> mapper, ResourceManipulationOperation operation, IList<Entry> interactions)
        {
            if(mapper == null)
                return;
            if (interactions.Count() == 1)
            {
                Entry entry = interactions.First();
                if (!entry.Key.Equals(operation.OperationKey))
                {
                    if (_localhost.GetKeyKind(operation.OperationKey) == KeyKind.Temporary)
                    {
                        mapper.Remap(operation.OperationKey.ResourceId, entry.Key.WithoutVersion());
                    }
                    else
                    {
                        mapper.Remap(operation.OperationKey.ToString(), entry.Key.WithoutVersion());
                    }
                }
            }
        }

        private IList<Tuple<Entry, FhirResponse>> HandleTransaction(IList<Entry> interactions, IInteractionHandler interactionHandler, Mapper<string, IKey> mapper)
        {
            List<Tuple<Entry, FhirResponse>> responses = new List<Tuple<Entry, FhirResponse>>();

            _transfer.Internalize(interactions, mapper);

            foreach (Entry interaction in interactions)
            {
                FhirResponse response = interactionHandler.HandleInteraction(interaction);
                if (!response.IsValid)
                {
                    throw new Exception($"Unsuccessful response to interaction {interaction}: {response}");
                }
                interaction.Resource = response.Resource;
                response.Resource = null;

                responses.Add(new Tuple<Entry, FhirResponse>(interaction, response)); //CCR: How to handle responses for transactions? 
                                                                                      //The specifications says only one response should be sent per EntryComponent, 
                                                                                      //but one EntryComponent might correpond to multiple atomic entries (Entry)
                                                                                      //Example: conditional delete
            }

            _transfer.Externalize(interactions);
            return responses;
        }

        private async Task<IList<Tuple<Entry, FhirResponse>>> HandleTransactionAsync(IList<Entry> interactions, IInteractionHandler interactionHandler, Mapper<string, IKey> mapper)
        {
            List<Tuple<Entry, FhirResponse>> responses = new List<Tuple<Entry, FhirResponse>>();

            _transfer.Internalize(interactions, mapper);

            foreach (Entry interaction in interactions)
            {
                FhirResponse response = await interactionHandler.HandleInteractionAsync(interaction).ConfigureAwait(false);
                if (!response.IsValid)
                {
                    throw new Exception($"Unsuccessful response to interaction {interaction}: {response}");
                }
                interaction.Resource = response.Resource;
                response.Resource = null;

                responses.Add(new Tuple<Entry, FhirResponse>(interaction, response)); //CCR: How to handle responses for transactions? 
                                                                                      //The specifications says only one response should be sent per EntryComponent, 
                                                                                      //but one EntryComponent might correpond to multiple atomic entries (Entry)
                                                                                      //Example: conditional delete
            }

            _transfer.Externalize(interactions);
            return responses;
        }
    }
}