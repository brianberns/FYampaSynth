namespace FYampaSynth

type Frequency = float
type ControlValue = float
type Sample = float

module Synth =

    /// Integrates a sample over time.
    let integral =
        let rec loop (acc : Sample) (prev : Sample) =
            SF (fun dt (samp : Sample) ->
                let acc' = acc + (dt * prev)   // rectangle rule
                loop acc' samp, acc')
        loop 0.0 0.0
