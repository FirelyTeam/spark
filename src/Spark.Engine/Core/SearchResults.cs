/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using System.Collections.Generic;
using System.Linq;

using Hl7.Fhir.Model;
using Spark.Search;

namespace Spark.Engine.Core
{
    public class SearchResults : List<string>
    {
        
        public List<Criterium> UsedCriteria { get; set; }
        public int MatchCount { get; set; }

        private OperationOutcome outcome;
        public OperationOutcome Outcome { 
            get
            {
                return outcome.Issue.Any() ? outcome : null;
            }
        }


        // todo: I think OperationOutcome logic should be on a higher level or at least not SearchResults specific -mh
        public SearchResults()
        {
            outcome = new OperationOutcome();
            outcome.Issue = new List<OperationOutcome.IssueComponent>();
        }

        public void AddIssue(string errorMessage, OperationOutcome.IssueSeverity severity = OperationOutcome.IssueSeverity.Error)
        {
            var newIssue = new OperationOutcome.IssueComponent() { Diagnostics = errorMessage, Severity = severity };
            outcome.Issue.Add(newIssue);
        }

        public bool HasErrors
        {
            get
            {
                return Outcome != null && Outcome.Issue.Any(i => i.Severity <= OperationOutcome.IssueSeverity.Error);
            }
        }

        public bool HasIssues
        {
            get
            {
                return Outcome != null && Outcome.Issue.Any();
            }
        }

        public string UsedParameters
        {
            get
            {
                string[] used = UsedCriteria.Select(c => c.ToString()).ToArray();
                return string.Join("&", used);
            }
        }
    }

    //public static class UriListExtentions
    //{
        //public static bool SameAs(this ResourceIdentity a, ResourceIdentity b)
        //{
        //    if (a.ResourceType == b.ResourceType && a.Id == b.Id)
        //    {
        //        if (a.VersionId == b.VersionId || a.VersionId == null || b.VersionId == null)
        //            return true;
        //    }
        //    return false;
        //}
        //public static bool Has(this SearchResults list, Uri uri)
        //{
        //    foreach (Uri item in list)
        //    {
        //        //if (item.ToString() == uri.ToString())
        //        ResourceIdentity a = new ResourceIdentity(item);
        //        ResourceIdentity b = new ResourceIdentity(uri);
        //        if (a.SameAs(b))
        //            return true;

        //    }
        //    return false;
        //}
        //public static bool Has(this SearchResults list, string s)
        //{
        //    //var uri = new Uri(s, UriKind.Relative);
        //    //var uri = new Uri(s, UriKind.RelativeOrAbsolute);
        //    return list.Contains(s);
        //}
    //}
}