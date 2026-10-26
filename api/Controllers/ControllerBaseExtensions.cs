using Microsoft.AspNetCore.Mvc;

namespace StargateAPI.Controllers
{
    public static class ControllerBaseExtensions
    {

        public static IActionResult GetResponse(this ControllerBase controllerBase, BaseResponse response)
        {
            var httpResponse = new ObjectResult(response);
            httpResponse.StatusCode = response.ResponseCode;
            return httpResponse;
        }

        public static IActionResult GetErrorResponse(this ControllerBase controllerBase, Exception ex)
        {
            // pre-processor errors are fine to show, anything else just gets a generic message (details are in ProcessLog)
            var response = ex is BadHttpRequestException badRequest
                ? new BaseResponse { Message = badRequest.Message, ResponseCode = badRequest.StatusCode }
                : new BaseResponse { Message = "Something went wrong, please try again.", ResponseCode = StatusCodes.Status500InternalServerError };

            response.Success = false;
            return controllerBase.GetResponse(response);
        }
    }
}