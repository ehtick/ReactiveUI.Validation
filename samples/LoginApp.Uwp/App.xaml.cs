﻿// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LoginApp.Uwp.Views;

namespace LoginApp.Uwp
{
    /// <summary>
    /// Defines the main Universal Windows Application class.
    /// </summary>
    /// <inheritdoc />
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App() => InitializeComponent();

        /// <inheritdoc />
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated) return;
            if (rootFrame.Content == null) 
                rootFrame.Navigate(typeof(SignUpView), e.Arguments);

            Window.Current.Activate();
        }
    }
}