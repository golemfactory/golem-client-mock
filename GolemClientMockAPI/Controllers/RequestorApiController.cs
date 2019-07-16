/*
 * Golem Market API
 *
 * Market API
 *
 * OpenAPI spec version: 1.0.0
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Swashbuckle.AspNetCore.SwaggerGen;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using GolemClientMockAPI.Attributes;
using GolemMarketMockAPI.MarketAPI.Models;
using Swashbuckle.AspNetCore.Annotations;
using GolemClientMockAPI.Repository;
using GolemClientMockAPI.Processors;
using GolemClientMockAPI.Mappers;
using GolemClientMockAPI.Security;

namespace GolemMarketMockAPI.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [GolemClientAuthorizationFilter(DefaultNodeId = "DummyRequestorNodeId")]
    public class RequestorApiController : Controller
    {
        public IRequestorMarketProcessor MarketProcessor { get; set; }
        public ISubscriptionRepository SubscriptionRepository { get; set; }
        public IProposalRepository ProposalRepository { get; set; }
        public IAgreementRepository AgreementRepository { get; set; }

        public RequestorEventMapper RequestorEventMapper { get; set; }
        public DemandMapper DemandMapper { get; set; }
        public OfferMapper OfferMapper { get; set; }

        public RequestorApiController(IRequestorMarketProcessor marketProcessor,
            ISubscriptionRepository subscriptionRepository,
            IProposalRepository proposalRepository,
            IAgreementRepository agreementRepository,
            RequestorEventMapper requestorEventMapper,
            DemandMapper demandMapper,
            OfferMapper offerMapper)
        {
            this.MarketProcessor = marketProcessor;
            this.SubscriptionRepository = subscriptionRepository;
            this.ProposalRepository = proposalRepository;
            this.AgreementRepository = agreementRepository;
            this.RequestorEventMapper = requestorEventMapper;
            this.DemandMapper = demandMapper;
            this.OfferMapper = offerMapper;
        }

        /// <summary>
        /// Cancels agreement.
        /// </summary>

        /// <param name="agreementId"></param>
        /// <response code="204">Agreement canceled</response>
        [HttpDelete]
        [Route("/market-api/v1/agreements/{agreementId}")]
        [ValidateModelState]
        [SwaggerOperation("CancelAgreement")]
        public virtual IActionResult CancelAgreement([FromRoute][Required]string agreementId)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            // locate the agreement
            var agreement = this.AgreementRepository.GetAgreement(agreementId);

            if (agreement == null)
            {
                return StatusCode(404); // Not Found
            }

            if (clientContext.NodeId != agreement.Demand.NodeId)
            {
                return StatusCode(401); // Unauthorized
            }

            this.MarketProcessor.CancelAgreement(agreementId);

            return StatusCode(204);
        }

        /// <summary>
        /// 
        /// </summary>
        
        /// <param name="subscriptionId"></param>
        /// <param name="timeout"></param>
        /// <param name="maxEvents"></param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/market-api/v1/demands/{subscriptionId}/events")]
        [ValidateModelState]
        [SwaggerOperation("Collect")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<RequestorEvent>), description: "OK")]
        public virtual async Task<IActionResult> Collect([FromRoute][Required]string subscriptionId, [FromQuery]float? timeout, [FromQuery]long? maxEvents)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            var subscription = this.SubscriptionRepository.GetDemandSubscription(subscriptionId);

            if(subscription == null)
            {
                return StatusCode(404); // Not Found
            }

            if(clientContext.NodeId != subscription.Demand.NodeId)
            {
                return StatusCode(401); // Unauthorized
            }
            
            var events = await this.MarketProcessor.CollectRequestorEventsAsync(subscriptionId, timeout, (int?)maxEvents);

            var result = events.Select(proposal => this.RequestorEventMapper.Map(proposal))
                               .ToList();

            // Return the collected requestor events (including offer proposals)
            return StatusCode(200, result);
        }

        /// <summary>
        /// approves
        /// </summary>
        
        /// <param name="agreementId"></param>
        /// <response code="200">OK</response>
        [HttpPost]
        [Route("/market-api/v1/agreements/{agreementId}/confirm")]
        [ValidateModelState]
        [SwaggerOperation("ConfirmAgreement")]
        public virtual IActionResult ConfirmAgreement([FromRoute][Required]string agreementId)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            // locate the agreement
            var agreement = this.AgreementRepository.GetAgreement(agreementId);

            if (agreement == null)
            {
                return StatusCode(404); // Not Found
            }

            if (clientContext.NodeId != agreement.Demand.NodeId)
            {
                return StatusCode(401); // Unauthorized
            }

            this.MarketProcessor.SendConfirmAgreement(agreementId);

            return StatusCode(200);
        }

        /// <summary>
        /// Creates new agreement from proposal
        /// </summary>
        
        /// <param name="agreement"></param>
        /// <response code="201">Created</response>
        [HttpPost]
        [Route("/market-api/v1/agreements")]
        [ValidateModelState]
        [SwaggerOperation("CreateAgreement")]
        public virtual IActionResult CreateAgreement([FromBody]Agreement agreement)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            // locate the offerProposalId
            var offerProposal = this.ProposalRepository.GetOfferProposal(agreement.ProposalId);

            var receivingSubscription = this.SubscriptionRepository.GetDemandSubscription(offerProposal.ReceivingSubscriptionId);

            if (offerProposal == null)
            {
                return StatusCode(404); // Not Found
            }

            if (clientContext.NodeId != receivingSubscription.Demand.NodeId)
            {
                return StatusCode(401); // Unauthorized
            }

            var resultAgreement = this.MarketProcessor.CreateAgreement(agreement.ProposalId);

            return StatusCode(201);

        }

        /// <summary>
        /// Creates agreement proposal
        /// </summary>
        
        /// <param name="subscriptionId"></param>
        /// <param name="proposalId"></param>
        /// <param name="demandProposal"></param>
        /// <response code="200">OK</response>
        [HttpPost]
        [Route("/market-api/v1/demands/{subscriptionId}/proposals/{proposalId}/demand")]
        [ValidateModelState]
        [SwaggerOperation("CreateProposal")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "OK")]
        public virtual IActionResult CreateProposal([FromRoute][Required]string subscriptionId, [FromRoute][Required]string proposalId, [FromBody]Proposal demandProposal)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            var subscription = this.SubscriptionRepository.GetDemandSubscription(subscriptionId);

            if (subscription == null)
            {
                return StatusCode(404); // Not Found
            }

            if (clientContext.NodeId != subscription.Demand.NodeId)
            {
                return StatusCode(401); // Unauthorized
            }

            var demandEntity = new GolemClientMockAPI.Entities.Demand()
                {
                    NodeId = clientContext.NodeId,
                    Constraints = demandProposal.Constraints,
                    Properties = demandProposal.Properties as Dictionary<string, string>
                };

            try
            {
                var demandProposalEntity = this.MarketProcessor.CreateDemandProposal(subscriptionId, proposalId, demandEntity);

                return new ObjectResult(demandProposalEntity.Id);
            }
            catch (Exception exc)
            {
                return StatusCode(404); // Not Found
            }
        }

        /// <summary>
        /// Fetches agreement proposal from proposal id.
        /// </summary>
        
        /// <param name="subscriptionId"></param>
        /// <param name="proposalId"></param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/market-api/v1/demands/{subscriptionId}/proposals/{proposalId}")]
        [ValidateModelState]
        [SwaggerOperation("GetProposal")]
        [SwaggerResponse(statusCode: 200, type: typeof(AgreementProposal), description: "OK")]
        public virtual IActionResult GetProposal([FromRoute][Required]string subscriptionId, [FromRoute][Required]string proposalId)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            var subscription = this.SubscriptionRepository.GetDemandSubscription(subscriptionId);

            if (subscription == null)
            {
                return StatusCode(404); // Not Found
            }

            if (clientContext.NodeId != subscription.Demand.NodeId)
            {
                return StatusCode(401); // Unauthorized
            }

            var offerProposal = this.ProposalRepository.GetOfferProposals(subscriptionId).Where(prop => prop.Id == proposalId).FirstOrDefault();

            if(offerProposal == null)
            {
                return StatusCode(404); // Not Found
            }

            var demandProposal = (offerProposal.DemandId == null) ? 
                                    new GolemClientMockAPI.Entities.DemandProposal() { Demand = subscription.Demand }  : 
                                    this.ProposalRepository.GetDemandProposal(offerProposal.DemandId);

            var result = new AgreementProposal()
            {
                Id = proposalId,
                Offer = this.OfferMapper.MapEntityToProposal(offerProposal),
                Demand = this.DemandMapper.MapEntityToProposal(demandProposal)
            };
            
            return StatusCode(200, result);
        }

        /// <summary>
        /// 
        /// </summary>
        
        /// <param name="subscriptionId"></param>
        /// <param name="queryId"></param>
        /// <param name="propertyValues"></param>
        /// <response code="200">OK</response>
        [HttpPost]
        [Route("/market-api/v1/demands/{subscriptionId}/propertyQuery/{queryId}")]
        [ValidateModelState]
        [SwaggerOperation("QueryResponse")]
        public virtual IActionResult QueryResponse([FromRoute][Required]string subscriptionId, [FromRoute][Required]string queryId, [FromBody]PropertyQueryResponse propertyValues)
        { 
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200);


            throw new NotImplementedException();
        }

        /// <summary>
        /// Rejects offer
        /// </summary>
        
        /// <param name="subscriptionId"></param>
        /// <param name="proposalId"></param>
        /// <response code="204">OK</response>
        [HttpDelete]
        [Route("/market-api/v1/demands/{subscriptionId}/proposals/{proposalId}")]
        [ValidateModelState]
        [SwaggerOperation("RejectProposal")]
        public virtual IActionResult RejectProposal([FromRoute][Required]string subscriptionId, [FromRoute][Required]string proposalId)
        { 
            //TODO: Uncomment the next line to return response 204 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(204);


            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        
        /// <param name="body">Demand description</param>
        /// <response code="201">Offer published</response>
        /// <response code="400">Bad offer desciption</response>
        [HttpPost]
        [Route("/market-api/v1/demands")]
        [ValidateModelState]
        [SwaggerOperation("Subscribe")]
        [SwaggerResponse(statusCode: 201, type: typeof(string), description: "Offer published")]
        public virtual IActionResult Subscribe([FromBody]Demand body)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            var demandEntity = this.DemandMapper.MapToEntity(body);

            demandEntity.NodeId = clientContext.NodeId;

            var subscription = this.MarketProcessor.SubscribeDemand(demandEntity);
            
            // return created Subscription Id
            return this.Content(subscription.Id);
        }

        /// <summary>
        /// 
        /// </summary>
        
        /// <param name="subscriptionId"></param>
        /// <response code="204">Delete</response>
        /// <response code="404">Subscription not found</response>
        [HttpDelete]
        [Route("/market-api/v1/demands/{subscriptionId}")]
        [ValidateModelState]
        [SwaggerOperation("Unsubscribe")]
        public virtual IActionResult Unsubscribe([FromRoute][Required]string subscriptionId)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            var subscription = this.SubscriptionRepository.GetDemandSubscription(subscriptionId);

            if (subscription == null)
            {
                return StatusCode(404); // Not Found
            }

            if (clientContext.NodeId != subscription.Demand.NodeId)
            {
                return StatusCode(401); // Unauthorized
            }

            this.MarketProcessor.UnsubscribeDemand(subscriptionId);

            return StatusCode(204);
        }

        /// <summary>
        /// 
        /// </summary>
        
        /// <param name="agreementId"></param>
        /// <response code="200">OK</response>
        [HttpPost]
        [Route("/market-api/v1/agreements/{agreementId}/wait")]
        [ValidateModelState]
        [SwaggerOperation("WaitForApproval")]
        public async virtual Task<IActionResult> WaitForApproval([FromRoute][Required]string agreementId)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            // locate the agreement
            var agreement = this.AgreementRepository.GetAgreement(agreementId);

            if (agreement == null)
            {
                return StatusCode(404); // Not Found
            }

            if (clientContext.NodeId != agreement.Demand.NodeId)
            {
                return StatusCode(401); // Unauthorized
            }


            var result = await this.MarketProcessor.WaitConfirmAgreementResponseAsync(agreementId, 10000);

            switch (result)
            {
                case GolemClientMockAPI.Entities.AgreementResultEnum.Approved:
                    return StatusCode(200);
                case GolemClientMockAPI.Entities.AgreementResultEnum.Rejected:
                    return StatusCode(406); // Not Acceptable = Rejected
                case GolemClientMockAPI.Entities.AgreementResultEnum.Timeout:
                default:
                    return StatusCode(408); // Timeout
            }
            
        }
    }
}
