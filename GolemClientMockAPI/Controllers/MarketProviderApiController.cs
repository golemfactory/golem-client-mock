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
using GolemClientMockAPI.Security;
using GolemClientMockAPI.Processors;
using GolemClientMockAPI.Repository;
using GolemClientMockAPI.Mappers;

namespace GolemMarketMockAPI.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    [GolemClientAuthorizationFilter(DefaultNodeId = "DummyProviderNodeId")]
    public class MarketProviderApiController : Controller
    {

        public IProviderMarketProcessor MarketProcessor { get; set; }
        public ISubscriptionRepository SubscriptionRepository { get; set; }
        public IProposalRepository ProposalRepository { get; set; }
        public IAgreementRepository AgreementRepository { get; set; }

        public MarketProviderEventMapper ProviderEventMapper { get; set; }
        public DemandMapper DemandMapper { get; set; }
        public OfferMapper OfferMapper { get; set; }

        public MarketProviderApiController(IProviderMarketProcessor marketProcessor,
            ISubscriptionRepository subscriptionRepository,
            IProposalRepository proposalRepository,
            IAgreementRepository agreementRepository,
            MarketProviderEventMapper providerEventMapper,
            OfferMapper offerMapper,
            DemandMapper demandMapper)
        {
            this.MarketProcessor = marketProcessor;
            this.SubscriptionRepository = subscriptionRepository;
            this.ProposalRepository = proposalRepository;
            this.AgreementRepository = agreementRepository;
            this.ProviderEventMapper = providerEventMapper;
            this.OfferMapper = offerMapper;
            this.DemandMapper = demandMapper;
        }


        /// <summary>
        /// 
        /// </summary>

        /// <param name="agreementId"></param>
        /// <response code="200">OK</response>
        [HttpPost]
        [Route("/market-api/v1/agreements/{agreementId}/approve")]
        [ValidateModelState]
        [SwaggerOperation("ApproveAgreement")]
        public virtual IActionResult ApproveAgreement([FromRoute][Required]string agreementId)
        { 
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            // locate the agreement
            var agreement = this.AgreementRepository.GetAgreement(agreementId);

            if (agreement == null)
            {
                return StatusCode(404); // Not Found
            }

            if (clientContext.NodeId != agreement.Offer.NodeId)
            {
                return StatusCode(401); // Unauthorized
            }

            var agreementEntity = this.MarketProcessor.ApproveAgreement(agreementId);

            if(agreementEntity.State == GolemClientMockAPI.Entities.AgreementState.Cancelled)
            {
                return StatusCode(410, "Cancelled");
            }

            return StatusCode(200, "OK");

        }

        /// <summary>
        /// 
        /// </summary>

        /// <param name="subscriptionId"></param>
        /// <param name="timeout"></param>
        /// <param name="maxEvents"></param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/market-api/v1/offers/{subscriptionId}/events")]
        [ValidateModelState]
        [SwaggerOperation("Collect")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<Proposal>), description: "OK")]
        public virtual async Task<IActionResult> Collect([FromRoute][Required]string subscriptionId, [FromQuery]float? timeout, [FromQuery]long? maxEvents)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            var subscription = this.SubscriptionRepository.GetOfferSubscription(subscriptionId);

            if (subscription == null)
            {
                return StatusCode(404); // Not Found
            }

            if (clientContext.NodeId != subscription.Offer.NodeId)
            {
                return StatusCode(401); // Unauthorized
            }

            var events = await this.MarketProcessor.CollectProviderEventsAsync(subscriptionId, timeout, (int?)maxEvents);

            var result = events.Select(proposal => this.ProviderEventMapper.Map(proposal))
                                   .ToList();

            // Return the collected requestor events (including offer proposals)
            return StatusCode(200, result);
        }

        /// <summary>
        /// Creates agreement proposal
        /// </summary>

        /// <param name="subscriptionId"></param>
        /// <param name="proposalId"></param>
        /// <param name="proposal"></param>
        /// <response code="200">OK</response>
        [HttpPost]
        [Route("/market-api/v1/offers/{subscriptionId}/proposals/{proposalId}/offer")]
        [ValidateModelState]
        [SwaggerOperation("CreateProposal")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "OK")]
        public virtual IActionResult CreateProposal([FromRoute][Required]string subscriptionId, [FromRoute][Required]string proposalId, [FromBody]Proposal proposal)
        { 
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(string));

            string exampleJson = null;
            exampleJson = "\"\"";
            
            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<string>(exampleJson)
            : default(string);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Fetches agreement proposal from proposal id.
        /// </summary>
        
        /// <param name="subscriptionId"></param>
        /// <param name="proposalId"></param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("/market-api/v1/offers/{subscriptionId}/proposals/{proposalId}")]
        [ValidateModelState]
        [SwaggerOperation("GetProposal")]
        [SwaggerResponse(statusCode: 200, type: typeof(AgreementProposal), description: "OK")]
        public virtual IActionResult GetProposal([FromRoute][Required]string subscriptionId, [FromRoute][Required]string proposalId)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            var subscription = this.SubscriptionRepository.GetOfferSubscription(subscriptionId);

            if (subscription == null)
            {
                return StatusCode(404); // Not Found
            }

            if (clientContext.NodeId != subscription.Offer.NodeId)
            {
                return StatusCode(401); // Unauthorized
            }

            var demandProposal = this.ProposalRepository.GetDemandProposals(subscriptionId).Where(prop => prop.Id == proposalId).FirstOrDefault();

            if (demandProposal == null)
            {
                return StatusCode(404); // Not Found
            }

            var offerProposal = (demandProposal.OfferId == null) ?
                                    new GolemClientMockAPI.Entities.OfferProposal() { Id = subscriptionId, Offer = subscription.Offer } :
                                    this.ProposalRepository.GetOfferProposal(demandProposal.OfferId);

            var result = new AgreementProposal()
            {
                Id = proposalId,
                Offer = this.DemandMapper.MapEntityToProposal(demandProposal),
                Demand = this.OfferMapper.MapEntityToProposal(offerProposal)
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
        [Route("/market-api/v1/offers/{subscriptionId}/propertyQuery/{queryId}")]
        [ValidateModelState]
        [SwaggerOperation("QueryResponse")]
        public virtual IActionResult QueryResponse([FromRoute][Required]string subscriptionId, [FromRoute][Required]string queryId, [FromBody]PropertyQueryResponse propertyValues)
        { 
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200);


            throw new NotImplementedException();
        }

        /// <summary>
        /// Rejects agreement.
        /// </summary>
        
        /// <param name="agreementId"></param>
        /// <response code="204">Agreement rejected</response>
        [HttpPost]
        [Route("/market-api/v1/agreements/{agreementId}/reject")]
        [ValidateModelState]
        [SwaggerOperation("RejectAgreement")]
        public virtual IActionResult RejectAgreement([FromRoute][Required]string agreementId)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            // locate the agreement
            var agreement = this.AgreementRepository.GetAgreement(agreementId);

            if (agreement == null)
            {
                return StatusCode(404); // Not Found
            }

            if (clientContext.NodeId != agreement.Offer.NodeId)
            {
                return StatusCode(401); // Unauthorized
            }

            this.MarketProcessor.RejectAgreement(agreementId);

            return StatusCode(204);
        }

        /// <summary>
        /// Rejects offer
        /// </summary>
        
        /// <param name="subscriptionId"></param>
        /// <param name="proposalId"></param>
        /// <response code="204">OK</response>
        [HttpDelete]
        [Route("/market-api/v1/offers/{subscriptionId}/proposals/{proposalId}")]
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
        
        /// <param name="body">Offer description</param>
        /// <response code="201">OK</response>
        /// <response code="400">Bad offer desciption</response>
        [HttpPost]
        [Route("/market-api/v1/offers")]
        [ValidateModelState]
        [SwaggerOperation("Subscribe")]
        [SwaggerResponse(statusCode: 201, type: typeof(string), description: "OK")]
        public virtual IActionResult Subscribe([FromBody]Offer body)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            var offerEntity = this.OfferMapper.MapToEntity(body);

            offerEntity.NodeId = clientContext.NodeId;

            var subscription = this.MarketProcessor.SubscribeOffer(offerEntity);

            // return created Subscription Id
            // return this.Content(subscription.Id);
            return StatusCode(201, subscription.Id);
        }

        /// <summary>
        /// 
        /// </summary>

        /// <param name="subscriptionId"></param>
        /// <response code="204">Delete</response>
        /// <response code="404">Subscription not found</response>
        [HttpDelete]
        [Route("/market-api/v1/offers/{subscriptionId}")]
        [ValidateModelState]
        [SwaggerOperation("Unsubscribe")]
        public virtual IActionResult Unsubscribe([FromRoute][Required]string subscriptionId)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            var subscription = this.SubscriptionRepository.GetOfferSubscription(subscriptionId);

            if (subscription == null)
            {
                return StatusCode(404); // Not Found
            }

            if (clientContext.NodeId != subscription.Offer.NodeId)
            {
                return StatusCode(401); // Unauthorized
            }

            this.MarketProcessor.UnsubscribeOffer(subscriptionId);

            return StatusCode(204);
        }
    }
}
