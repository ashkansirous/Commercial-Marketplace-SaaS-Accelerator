// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Microsoft.Marketplace.SaaS.SDK.Services.Models;

/// <summary>
///     Subscription Response.
/// </summary>
/// <seealso cref="SaasKit.Models.SaaSApiResult" />
public class SubscriptionResult : SaaSApiResult
{
    public string Action { get; set; }
    public Guid ActivityId { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorStatusCode { get; set; }

    [JsonPropertyName("id")]
    [DisplayName("Subscription Id")]
    public Guid Id { get; set; }

    [JsonPropertyName("publisherId")] public string PublisherId { get; set; }
    [JsonPropertyName("offerId")] public string OfferId { get; set; }
    public string OperationRequestSource { get; set; }

    [JsonPropertyName("name")]
    [DisplayName("Subscription Name")]
    public string Name { get; set; }

    [JsonPropertyName("saasSubscriptionStatus")]
    [DisplayName("Subscription Status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SubscriptionStatusEnum SaasSubscriptionStatus { get; set; }

    [DisplayName("Plan Id")]
    [JsonPropertyName("planId")]
    public string PlanId { get; set; }

    [JsonPropertyName("quantity")] public int Quantity { get; set; }
    public Uri ResourceLocation { get; set; }
    public string FulfillmentId { get; set; }
    public string StoreFront { get; set; }
    [DisplayName("Subscription Name")] public string SubscriptionName { get; set; }
    public bool IsActiveSubscription { get; set; }
    public DateTimeOffset TimeStamp { get; set; }
    public int SubscribeId { get; set; }
    public string SelectedPlanId { get; set; }
    public List<PlanDetailResult> PlanList { get; set; }
    public bool ShowWelcomeScreen { get; set; }
    [JsonPropertyName("purchaser")] public PurchaserResult Purchaser { get; set; }
    [JsonPropertyName("beneficiary")] public BeneficiaryResult Beneficiary { get; set; }
    [JsonPropertyName("term")] public TermResult Term { get; set; }
    public string CustomerEmailAddress { get; set; }
    public string CustomerName { get; set; }
}