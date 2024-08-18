/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

namespace Spark.Engine.Core
{
    /// <summary>
    ///  Any <see cref="IKey"/> will be triaged by an <see cref="ILocalhost"/> as one of these.
    /// </summary>
    public enum KeyKind
    {
        /// <summary>
        /// absolute url, where base is not localhost
        /// </summary>
        Foreign,

        /// <summary>
        /// temporary id, URN, but not a URL. 
        /// </summary>
        Temporary,

        /// <summary>
        /// absolute url, but base is (any of the) localhost(s)
        /// </summary>
        Local,

        /// <summary>
        /// Relative url, for internal references
        /// </summary>
        Internal
    }
}