﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.
namespace Microsoft.Marketplace.SaaS.SDK.Services.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Shows Operation Status.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OperationStatusEnum
    {
        /// <summary>
        /// The not started
        /// </summary>
        NotStarted,

        /// <summary>
        /// The in progress
        /// </summary>
        InProgress,

        /// <summary>
        /// The failed
        /// </summary>
        Failed,

        /// <summary>
        /// The succeeded
        /// </summary>
        Succeeded,

        /// <summary>
        /// The conflict
        /// </summary>
        Conflict,
    }
}