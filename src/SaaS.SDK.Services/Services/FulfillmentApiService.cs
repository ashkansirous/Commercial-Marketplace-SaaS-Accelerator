// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Microsoft.Marketplace.SaaS.Models;
using Microsoft.Marketplace.SaaS.SDK.Services.Configurations;
using Microsoft.Marketplace.SaaS.SDK.Services.Contracts;
using Microsoft.Marketplace.SaaS.SDK.Services.Exceptions;
using Microsoft.Marketplace.SaaS.SDK.Services.Helpers;
using Microsoft.Marketplace.SaaS.SDK.Services.Models;

namespace Microsoft.Marketplace.SaaS.SDK.Services.Services;

/// <summary>
///     Fulfillment API Client Action-List For Subscriptions.
/// </summary>
/// <seealso cref="Microsoft.Marketplace.SaaS.SDK.Services.Contracts.IFulfilmentApiClient" />
public class FulfillmentApiService : BaseApiService, IFulfillmentApiService
{
    /// <summary>
    ///     Gets or sets the Marketplace SaaS client.
    /// </summary>
    /// <value>
    ///     The SDK settings.
    /// </value>
    private readonly IMarketplaceSaaSClient marketplaceClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FulfillmentApiService" /> class.
    /// </summary>
    /// <param name="sdkSettings">The SDK settings.</param>
    /// <param name="logger">The logger.</param>
    public FulfillmentApiService(IMarketplaceSaaSClient marketplaceClient,
        SaaSApiClientConfiguration sdkSettings,
        ILogger logger) : base(logger)
    {
        this.marketplaceClient = marketplaceClient;
        ClientConfiguration = sdkSettings;
        Logger = logger;
    }


    /// <summary>
    ///     Gets or sets the logger.
    /// </summary>
    /// <value>
    ///     The logger.
    /// </value>
    protected ILogger Logger { get; set; }

    /// <summary>
    ///     Gets or sets the SDK settings.
    /// </summary>
    /// <value>
    ///     The SDK settings.
    /// </value>
    protected SaaSApiClientConfiguration ClientConfiguration { get; set; }

    /// <summary>
    ///     Get all subscriptions asynchronously.
    /// </summary>
    /// <returns> List of subscriptions.</returns>
    public async Task<List<SubscriptionResult>> GetAllSubscriptionAsync()
    {
        Logger?.Info("Inside GetAllSubscriptionAsync() of FulfillmentApiService, trying to get All Subscriptions.");
        try
        {
            var subscriptions = await marketplaceClient.Fulfillment.ListSubscriptionsAsync().ToListAsync();
            return subscriptions.subscriptionResultList();
        }
        catch (RequestFailedException ex)
        {
            ProcessErrorResponse(MarketplaceActionEnum.GET_ALL_SUBSCRIPTIONS, ex);
            return null;
        }
    }

    /// <summary>
    ///     Gets Subscription By SubscriptionId.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <returns>
    ///     Returns Subscription By SubscriptionId.
    /// </returns>
    public async Task<SubscriptionResult> GetSubscriptionByIdAsync(Guid subscriptionId)
    {
        Logger?.Info(
            $"Inside GetSubscriptionByIdAsync() of FulfillmentApiService, trying to gets the Subscription Detail by subscriptionId : {subscriptionId}");
        try
        {
            var subscription = (await marketplaceClient.Fulfillment.GetSubscriptionAsync(subscriptionId)).Value;
            return subscription.subscriptionResult();
        }
        catch (RequestFailedException ex)
        {
            ProcessErrorResponse(MarketplaceActionEnum.GET_SUBSCRIPTION, ex);
            return null;
        }
    }


    public SubscriptionResult GetSubscriptionById(Guid subscriptionId)
    {
        Logger?.Info(
            $"Inside GetSubscriptionById() of FulfillmentApiService, trying to gets the Subscription Detail by subscriptionId : {subscriptionId}");
        try
        {
            var subscription = marketplaceClient.Fulfillment.GetSubscription(subscriptionId).Value;
            return subscription.subscriptionResult();
        }
        catch (RequestFailedException ex)
        {
            ProcessErrorResponse(MarketplaceActionEnum.GET_SUBSCRIPTION, ex);
            return null;
        }
    }

    /// <summary>
    ///     Resolves the Subscription.
    /// </summary>
    /// <param name="marketPlaceAccessToken">The marketPlace access token.</param>
    /// <returns>
    ///     Resolve Subscription.
    /// </returns>
    public async Task<ResolvedSubscriptionResult> ResolveAsync(string marketPlaceAccessToken)
    {
        Logger?.Info(
            "Inside ResolveAsync() of FulfillmentApiService, trying to resolve the Subscription by MarketPlaceToken");
        try
        {
            var resolvedSubscription = (await marketplaceClient.Fulfillment.ResolveAsync(marketPlaceAccessToken)).Value;
            return resolvedSubscription.resolvedSubscriptionResult();
        }
        catch (RequestFailedException ex)
        {
            ProcessErrorResponse(MarketplaceActionEnum.RESOLVE, ex);
            return null;
        }
    }

    /// <summary>
    ///     GetAllPlansForSubscription By SubscriptionId.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <returns>
    ///     Get AllPlans For SubscriptionId.
    /// </returns>
    /// <exception cref="FulfillmentException">Invalid subscription ID.</exception>
    public async Task<List<PlanDetailResultExtension>> GetAllPlansForSubscriptionAsync(Guid subscriptionId)
    {
        Logger?.Info(
            $"Inside GetAllPlansForSubscriptionAsync() of FulfillmentApiService, trying to Get All Plans for {subscriptionId}");
        if (subscriptionId != default)
            try
            {
                var availablePlans =
                    (await marketplaceClient.Fulfillment.ListAvailablePlansAsync(subscriptionId)).Value;
                return availablePlans.Plans.planResults();
            }
            catch (RequestFailedException ex)
            {
                ProcessErrorResponse(MarketplaceActionEnum.GET_ALL_PLANS, ex);
                return null;
            }

        throw new MarketplaceException("Invalid subscription ID", SaasApiErrorCode.BadRequest);
    }

    /// <summary>
    ///     Changes the plan for subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <param name="subscriptionPlanID">The subscription plan identifier.</param>
    /// <returns>
    ///     Change Plan For Subscription.
    /// </returns>
    /// <exception cref="FulfillmentException">Invalid subscription ID.</exception>
    public async Task<SubscriptionUpdateResult> ChangePlanForSubscriptionAsync(Guid subscriptionId,
        string subscriptionPlanID)
    {
        Logger?.Info(
            $"Inside ChangePlanForSubscriptionAsync() of FulfillmentApiService, trying to Change Plan By {subscriptionId} with New Plan {subscriptionPlanID}");
        if (subscriptionId != default)
            try
            {
                var operationId = await marketplaceClient.Fulfillment.UpdateSubscriptionAsync(subscriptionId,
                    new SubscriberPlan { PlanId = subscriptionPlanID });
                return new SubscriptionUpdateResult { OperationIdFromClientLib = operationId };
            }
            catch (Exception ex)
            {
                ProcessErrorResponse(MarketplaceActionEnum.CHANGE_PLAN, ex);
                return null;
            }

        throw new MarketplaceException("Invalid subscription ID", SaasApiErrorCode.BadRequest);
    }

    /// <summary>
    ///     Changes the quantity for subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <param name="subscriptionQuantity">The subscription quantity identifier.</param>
    /// <returns>
    ///     Change Quantity For Subscription.
    /// </returns>
    /// <exception cref="FulfillmentException">Invalid subscription ID.</exception>
    public async Task<SubscriptionUpdateResult> ChangeQuantityForSubscriptionAsync(Guid subscriptionId,
        int? subscriptionQuantity)
    {
        Logger?.Info(
            $"Inside ChangeQuantityForSubscriptionAsync() of FulfillmentApiService, trying to Change Quantity By {subscriptionId} with New Quantity {subscriptionQuantity}");
        if (subscriptionId != default)
            try
            {
                var operationId = await marketplaceClient.Fulfillment.UpdateSubscriptionAsync(subscriptionId,
                    new SubscriberPlan { Quantity = subscriptionQuantity });
                return new SubscriptionUpdateResult { OperationIdFromClientLib = operationId };
            }
            catch (RequestFailedException ex)
            {
                ProcessErrorResponse(MarketplaceActionEnum.CHANGE_QUANTITY, ex);
                return null;
            }

        throw new MarketplaceException("Invalid subscription ID", SaasApiErrorCode.BadRequest);
    }

    /// <summary>
    ///     Gets the operation status result.
    /// </summary>
    /// <param name="subscriptionId">The subscription.</param>
    /// <param name="operationId">The operation location.</param>
    /// <returns>
    ///     Get Operation Status Result.
    /// </returns>
    /// <exception cref="System.Exception">Error occurred while getting the operation result.</exception>
    public async Task<OperationResult> GetOperationStatusResultAsync(Guid subscriptionId, Guid operationId)
    {
        Logger?.Info(
            $"Inside GetOperationStatusResultAsync() of FulfillmentApiService, trying to Get Operation Status By Operation ID : {operationId}");
        try
        {
            var operationDetails =
                (await marketplaceClient.Operations.GetOperationStatusAsync(subscriptionId, operationId)).Value;
            return operationDetails.operationResult();
        }
        catch (RequestFailedException ex)
        {
            ProcessErrorResponse(MarketplaceActionEnum.OPERATION_STATUS, ex);
            return null;
        }
    }

    /// <summary>
    ///     Repond Failure on the operation status result.
    /// </summary>
    /// <param name="subscriptionId">The subscription.</param>
    /// <param name="operationId">The operation location.</param>
    /// <param name="updateOperationStatus">The operation status to patch with.</param>
    /// <returns>
    ///     Patch Operation Status Result.
    /// </returns>
    /// <exception cref="System.Exception">Error occurred while getting the operation result.</exception>
    public async Task<Response> PatchOperationStatusResultAsync(Guid subscriptionId, Guid operationId,
        UpdateOperationStatusEnum updateOperationStatus)
    {
        Logger?.Info(
            $"Inside PatchOperationStatusResultAsync() of FulfillmentApiService, trying to Update Operation Status to {updateOperationStatus} Operation ID : {operationId} Subscription ID : {subscriptionId}");
        try
        {
            var updateOperation = new UpdateOperation();
            updateOperation.Status = updateOperationStatus;
            return await marketplaceClient.Operations.UpdateOperationStatusAsync(subscriptionId, operationId,
                updateOperation);
        }
        catch (RequestFailedException ex)
        {
            ProcessErrorResponse(MarketplaceActionEnum.UPDATE_OPERATION_STATUS, ex);
            return null;
        }
    }


    /// <summary>
    ///     Deletes the subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <param name="subscriptionPlanID">The subscription plan identifier.</param>
    /// <returns>
    ///     Delete Subscription.
    /// </returns>
    public async Task<SubscriptionUpdateResult> DeleteSubscriptionAsync(Guid subscriptionId, string subscriptionPlanID)
    {
        Logger?.Info(
            $"Inside DeleteSubscriptionAsync() of FulfillmentApiService, trying to Delete Subscription :: {subscriptionId}");
        try
        {
            var operationId = await marketplaceClient.Fulfillment.DeleteSubscriptionAsync(subscriptionId);
            return new SubscriptionUpdateResult { OperationIdFromClientLib = operationId };
        }
        catch (RequestFailedException ex)
        {
            ProcessErrorResponse(MarketplaceActionEnum.DELETE, ex);
            return null;
        }
    }

    /// <summary>
    ///     Activates the subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <param name="subscriptionPlanId">The subscription plan identifier.</param>
    /// <returns>
    ///     Activate Subscription.
    /// </returns>
    public async Task<Response> ActivateSubscriptionAsync(Guid subscriptionId, string subscriptionPlanId)
    {
        Logger?.Info(
            $"Inside ActivateSubscriptionAsync() of FulfillmentApiService, trying to Activate Subscription :: {subscriptionId}");
        try
        {
            return await marketplaceClient.Fulfillment.ActivateSubscriptionAsync(subscriptionId,
                new SubscriberPlan { PlanId = subscriptionPlanId });
        }
        catch (RequestFailedException ex)
        {
            ProcessErrorResponse(MarketplaceActionEnum.ACTIVATE, ex);
            return null;
        }
    }

    //private void ProcessErrorResponse(MarketplaceActionEnum marketplaceAction, Exception ex)
    //{
    //    int statusCode =0;
    //    if(ex.InnerException != null && ex.InnerException is Identity.Client.MsalServiceException msalInnerException)
    //    {
    //        statusCode = msalInnerException.StatusCode;
    //    }
    //    else if(ex is RequestFailedException reqFailedInnerException)
    //    {
    //        statusCode = reqFailedInnerException.Status;
    //    }

    //    if (statusCode != 0)
    //    {
    //        Enum.TryParse<HttpStatusCode>(statusCode.ToString(), out HttpStatusCode httpStatusCode);

    //        this.Logger?.Error("Error while completing the request as " + JsonSerializer.Serialize(new { Error = ex.Message, }));

    //        if (httpStatusCode == HttpStatusCode.Unauthorized || httpStatusCode == HttpStatusCode.Forbidden)
    //        {
    //            throw new FulfillmentException("Token invalid or expired. Please try again.", SaasApiErrorCode.Unauthorized);
    //        }
    //        else if (httpStatusCode == HttpStatusCode.NotFound)
    //        {
    //            this.Logger?.Warn("Returning the error as " + JsonSerializer.Serialize(new { Error = "Not Found" }));
    //            throw new FulfillmentException(string.Format("Unable to find the request {0}", marketplaceAction), SaasApiErrorCode.NotFound);
    //        }
    //        else if (httpStatusCode == HttpStatusCode.Conflict)
    //        {
    //            this.Logger?.Warn("Returning the error as " + JsonSerializer.Serialize(new { Error = "Conflict" }));
    //            throw new FulfillmentException(string.Format("Conflict came for {0}", marketplaceAction), SaasApiErrorCode.Conflict);
    //        }
    //        else if (httpStatusCode == HttpStatusCode.BadRequest)
    //        {
    //            this.Logger?.Warn("Returning the error as " + JsonSerializer.Serialize(new { Error = "Bad Request" }));
    //            throw new FulfillmentException(string.Format("Unable to process the request {0}, server responding as BadRequest. Please verify the post data. ", marketplaceAction), SaasApiErrorCode.BadRequest);
    //        }
    //        else
    //        {
    //            this.Logger?.Warn("Returning the error as " + JsonSerializer.Serialize(new { Error = "Unknown Error" }));
    //            throw new FulfillmentException(string.Format("Unable to process the request {0}, server responding as BadRequest. Please verify the post data. ", marketplaceAction), httpStatusCode.ToString());
    //        }
    //    }

    //    this.Logger?.Error("Error while completing the request as " + JsonSerializer.Serialize(new { Error = ex.Message, }));
    //    throw new FulfillmentException("Something went wrong, please check logs!");
    //}

    /// <summary>
    ///     Gets the saas application URL.
    /// </summary>
    /// <returns>SaaS App URL.</returns>
    public string GetSaaSAppURL()
    {
        try
        {
            return ClientConfiguration.SaaSAppUrl;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        // return string.Empty;
    }

    /// <summary>
    ///     Get all subscriptions synchronously.
    /// </summary>
    /// <returns> List of subscriptions.</returns>
    public List<SubscriptionResult> GetAllSubscriptions()
    {
        Logger?.Info("Inside GetAllSubscriptions() of FulfillmentApiService, trying to get All Subscriptions.");
        try
        {
            var subscriptions = marketplaceClient.Fulfillment.ListSubscriptions().ToList();
            return subscriptions.subscriptionResultList();
        }
        catch (RequestFailedException ex)
        {
            ProcessErrorResponse(MarketplaceActionEnum.GET_ALL_SUBSCRIPTIONS, ex);
            return null;
        }
    }
}