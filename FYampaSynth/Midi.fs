namespace FYampaSynth

module Midi =

    /// Converts a MIDI note number to frequency, assuming
    /// equal temperament.
    let toFreq noteNum =
        440.0 * (2.0 ** (((float noteNum) - 69.0) / 12.0))
