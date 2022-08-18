using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Marketplace.SaasKit.Client.DataAccess.Context;
using Microsoft.Marketplace.SaasKit.Client.DataAccess.Contracts;
using Microsoft.Marketplace.SaasKit.Client.DataAccess.DataModel;
using Microsoft.Marketplace.SaasKit.Client.DataAccess.Entities;

namespace Microsoft.Marketplace.SaasKit.Client.DataAccess.Services;

/// <summary>
///     Plans Repository.
/// </summary>
/// <seealso cref="Microsoft.Marketplace.SaasKit.DAL.Interface.IPlansRepository" />
public class PlansRepository : IPlansRepository
{
    /// <summary>
    ///     The application configuration repository.
    /// </summary>
    private readonly IApplicationConfigRepository applicationConfigRepository;

    /// <summary>
    ///     The context.
    /// </summary>
    private readonly SaasKitContext context;

    /// <summary>
    ///     The disposed.
    /// </summary>
    private bool disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PlansRepository" /> class.
    /// </summary>
    /// <param name="context">The this.context.</param>
    /// <param name="applicationConfigRepository">The application configuration repository.</param>
    public PlansRepository(SaasKitContext context, IApplicationConfigRepository applicationConfigRepository)
    {
        this.context = context;
        this.applicationConfigRepository = applicationConfigRepository;
    }

    /// <summary>
    ///     Gets all the records.
    /// </summary>
    /// <returns> List of plans.</returns>
    public IEnumerable<Plans> Get()
    {
        return context.Plans;
    }

    /// <summary>
    ///     Gets the specified identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns> Plans.</returns>
    public Plans Get(int id)
    {
        return context.Plans.Where(s => s.Id == id).FirstOrDefault();
    }

    /// <summary>
    ///     Adds the specified plan details.
    /// </summary>
    /// <param name="planDetails">The plan details.</param>
    /// <returns> Plan Id.</returns>
    public int Save(Plans planDetails)
    {
        if (planDetails != null && !string.IsNullOrEmpty(planDetails.PlanId))
        {
            var existingPlan = context.Plans.Include(p => p.MeteredDimensions)
                .Where(s => s.PlanId == planDetails.PlanId).FirstOrDefault();
            if (existingPlan != null)
            {
                //room for improvement as these values dont change we dont make a db trip if something changes?
                existingPlan.PlanId = planDetails.PlanId;
                existingPlan.Description = planDetails.Description;
                existingPlan.DisplayName = planDetails.DisplayName;
                existingPlan.OfferId = planDetails.OfferId;
                existingPlan.IsmeteringSupported = planDetails.IsmeteringSupported;
                CheckMeteredDimension(planDetails, existingPlan);
                context.Plans.Update(existingPlan);
                context.SaveChanges();
                return existingPlan.Id;
            }

            context.Plans.Add(planDetails);
            context.SaveChanges();
            return planDetails.Id;
        }

        return 0;
    }

    /// <summary>
    ///     Gets the plan detail by plan identifier.
    /// </summary>
    /// <param name="planId">The plan identifier.</param>
    /// <returns> Plan.</returns>
    public Plans GetById(string planId)
    {
        return context.Plans.Where(s => s.PlanId == planId).FirstOrDefault();
    }

    /// <summary>
    ///     Gets the plans by user.
    /// </summary>
    /// <returns> List of Plans.</returns>
    public IEnumerable<Plans> GetPlansByUser()
    {
        var getAllPlans = context.Plans;
        return getAllPlans;
    }

    /// <summary>
    ///     Gets the plan detail by plan identifier.
    /// </summary>
    /// <param name="planGuId">The plan gu identifier.</param>
    /// <returns>
    ///     Plan detail for the internal reference (GUID).
    /// </returns>
    public Plans GetByInternalReference(Guid planGuId)
    {
        return context.Plans.Where(s => s.PlanGuid == planGuId).FirstOrDefault();
    }

    /// <summary>
    ///     Gets the plan detail by plan identifier.
    /// </summary>
    /// <param name="offerId">The offer identifier.</param>
    /// <returns> List of plans.</returns>
    public List<Plans> GetPlansByOfferId(Guid offerId)
    {
        return context.Plans.Where(s => s.OfferId == offerId).ToList();
    }

    /// <summary>
    ///     Gets the plan attribute on offer attribute identifier.
    /// </summary>
    /// <param name="offerAttributeId">The offer attribute identifier.</param>
    /// <param name="planGuId">The plan gu identifier.</param>
    /// <returns> Plan attributes.</returns>
    public PlanAttributeMapping GetPlanAttributeOnOfferAttributeId(int offerAttributeId, Guid planGuId)
    {
        var planAttribute = context.PlanAttributeMapping
            .Where(s => s.OfferAttributeId == offerAttributeId && s.PlanId == planGuId).FirstOrDefault();
        return planAttribute;
    }

    /// <summary>
    ///     Gets the plan attributes.
    /// </summary>
    /// <param name="planGuId">The plan gu identifier.</param>
    /// <param name="offerId">The offer identifier.</param>
    /// <returns> List type="of Plan attributes.</returns>
    public IEnumerable<PlanAttributesModel> GetPlanAttributes(Guid planGuId, Guid offerId)
    {
        try
        {
            var offerAttributescCall = context.PlanAttributeOutput.FromSqlRaw("dbo.spGetOfferParameters {0}", planGuId);
            var offerAttributes = offerAttributescCall.ToList();

            var attributesList = new List<PlanAttributesModel>();

            if (offerAttributes != null && offerAttributes.Count() > 0)
                foreach (var offerAttribute in offerAttributes)
                {
                    var planAttributes = new PlanAttributesModel();
                    planAttributes.PlanAttributeId = offerAttribute.PlanAttributeId;
                    planAttributes.PlanId = offerAttribute.PlanId;
                    planAttributes.OfferAttributeId = offerAttribute.OfferAttributeId;
                    planAttributes.IsEnabled = offerAttribute.IsEnabled;
                    planAttributes.DisplayName = offerAttribute.DisplayName;
                    planAttributes.Type = offerAttribute.Type;
                    attributesList.Add(planAttributes);
                }

            return attributesList;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    /// <summary>
    ///     Gets the events by plan.
    /// </summary>
    /// <param name="planGuId">The plan gu identifier.</param>
    /// <param name="offerId">The offer identifier.</param>
    /// <returns> Plan Events Model.</returns>
    public IEnumerable<PlanEventsModel> GetEventsByPlan(Guid planGuId, Guid offerId)
    {
        try
        {
            var allEvents = context.PlanEventsOutPut.FromSqlRaw("dbo.spGetPlanEvents {0}", planGuId).ToList();

            var eventsList = new List<PlanEventsModel>();

            if (allEvents != null && allEvents.Count() > 0)
                foreach (var events in allEvents)
                {
                    var planEvent = new PlanEventsModel();
                    planEvent.Id = events.Id;
                    planEvent.PlanId = events.PlanId;
                    planEvent.Isactive = events.Isactive;
                    planEvent.SuccessStateEmails = events.SuccessStateEmails;
                    planEvent.FailureStateEmails = events.FailureStateEmails;
                    planEvent.EventsName = events.EventsName;
                    planEvent.EventId = events.EventId;
                    planEvent.CopyToCustomer = events.CopyToCustomer ?? false;
                    if (planEvent.EventsName != "Pending Activation")
                        eventsList.Add(planEvent);
                    else if (!Convert.ToBoolean(
                                 applicationConfigRepository.GetValueByName("IsAutomaticProvisioningSupported")))
                        eventsList.Add(planEvent);
                }

            return eventsList;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    /// <summary>
    ///     Adds the plan attributes.
    /// </summary>
    /// <param name="planAttributes">The plan attributes.</param>
    /// <returns> Plan Attribute Id.</returns>
    public int? SavePlanAttributes(PlanAttributeMapping planAttributes)
    {
        if (planAttributes != null)
        {
            var existingPlanAttribute = context.PlanAttributeMapping.Where(s => s.PlanAttributeId ==
                planAttributes.PlanAttributeId).FirstOrDefault();
            if (existingPlanAttribute != null)
            {
                existingPlanAttribute.OfferAttributeId = planAttributes.OfferAttributeId;
                existingPlanAttribute.IsEnabled = planAttributes.IsEnabled;
                existingPlanAttribute.PlanId = planAttributes.PlanId;
                existingPlanAttribute.UserId = planAttributes.UserId;
                existingPlanAttribute.PlanAttributeId = planAttributes.PlanAttributeId;
                existingPlanAttribute.CreateDate = DateTime.Now;

                context.PlanAttributeMapping.Update(existingPlanAttribute);
                context.SaveChanges();
                return existingPlanAttribute.PlanAttributeId;
            }

            context.PlanAttributeMapping.Add(planAttributes);
            context.SaveChanges();
            return planAttributes.PlanAttributeId;
        }

        return null;
    }

    /// <summary>
    ///     Adds the plan events.
    /// </summary>
    /// <param name="planEvents">The plan events.</param>
    /// <returns> Plan Event Id.</returns>
    public int? AddPlanEvents(PlanEventsMapping planEvents)
    {
        if (planEvents != null)
        {
            var existingPlanEvents = context.PlanEventsMapping.Where(s => s.Id ==
                                                                          planEvents.Id).FirstOrDefault();
            if (existingPlanEvents != null)
            {
                existingPlanEvents.Id = planEvents.Id;
                existingPlanEvents.Isactive = planEvents.Isactive;
                existingPlanEvents.PlanId = planEvents.PlanId;
                existingPlanEvents.SuccessStateEmails = planEvents.SuccessStateEmails;
                existingPlanEvents.FailureStateEmails = planEvents.FailureStateEmails;
                existingPlanEvents.EventId = planEvents.EventId;
                existingPlanEvents.UserId = planEvents.UserId;
                existingPlanEvents.CreateDate = DateTime.Now;
                existingPlanEvents.CopyToCustomer = planEvents.CopyToCustomer;
                context.PlanEventsMapping.Update(existingPlanEvents);
                context.SaveChanges();
                return existingPlanEvents.Id;
            }

            context.PlanEventsMapping.Add(planEvents);
            context.SaveChanges();
            return planEvents.Id;
        }

        return null;
    }

    /// <summary>
    ///     Removes the specified plan details.
    /// </summary>
    /// <param name="planDetails">The plan details.</param>
    public void Remove(Plans planDetails)
    {
        var existingPlan = context.Plans.Where(s => s.Id == planDetails.Id).FirstOrDefault();
        if (existingPlan != null)
        {
            context.Plans.Remove(existingPlan);
            context.SaveChanges();
        }
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Check if there is Metered Dimensions exists or updated
    /// </summary>
    /// <param name="planDetails">Incoming Plans data from Payload</param>
    /// <param name="existingPlan">Existing Plans data from database</param>
    private void CheckMeteredDimension(Plans planDetails, Plans existingPlan)
    {
        // Check if Metered Dimension exists or new Metered Dimension to add
        foreach (var metered in planDetails.MeteredDimensions)
        {
            // Assign Plan.Id to metered PlandId
            metered.PlanId = existingPlan.Id;

            // Query DB for metered dimension using PlanID and Dimension ID
            var existingMeteredDimensions = context.MeteredDimensions
                .Where(s => s.PlanId == existingPlan.Id && s.Dimension == metered.Dimension).FirstOrDefault();

            // Check if Metered Dimensions exists
            if (existingMeteredDimensions != null)
            {
                // Metered Dimension exists. No needs to updated Keys. We could update description if it is modified.
                if (existingMeteredDimensions.Description != metered.Description)
                {
                    var existingMetered = existingPlan.MeteredDimensions
                        .Where(s => s.PlanId == existingPlan.Id && s.Dimension == metered.Dimension).FirstOrDefault();
                    existingMetered.Description = metered.Description;
                }
            }
            else
            {
                // Add new metered Dimension
                existingPlan.MeteredDimensions.Add(metered);
            }
        }
    }

    /// <summary>
    ///     Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
    ///     unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
            if (disposing)
                context.Dispose();

        disposed = true;
    }
}