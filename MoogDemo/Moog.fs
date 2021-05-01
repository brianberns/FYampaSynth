namespace MoogDemo

open FYampaSynth
open Arrow

/// Variation types.
type VariationType =
    | Constant = 0
    | Sine = 1
    | Sawtooth = 2

module VariationType =

    /// Makes variation of the given type and frequency.
    let makeVariation variType variFreq =
        match variType with
            | VariationType.Constant -> constant 0.0
            | VariationType.Sine -> Synth.oscSine variFreq
            | VariationType.Sawtooth -> Synth.oscSawtooth variFreq
            | _ -> failwith "Unexpected"

module Synth =

    /// Hard-coded resonance, for now. Future: Allow this to vary.
    let private resonance = 0.5

    /// Creates either a Moog or unfiltered signal function.
    let private makeSynth note filterFreqOpt variation =
        match filterFreqOpt with
            | Some filterFreq ->
                (note &&& variation)
                    >>> Synth.moogVcf filterFreq resonance
            | None ->
                variation >>> note

    /// Builds a synthesizer that plays continuously from the given values.
    let makeContinuous noteFreq filterFreqOpt variation gain =
        let note = Synth.oscSawtooth noteFreq
        makeSynth note filterFreqOpt variation
            >>^ (*) gain
            |> Synth

    /// Builds a synthesizer that plays notes from the given values.
    let makeDiscrete filterFreqOpt variation gain =

        /// A bell-like envelope.
        let envBell =
            Synth.envGen 0.0 [(0.1, 1.0); (1.5, 0.0)] None

        /// Plays a single note at the given frequency.
        let playNote noteFreq =
            let note = Synth.oscSawtooth noteFreq
            let s =
                constant 0.0 >>>
                    makeSynth note filterFreqOpt variation
            let e =
                constant NoEvt
                    >>> envBell
                    >>^ fst
            (s &&& e) >>^ (fun (x, y) -> x * y)

        /// Plays a series of notes.
        let playNotes =
            let rec playNotesRec freq =
                Event.switch
                    (playNote freq &&& Event.notYet)
                    playNotesRec
            Event.switch
                (constant (0.0 : Sample) &&& identity)
                playNotesRec

        let notes =
            let dt = 2.0
            [
                0.0, 60
                dt, 62
                dt, 64
                dt, 65
                dt, 67
                dt, 69
                dt, 71
                dt, 72
            ] |> List.map (fun (time, note) ->
                time, Midi.toFreq note)
        Event.afterEach notes
            >>> playNotes
            >>^ (*) gain
            |> Synth
