﻿using Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cofoundry.Core.DependencyInjection;

namespace Cofoundry.Core.MessageAggregator
{
    /// <summary>
    /// Represents an individual subscription to a message
    /// </summary>
    /// <typeparam name="TMessageSubscribedTo">Type of message subscribed to</typeparam>
    /// <typeparam name="TMessageHandler">Type of handler to invoke when a message is published</typeparam>
    public class MessageSubscription<TMessageSubscribedTo, TMessageHandler>
        : IMessageSubscription
        where TMessageSubscribedTo : class
        where TMessageHandler : IMessageHandler<TMessageSubscribedTo>
    {
        public bool CanDeliver<TMessage>()
        {
            return typeof(TMessageSubscribedTo).IsAssignableFrom(typeof(TMessage));
        }

        public async Task DeliverAsync(IResolutionContext resolutionContext, object message)
        {
            Condition.Requires(message)
                .IsNotNull()
                .IsOfType(typeof(TMessageSubscribedTo));

            var handler = resolutionContext.Resolve<TMessageHandler>();
            await handler.HandleAsync((TMessageSubscribedTo)message);
        }
    }
}
