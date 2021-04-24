namespace FYampaSynth

open System
open NAudio.Wave

type Frequency = float
type ControlValue = float
type Sample = float

/// Synthesizes sound using the given signal function.
type Synth(sf) =

    let numChannels = 2
    let sampleRate = 44100
    let dt = 1.0 / float sampleRate

    /// Populates the given buffer using the given signal function.
    let read sf (buffer : float32[]) offset count =
        assert(count % numChannels = 0)
        let numSamples = count / numChannels
        (sf, seq { 0 .. numSamples - 1 })
            ||> Seq.fold (fun (SF tf) iSample ->
                let sf', (sample : float) = tf dt 0.0
                let sample = float32 sample
                let idx = numChannels * (offset + iSample)
                for iChannel = 0 to numChannels - 1 do
                    buffer.[idx + iChannel] <- sample
                sf')

    /// Current state of the signal function.
    let mutable sfCur = sf

    interface ISampleProvider with

        member __.WaveFormat =
            WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, numChannels)

        member __.Read(buffer, offset, count) =
            sfCur <- read sfCur buffer offset count
            count

/// https://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.159.2277&rep=rep1&type=pdf
module Synth =

    open Arrow

    /// Integrates a signal over time.
    let integral =
        let rec loop acc prev =
            SF (fun dt cur ->
                let acc' = acc + (float dt * prev)   // rectangle rule
                loop acc' cur, acc')
        loop 0.0 0.0

    /// Sine wave oscillator with dynamically controllable frequency.
    let oscSine (nominalFreq : Frequency) : SignalFunction<ControlValue, Sample> =
        let angularFreq =
            arr (fun cv ->
                let varyingFreq = nominalFreq * (2.0 ** cv)
                2.0 * Math.PI * varyingFreq)
        angularFreq >>> integral >>^ Math.Sin
