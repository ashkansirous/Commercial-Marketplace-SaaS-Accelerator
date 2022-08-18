// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Microsoft.Marketplace.SaaS.Models;
using Microsoft.Marketplace.SaaS.SDK.Services.Models;

namespace Microsoft.Marketplace.SaaS.SDK.Services.Contracts;

/// <summary>
///     Interface AMPClient.
/// </summary>
public interface IFulfillmentApiService
{
    Task<SubscriptionResult> GetSubscriptionByIdAsync(Guid subscriptionId);

    SubscriptionResult GetSubscriptionById(Guid subscriptionId);

    Task<ResolvedSubscriptionResult> ResolveAsync(string marketPlaceAccessToken);

    Task<List<PlanDetailResultExtension>> GetAllPlansForSubscriptionAsync(Guid subscriptionId);

    Task<Response> ActivateSubscriptionAsync(Guid subscriptionId, string subscriptionPlanID);

    Task<SubscriptionUpdateResult> ChangePlanForSubscriptionAsync(Guid subscriptionId, string subscriptionPlanID);

    Task<SubscriptionUpdateResult> ChangeQuantityForSubscriptionAsync(Guid subscriptionId, int? subscriptionQuantity);

    Task<OperationResult> GetOperationStatusResultAsync(Guid subscriptionId, Guid operationId);

    Task<Response> PatchOperationStatusResultAsync(Guid subscriptionId, Guid operationId,
        UpdateOperationStatusEnum updateOperationStatus);

    Task<SubscriptionUpdateResult> DeleteSubscriptionAsync(Guid subscriptionId, string subscriptionPlanID);

    Task<List<SubscriptionResult>> GetAllSubscriptionAsync();

    string GetSaaSAppURL();
}