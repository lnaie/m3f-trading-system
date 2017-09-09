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
    public class InsideMarketChangedEventArgs : EventArgs
    {
        public Instrument Instrument { get; }
        public InsideMarket NewInsideMarket { get; }
        public InsideMarket OldInsideMarket { get; }
        
        public InsideMarketChangedEventArgs (Instrument   instrument,
                                             InsideMarket newInsideMarket,
                                             InsideMarket oldInsideMarket)
        {
            Instrument      = instrument;
            NewInsideMarket = newInsideMarket;
            OldInsideMarket = oldInsideMarket;
        }

    }

}
