/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace M3F.TradingSystem
{
    public class NewOrderSingle
    {
        public ClientOrderId ClientOrderId { get; set; }
        public Instrument Instrument { get; set; }
        public Side Side { get; set; }
        public ExchangeOrderType OrderType { get; set; }

        public decimal? Price { get; set; }
        public decimal Size { get; set; }

        public bool IsMakerOrCancel { get; set; }

        // TODO: Time in force
        // TODO: Implement ManagementMode (Cancel/Leave on disconnect)
        
        public NewOrderSingle ()
        {
            ClientOrderId   = new ClientOrderId();
            OrderType       = ExchangeOrderType.Limit;
            IsMakerOrCancel = true;
        }

        public NewOrderSingle (Instrument instrument, Side side, decimal? price, decimal size)
            : this()
        {
            Instrument = instrument;
            Side       = side;
            Price      = price;
            Size       = size;
        }

    }

}
