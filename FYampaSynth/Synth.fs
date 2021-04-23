namespace FYampaSynth

open System

type Frequency = float
type ControlValue = float
type Sample = float

/// https://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.159.2277&rep=rep1&type=pdf
module Synth =

    open Arrow

    /// Integrates a sample over time.
    let integral =
        let rec loop (acc : Sample) (prev : Sample) =
            SF (fun dt (samp : Sample) ->
                let acc' = acc + (dt * prev)   // rectangle rule
                loop acc' samp, acc')
        loop 0.0 0.0

    /// Sine wave oscillator with dynamically controllable frequency.
    let oscSine (nominalFreq : Frequency) : SignalFunction<ControlValue, Sample> =
        let angularFreq =
            arr (fun cv ->
                let varyingFreq = nominalFreq * (2.0 ** cv)
                2.0 * Math.PI * varyingFreq)
        angularFreq >>> integral >>^ Math.Sin
