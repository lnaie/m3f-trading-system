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
    public class OrderEventArgs : EventArgs
    {
        protected OrderEventArgs (DateTime          timeStamp,
                                  ClientOrderId     clientOrderId,
                                  Instrument        instrument,
                                  ExchangeOrderType orderType,
                                  Side              side,
                                  decimal?          price,
                                  decimal           workingSize)
        {
            TimeStamp     = timeStamp;
            ClientOrderId = clientOrderId;
            Instrument    = instrument;
            OrderType     = orderType;
            Side          = side;
            Price         = price;
            WorkingSize   = workingSize;
        }

        public ClientOrderId ClientOrderId { get; }
        public Instrument Instrument { get; }
        public ExchangeOrderType OrderType { get; }
        public Side Side { get; }
        public decimal? Price { get; }
        public decimal WorkingSize { get; }
        public DateTime TimeStamp { get; }
    }

}
