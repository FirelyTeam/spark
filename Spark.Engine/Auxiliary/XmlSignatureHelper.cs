/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace Spark.Core
{
    // This code contains parts of the code found at
    // http://www.wiktorzychla.com/2012/12/interoperable-xml-digital-signatures-c_20.html

    public class XmlSignatureHelper
    {
        public static bool VerifySignature(string xml)
        {
            if (xml == null) throw new ArgumentNullException("xml");

            XmlDocument doc = new XmlDocument {PreserveWhitespace = true};
            doc.LoadXml(xml);

            // If there's no signature => return that we are "valid"
            XmlNode signatureNode = findSignatureElement(doc);
            if (signatureNode == null) return true;

            SignedXml signedXml = new SignedXml(doc);
            signedXml.LoadXml((XmlElement)signatureNode);

            //var x509Certificates = signedXml.KeyInfo.OfType<KeyInfoX509Data>();
            //var certificate = x509Certificates.SelectMany(cert => cert.Certificates.Cast<X509Certificate2>()).FirstOrDefault();

            //if (certificate == null) throw new InvalidOperationException("Signature does not contain a X509 certificate public key to verify the signature");
            //return signedXml.CheckSignature(certificate, true);

            return signedXml.CheckSignature();           
        }


        private static XmlNode findSignatureElement(XmlDocument doc)
        {
            XmlNode xmlNode = null;
            if (doc.DocumentElement != null)
            {
                var signatureElements = doc.DocumentElement.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#");
                switch (signatureElements.Count)
                {
                    case 1:
                        xmlNode = signatureElements[0];
                        break;
                    case 0:
                        break;
                    default:
                        throw new InvalidOperationException("Document has multiple xmldsig Signature elements");
                }
            }
            return xmlNode;
        }


        public static bool IsSigned(string xml)
        {
            if (xml == null) throw new ArgumentNullException("xml");

            // First, a quick check, before reading the full document
            if (!xml.Contains("Signature")) return false;

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return findSignatureElement(doc) != null;
        }


        public static string Sign(string xml, X509Certificate2 certificate)
        {
            if (xml == null) throw new ArgumentNullException("xml");
            if (certificate == null) throw new ArgumentNullException("certificate");
            if (!certificate.HasPrivateKey) throw new ArgumentException("Certificate should have a private key", "certificate");

            XmlDocument doc = new XmlDocument {PreserveWhitespace = true};

            doc.LoadXml(xml);

            SignedXml signedXml = new SignedXml(doc) {SigningKey = certificate.PrivateKey};

            // Attach certificate KeyInfo
            KeyInfoX509Data keyInfoData = new KeyInfoX509Data(certificate);
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(keyInfoData);
            signedXml.KeyInfo = keyInfo;

            // Attach transforms
            var reference = new Reference("");
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform(includeComments: false));
            reference.AddTransform(new XmlDsigExcC14NTransform(includeComments: false));
            signedXml.AddReference(reference);

            // Compute signature
            signedXml.ComputeSignature();
            var signatureElement = signedXml.GetXml();

            // Add signature to bundle
            if (doc.DocumentElement != null) doc.DocumentElement.AppendChild(doc.ImportNode(signatureElement, true));

            return doc.OuterXml;
        }
    }
}
