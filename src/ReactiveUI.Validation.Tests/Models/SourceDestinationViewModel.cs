// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;

namespace ReactiveUI.Validation.Tests.Models;

/// <summary>
/// Mocked SourceDestinationViewModel.
/// </summary>
public class SourceDestinationViewModel : ReactiveObject, IValidatableViewModel
{
    private TestViewModel _source = new();
    private TestViewModel _destination = new();

    /// <summary>
    /// Gets or sets get the Name.
    /// </summary>
    public TestViewModel Source
    {
        get => _source;
        set => this.RaiseAndSetIfChanged(ref _source, value);
    }

    /// <summary>
    /// Gets or sets get the Name2.
    /// </summary>
    public TestViewModel Destination
    {
        get => _destination;
        set => this.RaiseAndSetIfChanged(ref _destination, value);
    }

    /// <inheritdoc/>
    public IValidationContext ValidationContext { get; } = new ValidationContext(Scheduler.Immediate);
}
