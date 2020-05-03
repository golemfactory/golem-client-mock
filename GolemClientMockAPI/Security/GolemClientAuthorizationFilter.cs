using GolemClientMockAPI.Entities;
using GolemClientMockAPI.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GolemClientMockAPI.Security
{
    public class GolemClientAuthorizationFilter : Attribute, IActionFilter
    {
        public GolemClientAuthorizationFilter()
        {

        }

        public string DefaultNodeId { get; set; }

        public void OnActionExecuted(ActionExecutedContext context)
        {

        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // resolve the Bearer header here
            // and inject default if no header

            var config = context.HttpContext.RequestServices.GetService(typeof(IConfigProvider)) as IConfigProvider;
            var keyRepo = context.HttpContext.RequestServices.GetService(typeof(IAppKeyRepository)) as IAppKeyRepository;

            if (context.HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var clientContext = new ClientContext();

                var token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                token = token.Replace("Bearer ", "");
                var nodeId = keyRepo.GetNodeForKey(token);
                if (nodeId != null) {
                    clientContext.NodeId = nodeId;
                    context.HttpContext.Items["ClientContext"] = clientContext;
                    return;
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                try
                {
                    var jwt = tokenHandler.ReadJwtToken(token);

                    // Validate token

                    var jwtBuilder = context.HttpContext.RequestServices.GetService(typeof(IJwtBuilder)) as IJwtBuilder;

                    var isValid = true; /* jwtBuilder.ValidateToken(token, new Dictionary<string, string>()
                        {
                            { "aud", "GolemNetHub" },
                            { "sub", jwt.Subject }
                        }, config.PublicKey);
                    */

                    if (isValid)
                    {
                        clientContext.NodeId = jwt.Subject;

                        if (clientContext.NodeId != null)
                        {
                            context.HttpContext.Items["ClientContext"] = clientContext;
                            return;
                        }
                    }
                }
                catch (Exception exc)
                {
                    // TODO Log the invalid token exception
                }

            }

            // if we are here - there was no proper authorization token in request
            if(this.DefaultNodeId != null)
            {
                // context.Result = new StatusCodeResult(401); // short circuit to return status 401

                var clientContext = new ClientContext()
                {
                    NodeId = DefaultNodeId
                };

                context.HttpContext.Items["ClientContext"] = clientContext;
            }
            else
            {
                context.Result = new StatusCodeResult(401);
            }

        }
    }
}
