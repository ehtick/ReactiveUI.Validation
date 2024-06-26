// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ReactiveUI.Validation.Collections;
using ReactiveUI.Validation.Components;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Formatters.Abstractions;
using ReactiveUI.Validation.Helpers;
using ReactiveUI.Validation.Tests.Models;
using Xunit;

namespace ReactiveUI.Validation.Tests;

/// <summary>
/// Tests for INotifyDataErrorInfo support.
/// </summary>
public class NotifyDataErrorInfoTests
{
    private const string NameShouldNotBeEmptyMessage = "Name shouldn't be empty.";

    /// <summary>
    /// Verifies that the ErrorsChanged event fires on ViewModel initialization.
    /// </summary>
    [Fact]
    public void ShouldMarkPropertiesAsInvalidOnInit()
    {
        var viewModel = new IndeiTestViewModel();
        var view = new IndeiTestView(viewModel);

        using var firstValidation = new BasePropertyValidation<IndeiTestViewModel, string>(
            viewModel,
            vm => vm.Name,
            s => !string.IsNullOrEmpty(s),
            NameShouldNotBeEmptyMessage);

        viewModel.ValidationContext.Add(firstValidation);
        view.Bind(view.ViewModel, vm => vm.Name, v => v.NameLabel);
        view.BindValidation(view.ViewModel, vm => vm.Name, v => v.NameErrorLabel);

        // Verify validation context behavior.
        Assert.False(viewModel.ValidationContext.IsValid);
        Assert.Single(viewModel.ValidationContext.Validations.Items);
        Assert.Equal(NameShouldNotBeEmptyMessage, view.NameErrorLabel);

        // Verify INotifyDataErrorInfo behavior.
        Assert.True(viewModel.HasErrors);
        Assert.Equal(NameShouldNotBeEmptyMessage, viewModel.GetErrors("Name").Cast<string>().First());
    }

    /// <summary>
    /// Verifies that the view model listens to the INotifyPropertyChanged event
    /// and sends INotifyDataErrorInfo notifications.
    /// </summary>
    [Fact]
    public void ShouldSynchronizeNotifyDataErrorInfoWithValidationContext()
    {
        var viewModel = new IndeiTestViewModel();
        var view = new IndeiTestView(viewModel);

        using var firstValidation = new BasePropertyValidation<IndeiTestViewModel, string>(
            viewModel,
            vm => vm.Name,
            s => !string.IsNullOrEmpty(s),
            NameShouldNotBeEmptyMessage);

        viewModel.ValidationContext.Add(firstValidation);
        view.Bind(view.ViewModel, vm => vm.Name, v => v.NameLabel);
        view.BindValidation(view.ViewModel, vm => vm.Name, v => v.NameErrorLabel);

        // Verify the initial state.
        Assert.True(viewModel.HasErrors);
        Assert.False(viewModel.ValidationContext.IsValid);
        Assert.Single(viewModel.ValidationContext.Validations.Items);
        Assert.Equal(NameShouldNotBeEmptyMessage, viewModel.GetErrors("Name").Cast<string>().First());
        Assert.Equal(NameShouldNotBeEmptyMessage, view.NameErrorLabel);

        // Send INotifyPropertyChanged.
        viewModel.Name = "JoJo";

        // Verify the changed state.
        Assert.False(viewModel.HasErrors);
        Assert.True(viewModel.ValidationContext.IsValid);
        Assert.Empty(viewModel.GetErrors("Name").Cast<string>());
        Assert.Empty(view.NameErrorLabel);

        // Send INotifyPropertyChanged.
        viewModel.Name = string.Empty;

        // Verify the changed state.
        Assert.True(viewModel.HasErrors);
        Assert.False(viewModel.ValidationContext.IsValid);
        Assert.Single(viewModel.ValidationContext.Validations.Items);
        Assert.Equal(NameShouldNotBeEmptyMessage, viewModel.GetErrors("Name").Cast<string>().First());
        Assert.Equal(NameShouldNotBeEmptyMessage, view.NameErrorLabel);
    }

    /// <summary>
    /// The ErrorsChanged event should fire when properties change.
    /// </summary>
    [Fact]
    public void ShouldFireErrorsChangedEventWhenValidationStateChanges()
    {
        var viewModel = new IndeiTestViewModel();

        DataErrorsChangedEventArgs arguments = null;
        viewModel.ErrorsChanged += (_, args) => arguments = args;

        using var firstValidation = new BasePropertyValidation<IndeiTestViewModel, string>(
            viewModel,
            vm => vm.Name,
            s => !string.IsNullOrEmpty(s),
            NameShouldNotBeEmptyMessage);

        viewModel.ValidationContext.Add(firstValidation);

        Assert.True(viewModel.HasErrors);
        Assert.False(viewModel.ValidationContext.IsValid);
        Assert.Single(viewModel.ValidationContext.Validations.Items);
        Assert.Single(viewModel.GetErrors("Name").Cast<string>());

        viewModel.Name = "JoJo";

        Assert.False(viewModel.HasErrors);
        Assert.Empty(viewModel.GetErrors("Name").Cast<string>());
        Assert.NotNull(arguments);
        Assert.Equal("Name", arguments.PropertyName);
    }

    /// <summary>
    /// Using ModelObservableValidation with NotifyDataErrorInfo should return errors when associated property changes.
    /// </summary>
    [Fact]
    public void ShouldDeliverErrorsWhenModelObservableValidationTriggers()
    {
        var viewModel = new IndeiTestViewModel();

        const string namesShouldMatchMessage = "names should match.";
        viewModel.ValidationRule(
            vm => vm.OtherName,
            viewModel.WhenAnyValue(
                m => m.Name,
                m => m.OtherName,
                (name, other) => name == other),
            namesShouldMatchMessage);

        Assert.False(viewModel.HasErrors);
        Assert.True(viewModel.ValidationContext.IsValid);
        Assert.Single(viewModel.ValidationContext.Validations.Items);
        Assert.Empty(viewModel.GetErrors(nameof(viewModel.Name)).Cast<string>());
        Assert.Empty(viewModel.GetErrors(nameof(viewModel.OtherName)).Cast<string>());

        viewModel.Name = "JoJo";
        viewModel.OtherName = "NoNo";

        Assert.True(viewModel.HasErrors);
        Assert.Empty(viewModel.GetErrors(nameof(viewModel.Name)).Cast<string>());
        Assert.Single(viewModel.GetErrors(nameof(viewModel.OtherName)).Cast<string>());
        Assert.Single(viewModel.ValidationContext.Text);
        Assert.Equal(namesShouldMatchMessage, viewModel.ValidationContext.Text.Single());
    }

    /// <summary>
    /// Verifies that validation rules of the same property do not duplicate.
    /// Earlier they sometimes could, due to the .Connect() method misuse.
    /// </summary>
    [Fact]
    public void ValidationRulesOfTheSamePropertyShouldNotDuplicate()
    {
        var viewModel = new IndeiTestViewModel();
        viewModel.ValidationRule(
            m => m.Name,
            m => m is not null,
            "Name shouldn't be null.");

        viewModel.ValidationRule(
            m => m.Name,
            m => !string.IsNullOrWhiteSpace(m),
            "Name shouldn't be white space.");

        Assert.False(viewModel.ValidationContext.IsValid);
        Assert.Equal(2, viewModel.ValidationContext.Validations.Count);
    }

    /// <summary>
    /// Verifies that the <see cref="INotifyDataErrorInfo"/> events are published
    /// according to the changes of the validated properties.
    /// </summary>
    [Fact]
    public void ShouldSendPropertyChangeNotificationsForCorrectProperties()
    {
        var viewModel = new IndeiTestViewModel();
        viewModel.ValidationRule(
            m => m.Name,
            m => m is not null,
            "Name shouldn't be null.");

        viewModel.ValidationRule(
            m => m.OtherName,
            m => m is not null,
            "Other name shouldn't be null.");

        Assert.Single(viewModel.GetErrors(nameof(viewModel.Name)));
        Assert.Single(viewModel.GetErrors(nameof(viewModel.OtherName)));

        var arguments = new List<DataErrorsChangedEventArgs>();
        viewModel.ErrorsChanged += (_, args) => arguments.Add(args);
        viewModel.Name = "Josuke";
        viewModel.OtherName = "Jotaro";

        Assert.Equal(2, arguments.Count);
        Assert.Equal(nameof(viewModel.Name), arguments[0].PropertyName);
        Assert.Equal(nameof(viewModel.OtherName), arguments[1].PropertyName);
        Assert.False(viewModel.HasErrors);

        viewModel.Name = null;
        viewModel.OtherName = null;

        Assert.Equal(4, arguments.Count);
        Assert.Equal(nameof(viewModel.Name), arguments[2].PropertyName);
        Assert.Equal(nameof(viewModel.OtherName), arguments[3].PropertyName);
        Assert.True(viewModel.HasErrors);
    }

    /// <summary>
    /// Verifies that we detach and dispose the disposable validations once the
    /// <see cref="ValidationHelper"/> is disposed. Also, here we ensure that
    /// the property change subscriptions are unsubscribed.
    /// </summary>
    [Fact]
    public void ShouldDetachAndDisposeTheComponentWhenValidationHelperDisposes()
    {
        var view = new IndeiTestView(new IndeiTestViewModel { Name = string.Empty });
        var arguments = new List<DataErrorsChangedEventArgs>();
        view.ViewModel.ErrorsChanged += (_, args) => arguments.Add(args);

        var helper = view
            .ViewModel
            .ValidationRule(
                viewModel => viewModel.Name,
                name => !string.IsNullOrWhiteSpace(name),
                "Name shouldn't be empty.");

        Assert.Equal(1, view.ViewModel.ValidationContext.Validations.Count);
        Assert.False(view.ViewModel.ValidationContext.IsValid);
        Assert.True(view.ViewModel.HasErrors);
        Assert.Equal(1, arguments.Count);
        Assert.Equal(nameof(view.ViewModel.Name), arguments[0].PropertyName);

        helper.Dispose();

        Assert.Equal(0, view.ViewModel.ValidationContext.Validations.Count);
        Assert.True(view.ViewModel.ValidationContext.IsValid);
        Assert.False(view.ViewModel.HasErrors);
        Assert.Equal(2, arguments.Count);
        Assert.Equal(nameof(view.ViewModel.Name), arguments[1].PropertyName);
    }

    /// <summary>
    /// Verifies that we support custom formatters in our <see cref="INotifyDataErrorInfo"/> implementation.
    /// </summary>
    [Fact]
    public void ShouldInvokeCustomFormatters()
    {
        var formatter = new PrefixFormatter("Validation error:");
        var view = new IndeiTestView(new IndeiTestViewModel(formatter) { Name = string.Empty });
        var arguments = new List<DataErrorsChangedEventArgs>();

        view.ViewModel.ErrorsChanged += (_, args) => arguments.Add(args);
        view.ViewModel.ValidationRule(
            viewModel => viewModel.Name,
            name => !string.IsNullOrWhiteSpace(name),
            "Name shouldn't be empty.");

        Assert.Equal(1, view.ViewModel.ValidationContext.Validations.Count);
        Assert.False(view.ViewModel.ValidationContext.IsValid);
        Assert.True(view.ViewModel.HasErrors);

        var errors = view.ViewModel
            .GetErrors("Name")
            .Cast<string>()
            .ToArray();

        Assert.Single(errors);
        Assert.Equal("Validation error: Name shouldn't be empty.", errors[0]);
    }

    private class PrefixFormatter : IValidationTextFormatter<string>
    {
        private readonly string _prefix;

        public PrefixFormatter(string prefix) => _prefix = prefix;

        public string Format(IValidationText validationText) => $"{_prefix} {validationText.ToSingleLine()}";
    }
}
