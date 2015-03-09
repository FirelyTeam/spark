/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Service
{
    
    internal class ConnectathonDocumentScenario
    {
        // API: Is this still needed
        public static DocumentReference DocumentToDocumentReference(Composition composition, Bundle bundle, Binary bin, Uri binLocation)
        {
            /*
            var reference = new DocumentReference();
            reference.MasterIdentifier = new Identifier(Identifier.SYSTMEM_URI, bundle.Id.ToString());
            reference.Identifier = composition.Identifier != null ? new List<Identifier>() { composition.Identifier } : null;
            reference.Subject = composition.Subject;
            reference.Type = composition.Type;
            reference.Class = composition.Class;
            reference.Author = new List<ResourceReference>( composition.Author );
            reference.Custodian = composition.Custodian;
            reference.Authenticator = composition.Attester != null ? composition.Attester.Where(att => att.Mode.Any(am => am == Composition.CompositionAttestationMode.Professional ||
                                                                    am == Composition.CompositionAttestationMode.Legal) && att.Party != null).Select(att => att.Party).Last() : null;
            reference.CreatedElement = composition.Date != null ? new FhirDateTime(composition.Date) : null;
            reference.IndexedElement = Instant.Now();
            reference.Status = DocumentReference.DocumentReferenceStatus.Current;

            reference.DocStatus = composition.Status != null ?
                new CodeableConcept { Coding = new List<Coding>() { new Coding("http://hl7.org/fhir/composition-status", composition.Status.ToString()) } } : null;
            
            reference.Description = composition.Title;
            reference.Confidentiality = composition.Confidentiality != null ? new List<CodeableConcept>() { new CodeableConcept() { Coding = new List<Coding>() { composition.Confidentiality } } } : null;
            reference.PrimaryLanguage = composition.Language;
            reference.MimeType = bin.ContentType;
            reference.Format = new List<string>( bundle.Tags.FilterOnFhirSchemes().Where(t => t.Scheme != Tag.FHIRTAGSCHEME_SECURITY).Select(tg => tg.Term) );
            reference.Size = bin.Content.Length;
            reference.Hash = calculateSHA1(bin.Content);
            reference.Location = binLocation.ToString();

            if (composition.Event != null)
            {
                reference.Context = new DocumentReference.DocumentReferenceContextComponent();
                reference.Context.Event = composition.Event.Code != null ? new List<CodeableConcept>(composition.Event.Code) : null;
                reference.Context.Period = composition.Event.Period != null ? composition.Event.Period : null;
            }

            return reference;
            */
            return null;
        }

        private static string calculateSHA1(byte[] data)
        {
            SHA1CryptoServiceProvider cryptoTransformSHA1 = new SHA1CryptoServiceProvider();
            return BitConverter.ToString(cryptoTransformSHA1.ComputeHash(data));
        }
    }
    
}

// Grahame's Delphi equivalent
 //comp := mainRequest.Feed.entries[0].resource as TFhirComposition;

 // ref := TFhirDocumentReference.Create;
 // try
 //   ref.masterIdentifier := FFactory.makeIdentifier('urn:ietf:rfc:3986', mainRequest.Feed.id);
 //   if (comp.identifier <> nil) then
 //     ref.identifierList.Add(comp.identifier.Clone);
 //   ref.subject := comp.subject.Clone;
 //   ref.type_ := comp.type_.Clone;
 //   ref.class_ := comp.class_.Clone;
 //   ref.authorList.AddAll(comp.authorList);
 //   ref.custodian := comp.custodian.Clone;
 //   // we don't have a use for policyManager at this point
 //   for i := 0 to comp.attesterList.Count - 1 do
 //   begin
 //     att := comp.attesterList[i];
 //     if (att.modeST * [CompositionAttestationModeProfessional, CompositionAttestationModeLegal] <> []) then
 //       ref.authenticator := att.party.Clone; // which means that last one is the one
 //   end;
 //   ref.createdST := comp.instantST.Clone;
 //   ref.indexedST := NowUTC;
 //   ref.statusST := DocumentReferenceStatusCurrent;
 //   ref.docStatus := FFactory.makeCodeableConcept(FFactory.makeCoding('http://hl7.org/fhir/composition-status', comp.status.value, comp.status.value), '');
 //   // no relationships to other documents
 //   ref.description := comp.title.Clone;
 //   ref.confidentialityList.Add(FFactory.makeCodeableConcept(comp.confidentiality.Clone, ''));
 //   ref.primaryLanguage := comp.language.Clone;
 //   if mainRequest.PostFormat = ffJson then
 //     ref.mimeTypeST := 'application/json+fhir'
 //   else
 //     ref.mimeTypeST := 'application/atom+xml';
 //   // populating DocumentReference.format:
 //   // we take any tags on the document. We ignore security tags. Always will be at least one - the document tag itself
 //   for i := 0 to mainRequest.Feed.categories.Count - 1 do
 //     if (mainRequest.Feed.categories[i].scheme <> 'http://hl7.org/fhir/tag/security') then
 //       ref.formatList.Add(FFactory.makeUri(mainRequest.Feed.categories[i].term));
 //   ref.sizeST := inttostr(mainRequest.Source.Size);
 //   // ref.hash (HexBinary representation of SHA1)
 //   ref.locationST := 'Binary/'+binaryId;
 //   if comp.event <> nil then
 //   begin
 //     ref.context := TFhirDocumentReferenceContext.Create;
 //     ref.context.eventList.AddAll(comp.event.codeList);
 //     ref.context.period := comp.event.period.Clone;
 //   end;
