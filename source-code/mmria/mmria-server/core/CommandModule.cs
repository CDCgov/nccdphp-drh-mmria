
using System;
using System.Linq;
//using Autofac;

namespace cqrs;
/*
public sealed class CommandModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterAssemblyTypes(ThisAssembly)
                .Where(x => x.IsAssignableTo<ICommandHandler>())
            .AsImplementedInterfaces();

        builder.Register<Func<Type, ICommandHandler>>(c =>
        {
            var ctx = c.Resolve<IComponentContext>();

            return t =>
            {
                var handlerType = typeof(ICommandHandler<>).MakeGenericType(t);
                return (ICommandHandler)ctx.Resolve(handlerType);
            };
        });

        builder.RegisterType<CommandBus>()
            .AsImplementedInterfaces();
    }
}
*/

