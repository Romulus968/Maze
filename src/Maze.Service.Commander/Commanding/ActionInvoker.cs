using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Maze.Modules.Api;
using Maze.Modules.Api.ModelBinding;
using Maze.Modules.Api.Parameters;
using Maze.Service.Commander.Commanding.ModelBinding;
using IValueProvider = Maze.Service.Commander.Commanding.ModelBinding.IValueProvider;

namespace Maze.Service.Commander.Commanding
{
    /// <summary>
    ///     Cached delegate for the controller method
    /// </summary>
    public class ActionInvoker
    {
        private readonly ObjectMethodExecutor _objectMethodExecutor;
        private readonly ActionMethodExecutor _executor;

        /// <summary>
        /// Initialize a new instance of <see cref="ActionInvoker"/>
        /// </summary>
        /// <param name="controllerType">The controller type</param>
        /// <param name="routeMethod">The action method that should be executed</param>
        public ActionInvoker(Type controllerType, MethodInfo routeMethod)
        {
            Metadata = new ActionMethodMetadata(controllerType, routeMethod);

            ObjectFactory = ActivatorUtilities.CreateFactory(controllerType, new Type[0]);
            _objectMethodExecutor = new ObjectMethodExecutor(routeMethod);
            _executor = ActionMethodExecutor.GetExecutor(Metadata);

            var parameters = new List<ParameterDescriptor>();
            foreach (var parameter in routeMethod.GetParameters())
            {
                var methodParam = new ParameterDescriptor
                {
                    Name = parameter.Name,
                    ParameterType = parameter.ParameterType,
                    BindingInfo = GetBindingInfo(parameter)
                };
                parameters.Add(methodParam);
            }

            Parameters = parameters.ToImmutableList();
        }

        public ActionMethodMetadata Metadata { get; }
        public ObjectFactory ObjectFactory { get; }
        public IImmutableList<ParameterDescriptor> Parameters { get; }

        /// <summary>
        /// Invoke the method with the given <see cref="ActionContext"/>
        /// </summary>
        /// <param name="actionContext">The action context that provides the information used to execute the method</param>
        /// <returns>Return the action result of the method</returns>
        public async Task<IActionResult> Invoke(ActionContext actionContext)
        {
            var controller =
                (MazeController) ObjectFactory.Invoke(actionContext.Context.RequestServices, new object[0]);

            using (controller)
            {
                return await InvokeMethod(actionContext, controller);
            }
        }

        private async Task<IActionResult> InvokeMethod(ActionContext actionContext, MazeController controller)
        {
            controller.MazeContext = actionContext.Context;

            var parameterBindingInfo =
                GetParameterBindingInfo(actionContext.Context.RequestServices
                    .GetRequiredService<IModelBinderFactory>());

            var arguments = new object[Parameters.Count];
            var parameterBinding = new ParameterBinder();
            var valueProvider = new CompositeValueProvider(new List<IValueProvider>
            {
                new QueryStringValueProvider(BindingSource.Query, actionContext.Context.Request.Query,
                    CultureInfo.InvariantCulture),
                new RouteValueProvider(BindingSource.Path, actionContext.RouteData)
            });

            for (var i = 0; i < Parameters.Count; i++)
            {
                var parameterDescriptor = Parameters[i];
                var bindingInfo = parameterBindingInfo[i];

                var result = await parameterBinding.BindModelAsync(actionContext, bindingInfo.ModelBinder,
                    valueProvider, parameterDescriptor, bindingInfo.ModelMetadata, value: null);

                if (result.IsModelSet)
                    arguments[i] = result.Model;
            }

            return await _executor.Execute(_objectMethodExecutor, controller, arguments, Metadata);
        }

        public Task<IActionResult> InvokeChannel(ActionContext actionContext, MazeChannel channel)
        {
            return InvokeMethod(actionContext, channel);
        }

        public async Task<MazeChannel> InitializeChannel(ActionContext actionContext, IChannelServer channelServer)
        {
            var channel = (MazeChannel) ObjectFactory.Invoke(actionContext.Context.RequestServices, new object[0]);
            channel.MazeContext = actionContext.Context;
            channel.ChannelId = channelServer.RegisterChannel(channel);

            await _executor.Execute(_objectMethodExecutor, channel, new object[0], Metadata);
            return channel;
        }

        private static BindingInfo GetBindingInfo(ParameterInfo parameter)
        {
            var attributes = parameter.GetCustomAttributes().ToList();
            var result = new BindingInfo();
            BindingSource? bindingSource = null;

            foreach (var sourceMetadata in attributes.OfType<IBindingSourceMetadata>())
            {
                bindingSource = sourceMetadata.BindingSource;
                break;
            }

            foreach (var modelNameProvider in attributes.OfType<IModelNameProvider>())
            {
                if (modelNameProvider.Name != null)
                {
                    result.BinderModelName = modelNameProvider.Name;
                    break;
                }
            }

            if (bindingSource == null)
                bindingSource = BindingSource.Path;

            result.BindingSource = bindingSource.Value;
            return result;
        }

        private BinderItem[] GetParameterBindingInfo(IModelBinderFactory modelBinderFactory)
        {
            if (Parameters.Count == 0)
                return null;

            var parameterBindingInfo = new BinderItem[Parameters.Count];
            for (var i = 0; i < Parameters.Count; i++)
            {
                var parameter = Parameters[i];
                var metadata = new ModelMetadata(parameter);

                var binder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
                {
                    BindingInfo = parameter.BindingInfo,
                    Metadata = metadata,
                    CacheToken = parameter
                });

                parameterBindingInfo[i] = new BinderItem(binder, metadata);
            }

            return parameterBindingInfo;
        }
        
        private struct BinderItem
        {
            public BinderItem(IModelBinder modelBinder, ModelMetadata modelMetadata)
            {
                ModelBinder = modelBinder;
                ModelMetadata = modelMetadata;
            }

            public IModelBinder ModelBinder { get; }
            public ModelMetadata ModelMetadata { get; }
        }
    }
}