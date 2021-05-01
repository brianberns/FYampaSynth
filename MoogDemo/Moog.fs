namespace MoogDemo

open FYampaSynth
open Arrow

module Synth =

    let private resonance = 0.5

    /// Builds a synthesizer that plays continuously from the given values.
    let makeContinuous noteFreq filterFreqOpt variation gain =
        let note = Synth.oscSawtooth noteFreq
        let pipeline =
            match filterFreqOpt with
                | Some filterFreq ->
                    (note &&& variation)
                        >>> Synth.moogVcf filterFreq resonance
                | None ->
                    variation >>> note
        pipeline
            >>^ (*) gain
            |> Synth

    /// Builds a synthesizer that plays notes from the given values.
    let makeDiscrete filterFreqOpt variation gain =
        let envBell =
            Synth.envGen 0.0 [(0.1, 1.0); (1.5, 0.0)] None   // a bell-like envelope

        /// Plays a single note at the given frequency.
        let playNote freq =
            let s =
                match filterFreqOpt with
                    | Some filterFreq ->
                        constant 0.0
                            >>> (Synth.oscSawtooth freq &&& variation)
                            >>> Synth.moogVcf filterFreq resonance
                    | None ->
                        constant 0.0
                            >>> variation
                            >>> Synth.oscSawtooth freq
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
