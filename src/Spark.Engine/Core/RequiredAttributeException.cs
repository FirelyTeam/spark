/* 
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * See the file CONTRIBUTORS for details.
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using System.Runtime.Serialization;

namespace Spark.Engine.Core
{
    [Serializable]
    internal class RequiredAttributeException : Exception
    {
        public RequiredAttributeException()
        {
        }

        public RequiredAttributeException(string message) : base(message)
        {
        }

        public RequiredAttributeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RequiredAttributeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}