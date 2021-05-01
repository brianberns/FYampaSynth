# Modular Synthesizer Programming in F#
## Overview
FYampaSynth is a modular music synthesizer in F# based on Haskell's [Yampa](https://wiki.haskell.org/Yampa) and [YampaSynth](https://hackage.haskell.org/package/YampaSynth). It is based on the concept of a "signal function", which maps an input stream to an output stream:
```
type SignalFunction<'a, 'b> = Signal<'a> -> Signal<'b>
```
Where a "signal" is (conceptually) a function from time to values of some type:
```
type Signal<'a> = Time -> 'a
```
Signal functions are first-class objects in Yampa, and can be composed to produce synthesizers using ["arrow" combinators](https://www.haskell.org/arrows/).
<!--stackedit_data:
eyJoaXN0b3J5IjpbLTk5MDk2NDg0Nl19
-->