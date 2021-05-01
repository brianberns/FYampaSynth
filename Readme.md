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
## Examples
A simple sine wave can be generated as follows:
```
open FYampaSynth
open Arrow

use engine = new AudioEngine()
Synth.oscSine 440.0   // A above middle C
    >>^ (*) 0.05      // reduce volume (sine waves are obnoxious!)
    |> Synth
    |> engine.AddInput
```
To make a slightly more interesting synthesizer, we can use a sawtooth wave instead and slowly oscillate its frequency:

```
Synth.oscSine 1.0   // low-frequency control value
    >>> Synth.oscSawtooth 440.0
```
More pleasingly, we can emulate an old Moog synthesizer (via subtractive synthesis), like this:
```
(Synth.oscSawtooth 440.00
    &&& Synth.oscSine 1.0)        // note and oscillation
    >>> Synth.moogVcf 880.0 0.5   // filter frequencies above 880 hz
```

## Additional references
* [Modular Synthesizer Programming in Haskell](https://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.159.2277&rep=rep1&type=pdf): Paper describing the original YampaSynth
* [Fire and Forget Audio Playback with NAudio](https://markheath.net/post/fire-and-forget-audio-playback-with)
* [The Yampa Implementation](http://www.cs.nott.ac.uk/~psznhn/ITU-FRP2010/LectureNotes/lecture05.pdf): 
<!--stackedit_data:
eyJoaXN0b3J5IjpbMTIzNjE0MDcwNywtNjEwOTgzMTY5LC02NT
kwMjE2MjEsNDIwMTg1NTk0LDU0MzMxMzg3NV19
-->