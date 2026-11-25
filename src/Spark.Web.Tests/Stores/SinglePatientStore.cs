/*
 * Copyright (c) 2026, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Linq;
using Spark.Engine.Core;
using Spark.Engine.Store.Interfaces;
using System;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Spark.Web.Tests.Stores;

public sealed class SinglePatientStore : IFhirStore
{
    private readonly Patient _patient;

    public SinglePatientStore(Patient patient)
    {
        _patient = patient;
    }

    public Task AddAsync(Entry entry) => throw new NotSupportedException();

    public Task<Entry> GetAsync(IKey key) =>
        Task.FromResult(key.TypeName == "Patient" && key.ResourceId == _patient.Id ? CreateEntry() : null);

    public Task<IList<Entry>> GetAsync(IEnumerable<IKey> localIdentifiers, IEnumerable<string> elements = null) =>
        Task.FromResult<IList<Entry>>(localIdentifiers.Select(GetAsync).Select(task => task.Result).Where(entry => entry != null).ToList());

    // Hand out a copy each time, so that whatever the server does to the resource does not leak into the next read.
    private Entry CreateEntry() =>
        Entry.Create(Bundle.HTTPVerb.PUT, Key.Create("Patient", _patient.Id, _patient.Meta?.VersionId), (Patient)_patient.DeepCopy());
}
