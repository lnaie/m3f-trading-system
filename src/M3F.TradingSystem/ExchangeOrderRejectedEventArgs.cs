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
    public class ExchangeOrderRejectedEventArgs : OrderEventArgs
    {
        public string RejectReason { get; }

        public ExchangeOrderRejectedEventArgs (DateTime timeStamp,
                                               ClientOrderId clientOrderId,
                                               Instrument instrument,
                                               ExchangeOrderType orderType,
                                               Side side,
                                               decimal? price,
                                               decimal size,
                                               string rejectReason) : base(timeStamp,
                                                                           clientOrderId,
                                                                           instrument,
                                                                           orderType,
                                                                           side,
                                                                           price,
                                                                           size)
        {
            RejectReason = rejectReason;
        }
    }
}