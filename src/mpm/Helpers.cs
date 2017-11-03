/**
 * Copyright (c) 2017-present, Filip Frącz
 * Copyright (c) 2017-present, M3F Innovations, LLC
 * Copyright (c) 2017-present, lucian.naie@outlook.com
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree.
 */
using Newtonsoft.Json.Linq;

namespace M3F.TradingSystem.Mpm
{
    public class Helpers
    {
        public JObject ReadJson(string configFilePath)
        {
            var json = System.IO.File.ReadAllText(configFilePath);
            var config = JObject.Parse(json);
            return config;
        }
    }
}
