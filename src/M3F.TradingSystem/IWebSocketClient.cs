/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * Copyright (c) 2017-present, lucian.naie@outlook.com
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace M3F.TradingSystem
{
    public interface IWebSocketClient
    {
        void Start();
        void Disconnect();
    }
}
