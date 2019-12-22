﻿using GolemClientMockAPI.Entities;
using GolemClientMockAPI.Repository;
using GolemMarketApiMockup;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolemClientMockAPI.Processors.Operations
{
    public class SendAgreementResponseOperation : InMemoryMarketOperationBase
    {
        public IDictionary<string, BlockingCollection<AgreementResultEnum>> AgreementResultPipelines { get; set; }

        public SendAgreementResponseOperation(ISubscriptionRepository subscriptionRepo,
                                        IProposalRepository proposalRepo,
                                        IAgreementRepository agreementRepo,
                                        IDictionary<string, SubscriptionPipeline<DemandSubscription, MarketRequestorEvent>> requestorEventPipelines,
                                        IDictionary<string, string> demandSubscriptions,
                                        IDictionary<string, SubscriptionPipeline<OfferSubscription, MarketProviderEvent>> providerEventPipelines,
                                        IDictionary<string, string> offerSubscriptions,
                                        IDictionary<string, BlockingCollection<AgreementResultEnum>> agreementResultPipelines) 
            : base(subscriptionRepo, proposalRepo, agreementRepo, requestorEventPipelines, demandSubscriptions, providerEventPipelines, offerSubscriptions)
        {
            this.AgreementResultPipelines = agreementResultPipelines;
        }

        public Agreement Run(string agreementId, AgreementResultEnum response)
        {
            // 0. Validate the agreement
            var agreement = this.AgreementRepository.GetAgreement(agreementId);

            if (agreement != null)
            {

                switch(agreement.State)
                {
                    case AgreementState.Proposed:
                        // 1. Set agreement state according to Provider's decision and persist

                        this.AgreementRepository.UpdateAgreementState(agreementId, this.DecodeIntendedAgreementState(response));

                        // 2. Send the Agreement response to the Requestor

                        if (this.AgreementResultPipelines.ContainsKey(agreementId))
                        {
                            this.AgreementResultPipelines[agreementId].Add(response);
                        }
                        else
                        {
                            throw new Exception($"AgreementId {agreementId} not found in AgreementResultPipelines...");
                        }
                        break;
                    case AgreementState.Cancelled:   // Agreement has been cancelled in between
                        // 
                        break;
                    default:
                        throw new Exception($"Agreement {agreementId} must be in Proposed state, is in {agreement.State}!");
                }
                

                return agreement;
            }
            else
            {
                throw new Exception($"Agreement Id {agreementId} does not exist...");
            }
        }

        private AgreementState DecodeIntendedAgreementState(AgreementResultEnum response)
        {
            switch(response)
            {
                case AgreementResultEnum.Approved:
                    return AgreementState.Approved;
                case AgreementResultEnum.Rejected:
                    return AgreementState.Rejected;
                default:
                    throw new Exception($"Unknown repsonse: {response}");
            }
        }
    }
}
