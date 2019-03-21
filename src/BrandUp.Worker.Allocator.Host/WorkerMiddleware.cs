using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator.WebHost
{
    internal class WorkerMiddleware
    {
        private readonly RequestDelegate next;

        public WorkerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var requestPath = request.Path.Value.ToLower().Trim(new char[] { '/' });
            switch (requestPath)
            {
                case "task":
                    {
                        if (request.Method == "POST")
                        {
                            await ProcessPushTaskAsync(context);
                            return;
                        }
                        else
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                            return;
                        }
                    }
                case "executor/subscribe":
                    {
                        if (request.Method == "POST")
                        {
                            await ProcessSubscribeAsync(context);
                            return;
                        }
                        else
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                            return;
                        }
                    }
                case "executor/wait":
                    {
                        if (request.Method == "POST")
                        {
                            await ProcessWaitTasksAsync(context);
                            return;
                        }
                        else
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                            return;
                        }
                    }
                case "executor/success":
                    {
                        if (request.Method == "POST")
                        {
                            await ProcessSuccessTaskAsync(context);
                            return;
                        }
                        else
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                            return;
                        }
                    }
                case "executor/error":
                    {
                        if (request.Method == "POST")
                        {
                            await ProcessSuccessTaskAsync(context);
                            return;
                        }
                        else
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                            return;
                        }
                    }
            }

            await next(context);
        }

        private async Task ProcessPushTaskAsync(HttpContext context)
        {
            var taskAllocator = context.RequestServices.GetRequiredService<ITaskAllocator>();
            var contractSerializer = context.RequestServices.GetRequiredService<Remoting.IContractSerializer>();

            var requestData = contractSerializer.Deserialize<Models.PushTaskRequest>(context.Request.Body);
            if (requestData.TaskModel == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            var taskId = await taskAllocator.PushTaskAsync(requestData.TaskModel, context.RequestAborted);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.Headers.Add("Content-Type", new Microsoft.Extensions.Primitives.StringValues(contractSerializer.ContentType));
            contractSerializer.Serialize(context.Response.Body, new Models.PushTaskResponse { TaskId = taskId }, false);
        }
        private async Task ProcessSubscribeAsync(HttpContext context)
        {
            var taskAllocator = context.RequestServices.GetRequiredService<ITaskAllocator>();
            var contractSerializer = context.RequestServices.GetRequiredService<Remoting.IContractSerializer>();

            var requestData = contractSerializer.Deserialize<Models.SubscribeExecutorRequest>(context.Request.Body);
            if (requestData.TaskTypeNames == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            var executorId = await taskAllocator.SubscribeAsync(requestData.TaskTypeNames, context.RequestAborted);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.Headers.Add("Content-Type", new Microsoft.Extensions.Primitives.StringValues(contractSerializer.ContentType));
            contractSerializer.Serialize(context.Response.Body, new Models.SubscribeExecutorResponse { ExecutorId = executorId }, false);
        }
        private async Task ProcessWaitTasksAsync(HttpContext context)
        {
            var taskAllocator = context.RequestServices.GetRequiredService<ITaskAllocator>();
            var contractSerializer = context.RequestServices.GetRequiredService<Remoting.IContractSerializer>();

            var requestData = contractSerializer.Deserialize<Models.WaitTasksRequest>(context.Request.Body);
            var tasks = await taskAllocator.WaitTasksAsync(requestData.ExecutorId, context.RequestAborted);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.Headers.Add("Content-Type", new Microsoft.Extensions.Primitives.StringValues(contractSerializer.ContentType));
            contractSerializer.Serialize(context.Response.Body, new Models.WaitTasksResponse { Tasks = tasks.ToArray() }, false);
        }
        private async Task ProcessSuccessTaskAsync(HttpContext context)
        {
            var taskAllocator = context.RequestServices.GetRequiredService<ITaskAllocator>();
            var contractSerializer = context.RequestServices.GetRequiredService<Remoting.IContractSerializer>();

            var requestData = contractSerializer.Deserialize<Models.SuccessTaskRequest>(context.Request.Body);
            await taskAllocator.SuccessTaskAsync(requestData.ExecutorId, requestData.TaskId, requestData.ExecutingTime, context.RequestAborted);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }
        private async Task ProcessErrorTaskAsync(HttpContext context)
        {
            var taskAllocator = context.RequestServices.GetRequiredService<ITaskAllocator>();
            var contractSerializer = context.RequestServices.GetRequiredService<Remoting.IContractSerializer>();

            var requestData = contractSerializer.Deserialize<Models.ErrorTaskRequest>(context.Request.Body);
            await taskAllocator.ErrorTaskAsync(requestData.ExecutorId, requestData.TaskId, requestData.ExecutingTime, null, context.RequestAborted);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }
    }
}