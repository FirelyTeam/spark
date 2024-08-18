/* 
 * Copyright (c) 2014-2018, Firely (info@fire.ly)
 * Copyright (c) 2021-2024, Incendi (info@incendi.no)
 * 
 * SPDX-License-Identifier: BSD-3-Clause
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

        private readonly OperationOutcome _outcome;
        public OperationOutcome Outcome { 
            get
            {
                return _outcome.Issue.Any() ? _outcome : null;
            }
        }

        // todo: I think OperationOutcome logic should be on a higher level or at least not SearchResults specific -mh
        public SearchResults()
        {
            UsedCriteria = new List<Criterium>();
            MatchCount = 0;
            _outcome = new OperationOutcome
            {
                Issue = new List<OperationOutcome.IssueComponent>()
            };
        }

        public void AddIssue(string errorMessage, OperationOutcome.IssueSeverity severity = OperationOutcome.IssueSeverity.Error)
        {
            var newIssue = new OperationOutcome.IssueComponent() { Diagnostics = errorMessage, Severity = severity };
            _outcome.Issue.Add(newIssue);
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
}