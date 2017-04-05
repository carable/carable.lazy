# README #

This repository contains an implementation of Lazy<T> that has a timeout for the value.

# Get Started

## Install the NuGet package

Go to the defined NuGet feed and install the package [Carable.Lazy](https://www.myget.org/feed/carable/package/nuget/Carable.Lazy).

## Use in code

In order to have a lazy expiring value you do the following:

```
var l = new LazyExpiry<SomeValue>(delegate () 
{
  var val = GetExpensiveValue();
  return Tuple.Create(val.Value, val.ExpirationTime);
}, true);
```