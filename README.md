# M3F Trading System


Crypto currency trading system for CoreCLR


This set of libraries provides the basic elements of
a crypto currency trading system. It currently features
connectivity to the GDAX exchange, but it should be 
fairly straight-forward to add more exchange backends.
There is also a simple _Akka.net_ actor wrapper that
should aid in writing trading bots.

## Project Layout

* *M3F.TradingSystem* contains the core classes and interfaces
  for consuming prices and operating on orders.
  _ITicker_ allows for top-of-book price subscriptions, while 
  _IOrderManager_ allows for placing orders as well as receiving
  order update notifications.

* *M3F.TradingSystem.Actors* contains a set of classes that
  work with the _Akka.net_ actor ecosystem. Making simple trading
  strategies is easier with the actor model.
  
* *M3F.TradingSystem.Gdax* implements connectivity to the GDAX
  exchange and provides several helper classes for extracting
  data out of GDAX.
  
* *mpm* (aka Money Printing Machine 😉) is an example application
  which uses GDAX backend in conjunction with the actor system
  to implement a simple _buy low, sell high_ bot.
  
## Getting started

1. Download [.NET Core for your platform](https://www.microsoft.com/net/download/core)
   (I've been using Ubuntu 16.04).

2. Clone this git repository and go to its `src` directory.
3. Modify the [configuration](/src/mpm/example_gdax_config.json) [files](src/mpm/example_mpm_config.json)
   and adjust paths as necessary.
4. Execute `dotnet restore`
5. Execute `dotnet run --project mpm/mpm.csproj mpm/example_mpm_config.json` to run the example bot.

## Contributing

Any contributions are welcome! Simply open a pull request.
Feel free to work on any of the open issues, or report a new
issue if you feel its needed.


## License

See LICENSE file for the full text of BSD-style license.


### Keywords

gdax, coreclr, csharp, c#, .net, bitcoin, crypto, ethereum, ether, litecoin,
BTC, LTC, ETH
