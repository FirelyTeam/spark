/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Search;

namespace Spark.Core
{
    public class SearchResults : List<Uri>
    {
        public string UsedParameters { get; set; }
        public int MatchCount { get; set; }
        private Dictionary<Criterium, string> errors = new Dictionary<Criterium,string>();
        public Dictionary<Criterium, string> Errors
        {
            get { return errors; }
            set { errors = value; }
        }

        public void AddError(Criterium criterium, string errorMessage)
        {
            errors.Add(criterium, errorMessage);
        }
    }

    public static class UriListExtentions
    {
        public static bool SameAs(this ResourceIdentity a, ResourceIdentity b)
        {
            if (a.Collection == b.Collection && a.Id == b.Id)
            {
                if (a.VersionId == b.VersionId || a.VersionId == null || b.VersionId == null)
                    return true;
            }
            return false;
        }
        public static bool Has(this SearchResults list, Uri uri)
        {
            foreach (Uri item in list)
            {
                //if (item.ToString() == uri.ToString())
                ResourceIdentity a = new ResourceIdentity(item);
                ResourceIdentity b = new ResourceIdentity(uri);
                if (a.SameAs(b))
                    return true;
            
            }
            return false;
        }
        public static bool Has(this SearchResults list, string s)
        {
            //var uri = new Uri(s, UriKind.Relative);
            var uri = new Uri(s, UriKind.RelativeOrAbsolute);
            return list.Has(uri);
        }
    }
}