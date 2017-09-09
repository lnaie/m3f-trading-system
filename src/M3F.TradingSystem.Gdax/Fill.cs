/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace M3F.TradingSystem.Gdax
{
    public class Fill
    {
        public long TradeId { get; set; }
        public Instrument Instrument { get; set; }
        public decimal Price { get; set; }
        public decimal Size { get; set; }

        public Guid OrderId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public LiquidityType Liquidity { get; set; }

        public decimal Fee { get; set; }

        public bool IsSettled { get; set; }

        public Side Side { get; set; }

        public enum LiquidityType
        {
            Taker,
            Maker
        }
    }
}
