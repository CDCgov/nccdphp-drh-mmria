using System;
using System.Linq;
using System.Collections.Generic;
//using Autofac;

namespace cqrs;

/*
public sealed class EventModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterAssemblyTypes(ThisAssembly)
            .Where(x => x.IsAssignableTo<IEventHandler>())
            .AsImplementedInterfaces();

        builder.RegisterGeneric(typeof(IEventHandler<>))
            .As(typeof(IEventHandler<>));

        builder.Register<Func<Type, IEnumerable<IEventHandler>>>(c =>
        {
            var ctx = c.Resolve<IComponentContext>();
            return t =>
            {
                var handlerType = typeof(IEventHandler<>).MakeGenericType(t);
                var handlersCollectionType = typeof(IEnumerable<>).MakeGenericType(handlerType);
                return (IEnumerable<IEventHandler>)ctx.Resolve(handlersCollectionType);
            };
        });

        builder.RegisterType<EventBus>()
                .AsImplementedInterfaces();
    }
}
*/


