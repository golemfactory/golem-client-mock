﻿/*
 * Golem Market API
 *
 *  ## Yagna Market The Yagna Market is a core component of the Yagna Network, which enables computational Offers and Demands circulation. The Market is open for all entities willing to buy computations (Demands) or monetize computational resources (Offers). ## Yagna Market API The Yagna Market API is the entry to the Yagna Market through which Requestors and Providers can publish their Demands and Offers respectively, find matching counterparty, conduct negotiations and make an agreement.  This version of Market API conforms with capability level 1 of the <a href=\"https://docs.google.com/document/d/1Zny_vfgWV-hcsKS7P-Kdr3Fb0dwfl-6T_cYKVQ9mkNg\"> Market API specification</a>.  Market API contains two roles: Requestors and Providers which are symmetrical most of the time (excluding agreement phase). 
 *
 * OpenAPI spec version: 1.4.0
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
    //[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
    public class MarketCommonApiController : Controller
    {
        public IRequestorMarketProcessor MarketProcessor { get; set; }
        public ISubscriptionRepository SubscriptionRepository { get; set; }
        public IProposalRepository ProposalRepository { get; set; }
        public IAgreementRepository AgreementRepository { get; set; }

        public MarketRequestorEventMapper RequestorEventMapper { get; set; }
        public DemandMapper DemandMapper { get; set; }
        public OfferMapper OfferMapper { get; set; }
        public AgreementMapper AgreementMapper { get; set; }

        public MarketCommonApiController(IRequestorMarketProcessor marketProcessor,
            ISubscriptionRepository subscriptionRepository,
            IProposalRepository proposalRepository,
            IAgreementRepository agreementRepository,
            MarketRequestorEventMapper requestorEventMapper,
            DemandMapper demandMapper,
            OfferMapper offerMapper,
            AgreementMapper agreementMapper)
        {
            this.MarketProcessor = marketProcessor;
            this.SubscriptionRepository = subscriptionRepository;
            this.ProposalRepository = proposalRepository;
            this.AgreementRepository = agreementRepository;
            this.RequestorEventMapper = requestorEventMapper;
            this.DemandMapper = demandMapper;
            this.OfferMapper = offerMapper;
            this.AgreementMapper = agreementMapper;
        }


        /// <summary>
        /// Fetches agreement with given agreement id.
        /// </summary>
        /// <param name="agreementId"></param>
        /// <response code="200">Agreement.</response>
        /// <response code="401">Authorization information is missing or invalid.</response>
        /// <response code="404">The specified resource was not found.</response>
        /// <response code="0">Unexpected error.</response>
        [HttpGet]
        [Route("/market-api/v1/agreements/{agreementId}")]
        [ValidateModelState]
        [SwaggerOperation("GetAgreement")]
        [SwaggerResponse(statusCode: 200, type: typeof(Agreement), description: "Agreement.")]
        [SwaggerResponse(statusCode: 401, type: typeof(ErrorMessage), description: "Authorization information is missing or invalid.")]
        [SwaggerResponse(statusCode: 404, type: typeof(ErrorMessage), description: "The specified resource was not found.")]
        [SwaggerResponse(statusCode: 0, type: typeof(ErrorMessage), description: "Unexpected error.")]
        public virtual IActionResult GetAgreement([FromRoute][Required]string agreementId)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            // locate the agreement
            var agreementEntity = this.AgreementRepository.GetAgreement(agreementId);

            if (agreementEntity == null)
            {
                return StatusCode(404); // Not Found
            }

            /*
            if (
                (clientContext.NodeId != agreementEntity.DemandProposal.Demand.NodeId) 
                && 
                (clientContext.NodeId != agreementEntity.OfferProposal.Offer.NodeId)
            )
            {
                Console.WriteLine($"whould have returned 401 Unauthorized because of context {clientContext.NodeId} != {agreementEntity.DemandProposal.Demand.NodeId} agreement");
                return StatusCode(401); // Unauthorized
            }
            */

            var agreement = AgreementMapper.MapEntityToAgreement(agreementEntity);

            return StatusCode(200, agreement);
        }


        /// <summary>
        /// Terminates approved Agreement.
        /// </summary>
        /// <param name="agreementId"></param>
        /// <response code="204">Agreement terminated.</response>
        /// <response code="401">Authorization information is missing or invalid.</response>
        /// <response code="404">The specified resource was not found.</response>
        /// <response code="409">Agreement not in Approved state.</response>
        /// <response code="410">Agreement cancelled by the Requstor.</response>
        /// <response code="0">Unexpected error.</response>
        [HttpPost]
        [Route("/market-api/v1/agreements/{agreementId}/terminate")]
        [ValidateModelState]
        [SwaggerOperation("TerminateAgreement")]
        [SwaggerResponse(statusCode: 401, type: typeof(ErrorMessage), description: "Authorization information is missing or invalid.")]
        [SwaggerResponse(statusCode: 404, type: typeof(ErrorMessage), description: "The specified resource was not found.")]
        [SwaggerResponse(statusCode: 0, type: typeof(ErrorMessage), description: "Unexpected error.")]
        public virtual IActionResult TerminateAgreement([FromRoute][Required]string agreementId)
        { 
            //TODO: Uncomment the next line to return response 204 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(204);

            //TODO: Uncomment the next line to return response 401 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(401, default(ErrorMessage));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(ErrorMessage));

            //TODO: Uncomment the next line to return response 409 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(409);

            //TODO: Uncomment the next line to return response 410 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(410);

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(ErrorMessage));

            throw new NotImplementedException();
        }

    }
}
