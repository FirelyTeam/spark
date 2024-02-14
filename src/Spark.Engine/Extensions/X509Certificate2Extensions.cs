/*
 * Copyright (c) 2023, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Spark.Engine.Extensions
{
    internal static class X509Certificate2Extensions
    {
        public static AsymmetricAlgorithm GetPrivateKey(this X509Certificate2 certificate)
        {
#if NETSTANDARD2_0 || NET462
            return certificate.PrivateKey;
#else
            if (!certificate.HasPrivateKey)
                return null;

            return certificate.GetKeyAlgorithm() switch
            {
                "1.2.840.113549.1.1.1" => certificate.GetRSAPrivateKey(),
                "1.2.840.10040.4.1" => certificate.GetDSAPrivateKey(),
                _ => throw new NotSupportedException("Key algorithm is not supported")
            };
#endif
        }
    }
}