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
Signal functions are first-class objects in Yampa, and can be composed using ["arrow" combinators](https://www.haskell.org/arrows/) to synthesize music in software.

FYampaSynth uses [NAudio](https://github.com/naudio/NAudio) to generate sound, and hence is Windows-only.
## Example
A simple sine-wave oscillator can be created as follows:
```
let sf = Synth.oscSine 440.0   // A above middle C
```
This signal function takes
<!--stackedit_data:
eyJoaXN0b3J5IjpbMTgwNjkzNzYyOSw1NDMzMTM4NzVdfQ==
-->