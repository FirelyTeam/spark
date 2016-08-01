using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Hl7.Fhir.Model;
using Spark.Engine.Core;
using Spark.Service;

namespace Spark.Engine.Service.FhirServiceExtensions
{
    public class TransactionService : ITransactionService
    {
        private readonly InteractionBuilder interactionBuilder;
        private readonly ITransfer transfer;


        public TransactionService(InteractionBuilder interactionBuilder, ITransfer transfer)
        {
            this.interactionBuilder = interactionBuilder;
            this.transfer = transfer;
        }

        public IList<Tuple<Entry, FhirResponse>> HandleTransaction(Bundle bundle)
        {
            if (FhirService == null)
            {
                throw new InvalidOperationException("Unable to run transaction operation");
            }
            var entries = interactionBuilder.GetEntries(bundle).ToList();

            return HandleTransaction(entries);
        }

        public IList<Tuple<Entry, FhirResponse>> HandleTransaction(IList<Entry> interactions)
        {
            List<Tuple<Entry, FhirResponse>> responses = new List<Tuple<Entry, FhirResponse>>();

            transfer.Internalize(interactions);

            foreach (Entry interaction in interactions)
            {
                FhirResponse response = HandleInteraction(interaction);
                if (!response.IsValid) throw new Exception();
                interaction.Resource = response.Resource;
                response.Resource = null;

                responses.Add(new Tuple<Entry, FhirResponse>(interaction, response));
            }

            transfer.Externalize(interactions);
            return responses;
        }

        public IFhirService FhirService { get; set; }

        private FhirResponse HandleInteraction(Entry interaction)
        {
            switch (interaction.Method)
            {
                case Bundle.HTTPVerb.PUT:
                    return FhirService.Put(interaction);
                case Bundle.HTTPVerb.POST:
                    return FhirService.Put(interaction);
                case Bundle.HTTPVerb.DELETE:
                    return FhirService.Delete(interaction);
                case Bundle.HTTPVerb.GET:
                    return FhirService.VersionRead((Key)interaction.Key);
                default:
                    return Respond.Success;
            }
        }

    }
}