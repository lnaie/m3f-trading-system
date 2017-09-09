/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Threading.Tasks;

namespace M3F.TradingSystem
{
    public interface IOrderManager
    {
        Task
        NewExchangeOrder (NewOrderSingle nos);

        Task
        CancelExchangeOrder (ExchangeOrderId exchangeOrderId);

        event EventHandler<ExchangeOrderAcknowledgedEventArgs> ExchangeOrderAcknowledged;
        event EventHandler<ExchangeOrderRejectedEventArgs>     ExchangeOrderRejected;
        event EventHandler<ExchangeOrderFilledEventArgs>       ExchangeOrderFilled;
        event EventHandler<ExchangeOrderCancelledEventArgs>    ExchangeOrderCancelled;

        // TODO: Add support for changing orders
        //
        // For example:
        // Task ChangeExchangeOrder (ClientOrderId originalClientOrderId,
        //                           decimal? price,
        //                           decimal quantity);

        // TODO: Add event for ExchangeOrderChanged/Updated
    }

}
