// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Marketplace.SaaS.SDK.Services.Contracts;
using Microsoft.Marketplace.SaaS.SDK.Services.Exceptions;
using Microsoft.Marketplace.SaaS.SDK.Services.Models;
using Microsoft.Marketplace.SaaS.SDK.Services.Utilities;
using Microsoft.Marketplace.Saas.Web.Controllers;

namespace SaaS.SDK.PublisherSolution.Controllers;

[ServiceFilter(typeof(KnownUserAttribute))]
public class HomeController : BaseController
{
    private readonly IFulfillmentApiService fulfillApiService;
    private readonly ILogger<HomeController> logger;

    public HomeController(ILogger<HomeController> logger,
        IFulfillmentApiService fulfillApiService
    )
    {
        this.logger = logger;
        this.fulfillApiService = fulfillApiService;
    }

    public IActionResult Index()
    {
        logger.LogInformation("Home Controller / Index ");
        try
        {
            GetCurrentUserDetail();
            return View();
        }
        catch (Exception ex)
        {
            logger.LogError("Message:{0} :: {1}   ", ex.Message, ex.InnerException);
            return View("Error", ex);
        }
    }

    public async Task<IActionResult> Subscriptions()
    {
        var subscriptionDetail = new SubscriptionViewModel();
        subscriptionDetail.Subscriptions = new List<SubscriptionResultExtension>();
        var userIdentity = User.Identity;
        if (userIdentity is not { IsAuthenticated: true }) return RedirectToAction(nameof(Index));
        TempData["ShowWelcomeScreen"] = "True";
        var allSubscriptions = await fulfillApiService.GetAllSubscriptionAsync();

        foreach (var subscription in allSubscriptions)
        {
            var allPlansForSubscription =
                await fulfillApiService.GetAllPlansForSubscriptionAsync(subscription.Id);
            foreach (var plan in allPlansForSubscription)
                subscriptionDetail.Subscriptions.Add(Map(subscription, plan, allPlansForSubscription));
        }

        if (TempData["ErrorMsg"] != null)
        {
            subscriptionDetail.IsSuccess = false;
            subscriptionDetail.ErrorMessage = Convert.ToString(TempData["ErrorMsg"]);
        }

        return View(subscriptionDetail);
    }

    //public IActionResult SubscriptionLogDetail(Guid subscriptionId)
    //{
    //    if (User.Identity.IsAuthenticated)
    //    {
    //        var subscriptionAudit = new List<SubscriptionAuditLogs>();
    //        subscriptionAudit = subscriptionLogRepository.GetSubscriptionBySubscriptionId(subscriptionId).ToList();
    //        return View(subscriptionAudit);
    //    }

    //    return RedirectToAction(nameof(Index));
    //}

    public async Task<IActionResult> SubscriptionDetails(Guid subscriptionId, string planId)
    {
        var subscriptionDetail = await GetSubscriptionDetail(subscriptionId, planId);
        return View(subscriptionDetail);
    }


    public async Task<IActionResult> DeActivateSubscription(Guid subscriptionId, string planId, string operation)
    {
        await fulfillApiService.DeleteSubscriptionAsync(subscriptionId, planId);
        var subscriptionDetail = await GetSubscriptionDetail(subscriptionId, planId);

        return View("ActivateSubscription", subscriptionDetail);
    }

    public async Task<IActionResult> SubscriptionOperation(Guid subscriptionId, string planId, string operation,
        int numberofProviders)
    {
        try
        {
            var subscription = await fulfillApiService.GetSubscriptionByIdAsync(subscriptionId);
            if (operation == "Activate" &&
                subscription.SaasSubscriptionStatus != SubscriptionStatusEnum.PendingActivation)
                await fulfillApiService.ActivateSubscriptionAsync(subscriptionId, planId);

            if (operation == "Deactivate") await fulfillApiService.DeleteSubscriptionAsync(subscriptionId, planId);

            return RedirectToAction(nameof(ActivatedMessage));
        }
        catch (Exception ex)
        {
            logger.LogInformation("Message:{0} :: {1}", ex.Message, ex.InnerException);
            return View("Error");
        }
    }

    public IActionResult ActivatedMessage()
    {
        try
        {
            return View("ProcessMessage");
        }
        catch (Exception ex)
        {
            return View("Error", ex);
        }
    }

    //public IActionResult RecordUsage(int subscriptionId)
    //{
    //    if (!User.Identity.IsAuthenticated) return RedirectToAction(nameof(Index));
    //    var subscriptionDetail = subscriptionRepo.Get(subscriptionId);
    //        var allDimensionsList = dimensionsRepository.GetDimensionsByPlanId(subscriptionDetail.AmpplanId);
    //        var usageViewModel = new SubscriptionUsageViewModel();
    //        usageViewModel.SubscriptionDetail = subscriptionDetail;
    //        usageViewModel.MeteredAuditLogs = new List<MeteredAuditLogs>();
    //        usageViewModel.MeteredAuditLogs = subscriptionUsageLogsRepository
    //            .GetMeteredAuditLogsBySubscriptionId(subscriptionId).OrderByDescending(s => s.CreatedDate).ToList();
    //        usageViewModel.DimensionsList = new SelectList(allDimensionsList, "Dimension", "Description");
    //        return View(usageViewModel);

    //}

    //public IActionResult RecordUsageNow(int subscriptionId, string dimId, string quantity)
    //{
    //    if (!User.Identity.IsAuthenticated) return RedirectToAction(nameof(Index));
    //    var subscriptionDetail = subscriptionRepo.Get(subscriptionId);
    //        var allDimensionsList = dimensionsRepository.GetDimensionsByPlanId(subscriptionDetail.AmpplanId);
    //        var usageViewModel = new SubscriptionUsageViewModel();
    //        usageViewModel.SubscriptionDetail = subscriptionDetail;
    //        usageViewModel.MeteredAuditLogs = new List<MeteredAuditLogs>();
    //        usageViewModel.MeteredAuditLogs = subscriptionUsageLogsRepository
    //            .GetMeteredAuditLogsBySubscriptionId(subscriptionId).OrderByDescending(s => s.CreatedDate).ToList();
    //        usageViewModel.DimensionsList = new SelectList(allDimensionsList, "Dimension", "Description");

    //        usageViewModel.SelectedDimension = dimId;
    //        usageViewModel.Quantity = quantity;
    //        return View("RecordUsage", usageViewModel);

    //}

    public async Task<IActionResult> SubscriptionQuantityDetail(Guid subscriptionId)
    {
        if (!User.Identity.IsAuthenticated) return RedirectToAction(nameof(Index));
        var subscriptionDetail = await GetSubscriptionDetail(subscriptionId, "");

        return View(subscriptionDetail);
    }

    //[HttpPost]
    //public IActionResult ManageSubscriptionUsage(SubscriptionUsageViewModel subscriptionData)
    //{
    //    if (subscriptionData != null && subscriptionData.SubscriptionDetail != null)
    //    {
    //        var currentUserDetail = userRepository.GetPartnerDetailFromEmail(CurrentUserEmailAddress);
    //        var subscriptionUsageRequest = new MeteringUsageRequest
    //        {
    //            Dimension = subscriptionData.SelectedDimension,
    //            EffectiveStartTime = DateTime.UtcNow,
    //            PlanId = subscriptionData.SubscriptionDetail.AmpplanId,
    //            Quantity = Convert.ToDouble(subscriptionData.Quantity ?? "0"),
    //            ResourceId = subscriptionData.SubscriptionDetail.AmpsubscriptionId
    //        };
    //        var meteringUsageResult = new MeteringUsageResult();
    //        var requestJson = JsonSerializer.Serialize(subscriptionUsageRequest);
    //        var responseJson = string.Empty;
    //        try
    //        {
    //            meteringUsageResult = billingApiService.EmitUsageEventAsync(subscriptionUsageRequest)
    //                .ConfigureAwait(false).GetAwaiter().GetResult();
    //            responseJson = JsonSerializer.Serialize(meteringUsageResult);
    //            logger.LogInformation(responseJson);
    //        }
    //        catch (MarketplaceException mex)
    //        {
    //            responseJson = JsonSerializer.Serialize(mex.MeteredBillingErrorDetail);
    //            meteringUsageResult.Status = mex.ErrorCode;
    //            logger.LogInformation(responseJson);
    //        }

    //        var newMeteredAuditLog = new MeteredAuditLogs
    //        {
    //            RequestJson = requestJson,
    //            ResponseJson = responseJson,
    //            StatusCode = meteringUsageResult.Status,
    //            SubscriptionId = subscriptionData.SubscriptionDetail.Id,
    //            SubscriptionUsageDate = DateTime.UtcNow,
    //            CreatedBy = currentUserDetail == null ? 0 : currentUserDetail.UserId,
    //            CreatedDate = DateTime.Now
    //        };
    //        subscriptionUsageLogsRepository.Save(newMeteredAuditLog);
    //    }

    //    return RedirectToAction(nameof("RecordUsage"), new { subscriptionId = subscriptionData.SubscriptionDetail.Id });
    //}

    public async Task<IActionResult> ViewSubscriptionDetail(Guid subscriptionId)
    {
        if (!User.Identity.IsAuthenticated) return RedirectToAction(nameof(Index));

        var subscriptionDetail = await GetSubscriptionDetail(subscriptionId, "");

        return View(subscriptionDetail);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exceptionDetail = HttpContext.Features.Get<IExceptionHandlerFeature>();
        return View(exceptionDetail?.Error);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeSubscriptionPlan(SubscriptionResult subscriptionDetail)
    {
        if (subscriptionDetail.Id == default || string.IsNullOrEmpty(subscriptionDetail.PlanId))
            return RedirectToAction(nameof(Subscriptions));

        var updateResult = await fulfillApiService
            .ChangePlanForSubscriptionAsync(subscriptionDetail.Id, subscriptionDetail.PlanId);
        var changePlanOperationStatus = OperationStatusEnum.InProgress;

        if (updateResult == null || updateResult.OperationId == default)
            return RedirectToAction(nameof(Subscriptions));

        var counter = 0;

        while (OperationStatusEnum.InProgress.Equals(changePlanOperationStatus) ||
               OperationStatusEnum.NotStarted.Equals(changePlanOperationStatus))
        {
            var changePlanOperationResult = await fulfillApiService
                .GetOperationStatusResultAsync(subscriptionDetail.Id, updateResult.OperationId);

            changePlanOperationStatus = changePlanOperationResult.Status;

            await Task.Delay(5000);
            counter++;
            if (counter > 100)
                break;
        }

        if (changePlanOperationStatus != OperationStatusEnum.Succeeded)
            throw new MarketplaceException(
                $"Plan change operation failed with operation status {changePlanOperationStatus}. Check if the updates are allowed in the App config \"AcceptSubscriptionUpdates\" key or db application log for more information.");


        return RedirectToAction(nameof(Subscriptions));
    }

    //[HttpPost]
    //public async Task<IActionResult> ChangeSubscriptionQuantity(SubscriptionResult subscriptionDetail)
    //{
    //    logger.LogInformation("Home Controller / ChangeSubscriptionPlan  subscriptionDetail:{0}",
    //        JsonSerializer.Serialize(subscriptionDetail));
    //    if (User.Identity.IsAuthenticated)
    //        try
    //        {
    //            if (subscriptionDetail != null && subscriptionDetail.Id != default && subscriptionDetail.Quantity > 0)
    //                try
    //                {
    //                    //initiate change quantity
    //                    var currentUserId = userService.GetUserIdFromEmailAddress(CurrentUserEmailAddress);
    //                    var jsonResult = await fulfillApiService
    //                        .ChangeQuantityForSubscriptionAsync(subscriptionDetail.Id, subscriptionDetail.Quantity)
    //                        .ConfigureAwait(false);
    //                    var changeQuantityOperationStatus = OperationStatusEnum.InProgress;

    //                    if (jsonResult != null && jsonResult.OperationId != default)
    //                    {
    //                        var _counter = 0;

    //                        //loop untill the operation status has moved away from inprogress or notstarted, generally this will be the result of webhooks' action aganist this operation
    //                        while (OperationStatusEnum.InProgress.Equals(changeQuantityOperationStatus) ||
    //                               OperationStatusEnum.NotStarted.Equals(changeQuantityOperationStatus))
    //                        {
    //                            var changeQuantityOperationResult = await fulfillApiService
    //                                .GetOperationStatusResultAsync(subscriptionDetail.Id, jsonResult.OperationId)
    //                                .ConfigureAwait(false);
    //                            changeQuantityOperationStatus = changeQuantityOperationResult.Status;

    //                            logger.LogInformation(
    //                                $"Quantity Change Progress. SubscriptionId: {subscriptionDetail.Id} ToQuantity: {subscriptionDetail.Quantity} UserId: {currentUserId} OperationId: {jsonResult.OperationId} Operationstatus: {changeQuantityOperationStatus}.");
    //                            await applicationLogService.AddApplicationLog(
    //                                    $"Quantity Change Progress. SubscriptionId: {subscriptionDetail.Id} ToQuantity: {subscriptionDetail.Quantity} UserId: {currentUserId} OperationId: {jsonResult.OperationId} Operationstatus: {changeQuantityOperationStatus}.")
    //                                .ConfigureAwait(false);

    //                            //wait and check every 5secs
    //                            await Task.Delay(5000);
    //                            _counter++;
    //                            if (_counter > 100)
    //                                //if loop has been executed for more than 100 times then break, to avoid infinite loop just in case
    //                                break;
    //                        }

    //                        if (changeQuantityOperationStatus == OperationStatusEnum.Succeeded)
    //                        {
    //                            logger.LogInformation(
    //                                $"Quantity Change Success. SubscriptionId: {subscriptionDetail.Id} ToQuantity: {subscriptionDetail.Quantity} UserId: {currentUserId} OperationId: {jsonResult.OperationId}.");
    //                            await applicationLogService
    //                                .AddApplicationLog(
    //                                    $"Quantity Change Success. SubscriptionId: {subscriptionDetail.Id} ToQuantity: {subscriptionDetail.Quantity} UserId: {currentUserId} OperationId: {jsonResult.OperationId}.")
    //                                .ConfigureAwait(false);
    //                        }
    //                        else
    //                        {
    //                            logger.LogInformation(
    //                                $"Quantity Change Failed. SubscriptionId: {subscriptionDetail.Id} ToQuantity: {subscriptionDetail.Quantity} UserId: {currentUserId} OperationId: {jsonResult.OperationId} Operationstatus: {changeQuantityOperationStatus}.");
    //                            await applicationLogService.AddApplicationLog(
    //                                    $"Quantity Change Failed. SubscriptionId: {subscriptionDetail.Id} ToQuantity: {subscriptionDetail.Quantity} UserId: {currentUserId} OperationId: {jsonResult.OperationId} Operationstatus: {changeQuantityOperationStatus}.")
    //                                .ConfigureAwait(false);

    //                            throw new MarketplaceException(
    //                                $"Quantity Change operation failed with operation status {changeQuantityOperationStatus}. Check if the updates are allowed in the App config \"AcceptSubscriptionUpdates\" key or db application log for more information.");
    //                        }
    //                    }
    //                }
    //                catch (MarketplaceException fex)
    //                {
    //                    TempData["ErrorMsg"] = fex.Message;
    //                    logger.LogError("Message:{0} :: {1}   ", fex.Message, fex.InnerException);
    //                }

    //            return RedirectToAction(nameof(Subscriptions));
    //        }
    //        catch (Exception ex)
    //        {
    //            logger.LogError("Message:{0} :: {1}   ", ex.Message, ex.InnerException);
    //            return View("Error", ex);
    //        }

    //    return RedirectToAction(nameof(Index));
    //}

    [HttpPost]
    public async Task<IActionResult> FetchAllSubscriptions()
    {
        return RedirectToAction(nameof(Subscriptions));
    }

    private SubscriptionResultExtension Map(SubscriptionResult subscription, PlanDetailResultExtension planDetail,
        List<PlanDetailResultExtension> allPlans)
    {
        var subscritpionDetail = new SubscriptionResultExtension
        {
            Id = subscription.Id,
            SubscribeId = subscription.SubscribeId,
            PlanId = planDetail.PlanId,
            Quantity = subscription.Quantity,
            Name = subscription.Name,
            SubscriptionStatus =
                SubscriptionStatusEnumExtension.UnRecognized, //subscription.SaasSubscriptionStatus, todo: check
            IsActiveSubscription = subscription.IsActiveSubscription,
            CustomerEmailAddress = subscription.CustomerEmailAddress,
            CustomerName = subscription.CustomerName,
            IsMeteringSupported = false // todo: check
        };
        subscritpionDetail.Purchaser = new PurchaserResult();

        subscritpionDetail.Purchaser.EmailId = subscription.Purchaser.EmailId;
        subscritpionDetail.Purchaser.TenantId = subscription.Purchaser.TenantId;
        subscritpionDetail.PlanList = allPlans.Select(p => new PlanDetailResult
        {
            PlanId = p.PlanId,
            Description = p.Description,
            DisplayName = p.DisplayName,
            HasFreeTrials = p.HasFreeTrials,
            Id = p.Id,
            IsPrivate = p.IsPrivate,
            IsStopSell = p.IsStopSell,
            Market = p.Market,
            PlanComponents = p.PlanComponents,
            RequestID = p.RequestID
        }).ToList();
        return subscritpionDetail;
    }

    private async Task<SubscriptionResultExtension> GetSubscriptionDetail(Guid subscriptionId, string planId)
    {
        var subscription = await fulfillApiService.GetSubscriptionByIdAsync(subscriptionId);
        var plans = await fulfillApiService.GetAllPlansForSubscriptionAsync(subscription.Id);
        var plan = plans.SingleOrDefault(p => p.PlanId == planId);
        var subscriptionDetail = Map(subscription, plan, plans);
        return subscriptionDetail;
    }
}