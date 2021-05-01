namespace FYampaSynth.Test

open System
open FYampaSynth
open Arrow

module Program =

    [<EntryPoint>]
    let main _ =
        use engine = new AudioEngine()
        (Synth.oscSawtooth 440.00
            &&& Synth.oscSine 1.0)        // note and oscillation
            >>> Synth.moogVcf 880.0 0.5   // filter frequencies above 880 hz
            >>^ (*) 0.05
            |> Synth
            |> engine.AddInput
        Console.ReadLine() |> ignore
        0
