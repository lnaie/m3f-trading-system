/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace M3F.TradingSystem
{
    public class ExchangeOrderAcknowledgedEventArgs : OrderEventArgs
    {
        public ExchangeOrderId ExchangeOrderId { get; }
        
        public ExchangeOrderAcknowledgedEventArgs (DateTime          timeStamp,
                                                   ExchangeOrderId   exchangeOrderId,
                                                   ClientOrderId     clientOrderId,
                                                   Instrument        instrument,
                                                   ExchangeOrderType exchangeOrderType,
                                                   Side              side,
                                                   decimal?          price,
                                                   decimal           workingSize)
            : base(timeStamp,
                   clientOrderId,
                   instrument,
                   exchangeOrderType,
                   side,
                   price,
                   workingSize)
        {
            ExchangeOrderId = exchangeOrderId;
        }

    }

}
