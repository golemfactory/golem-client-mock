/*
 * Golem Activity API
 *
 * No description provided (generated by Swagger Codegen https://github.com/swagger-api/swagger-codegen)
 *
 * OpenAPI spec version: v1
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using GolemClientMockAPI.ActivityAPI.Models;
using GolemClientMockAPI.Attributes;
using GolemClientMockAPI.Processors;
using GolemClientMockAPI.Repository;
using GolemClientMockAPI.Security;
using GolemClientMockAPI.Mappers;
using System.Threading.Tasks;

namespace GolemMarketMockAPI.Controllers
{ 
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [GolemClientAuthorizationFilter(DefaultNodeId = "DummyRequestorNodeId")]
    public class ActivityControlApiController : ControllerBase
    {
        public IRequestorActivityProcessor ActivityProcessor { get; set; }
        public IAgreementRepository AgreementRepository { get; set; }
        public IActivityRepository ActivityRepository { get; set; }
        public ExeScriptMapper ExeScriptMapper { get; set; }

        public ActivityControlApiController(IRequestorActivityProcessor activityProcessor,
            IAgreementRepository agreementRepository,
            IActivityRepository activityRepository,
            ExeScriptMapper exeScriptMapper)
        {
            this.ActivityProcessor = activityProcessor;
            this.AgreementRepository = agreementRepository;
            this.ActivityRepository = activityRepository;
            this.ExeScriptMapper = exeScriptMapper;
        }


        /// <summary>
        /// Creates new Activity based on given Agreement.
        /// </summary>

        /// <param name="agreementId"></param>
        /// <response code="201">Success</response>
        /// <response code="400">Bad Request</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Server Error</response>
        [HttpPost]
        [Route("/activity-api/v1/activity")]
        [ValidateModelState]
        [SwaggerOperation("CreateActivity")]
        [SwaggerResponse(statusCode: 201, type: typeof(string), description: "Success")]
        [SwaggerResponse(statusCode: 400, type: typeof(CreateActivityError), description: "Bad Request")]
        [SwaggerResponse(statusCode: 403, type: typeof(GolemClientMockAPI.ActivityAPI.Models.ProblemDetails), description: "Forbidden")]
        [SwaggerResponse(statusCode: 500, type: typeof(CreateActivityError), description: "Server Error")]
        public virtual IActionResult CreateActivity([FromBody]string agreementId)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            try
            {
                var agreement = this.AgreementRepository.GetAgreement(agreementId);

                if (agreement == null)
                {
                    return this.StatusCode(404); // Agreement not found
                }

                if (agreement.DemandProposal.Demand.NodeId != clientContext.NodeId)
                {
                    return this.StatusCode(403); // Not entitled to acto on the agreement
                }

                var activity = this.ActivityProcessor.CreateActivity(agreementId);

                var response = this.Content(activity.Id);
                response.StatusCode = 201;

                return response;
            }
            catch(Exception exc)
            {
                return this.StatusCode(500, new CreateActivityError() { Message = exc.Message });
            }

        }

        /// <summary>
        /// Destroys given Activity.
        /// </summary>
        
        /// <param name="activityId"></param>
        /// <response code="200">Success</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Server Error</response>
        [HttpDelete]
        [Route("/activity-api/v1/activity/{activityId}")]
        [ValidateModelState]
        [SwaggerOperation("DestroyActivity")]
        [SwaggerResponse(statusCode: 403, type: typeof(GolemClientMockAPI.ActivityAPI.Models.ProblemDetails), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(GolemClientMockAPI.ActivityAPI.Models.ProblemDetails), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(DestroyActivityError), description: "Server Error")]
        public virtual IActionResult DestroyActivity([FromRoute][Required]string activityId)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            try
            {
                var activity = this.ActivityRepository.GetActivity(activityId);

                if (activity == null)
                {
                    return this.StatusCode(404); // Agreement not found
                }

                if (activity.RequestorNodeId != clientContext.NodeId)
                {
                    return this.StatusCode(403); // Not entitled to act on the activity
                }

                this.ActivityProcessor.DestroyActivity(activityId);

                return this.Ok();
            }
            catch (Exception exc)
            {
                return this.StatusCode(500, new DestroyActivityError() { Message = exc.Message });
            }
        }

        /// <summary>
        /// Executes an ExeScript batch within a given Activity.
        /// </summary>

        /// <param name="activityId"></param>
        /// <param name="script"></param>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Server Error</response>
        [HttpPost]
        [Route("/activity-api/v1/activity/{activityId}/exec")]
        [ValidateModelState]
        [SwaggerOperation("Exec")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "Success")]
        [SwaggerResponse(statusCode: 400, type: typeof(ExecError), description: "Bad Request")]
        [SwaggerResponse(statusCode: 403, type: typeof(GolemClientMockAPI.ActivityAPI.Models.ProblemDetails), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(GolemClientMockAPI.ActivityAPI.Models.ProblemDetails), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(ExecError), description: "Server Error")]
        public virtual IActionResult Exec([FromRoute][Required]string activityId, [FromBody]ExeScriptRequest script)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            try
            {
                var activity = this.ActivityRepository.GetActivity(activityId);

                if (activity == null)
                {
                    return this.StatusCode(404); // Agreement not found
                }

                if (activity.RequestorNodeId != clientContext.NodeId)
                {
                    return this.StatusCode(403); // Not entitled to act on the activity
                }

                // TODO validate the incoming exe script batch??? Return HTTP 400 and script error info?

                var exeScriptEntity = this.ExeScriptMapper.MapToEntity(script);

                var batchId = this.ActivityProcessor.ExecAsync(activityId, exeScriptEntity);

                return this.Content(batchId);
            }
            catch (Exception exc)
            {
                return this.StatusCode(500, new DestroyActivityError() { Message = exc.Message });
            }
        }

        /// <summary>
        /// Queries for ExeScript batch results.
        /// </summary>

        /// <param name="activityId"></param>
        /// <param name="batchId"></param>
        /// <param name="timeout"></param>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Server Error</response>
        [HttpGet]
        [Route("/activity-api/v1/activity/{activityId}/exec/{batchId}")]
        [ValidateModelState]
        [SwaggerOperation("GetExecBatchResults")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<ExeScriptCommandResult>), description: "Success")]
        [SwaggerResponse(statusCode: 400, type: typeof(ExecError), description: "Bad Request")]
        [SwaggerResponse(statusCode: 403, type: typeof(GolemClientMockAPI.ActivityAPI.Models.ProblemDetails), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(GolemClientMockAPI.ActivityAPI.Models.ProblemDetails), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(ErrorBase), description: "Server Error")]
        public virtual async Task<IActionResult> GetExecBatchResults([FromRoute][Required]string activityId, [FromRoute][Required]string batchId, [FromQuery]int? timeout)
        {
            var clientContext = this.HttpContext.Items["ClientContext"] as GolemClientMockAPI.Entities.ClientContext;

            try
            {
                var activity = this.ActivityRepository.GetActivity(activityId);

                if (activity == null)
                {
                    return this.StatusCode(404); // Agreement not found
                }

                if (activity.RequestorNodeId != clientContext.NodeId)
                {
                    return this.StatusCode(403); // Not entitled to act on the activity
                }

                // TODO check batch exists? validate the rights to batch??? 

                var resultsEntity = await this.ActivityProcessor.GetExecBatchResultsAsync(batchId, timeout ?? 30000);

                var results = this.ExeScriptMapper.MapResultsFromEntity(resultsEntity);

                return this.Ok(results);
            }
            catch (Exception exc)
            {
                return this.StatusCode(500, new DestroyActivityError() { Message = exc.Message });
            }

        }
    }
}
