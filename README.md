# README #

This repository contains an implementation of Lazy<T> that has a timeout for the value.

Why would you want to do something like this? A good start is to start looking at why you use Lazy<T>

https://msdn.microsoft.com/en-us/library/dd997286(v=vs.110).aspx

Say that the value has a given lifespan, then you get into a situation where you want to invalidate a previous value and hold on to a new one.

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