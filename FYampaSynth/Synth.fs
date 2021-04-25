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
    let oscSine (f0 : Frequency) : SignalFunction<ControlValue, Sample> =
        arr (fun cv ->
            let f = f0 * (2.0 ** cv)
            2.0 * Math.PI * f)
            >>> integral
            >>^ Math.Sin

    /// Sawtooth wave oscillator with dynamically controllable frequency.
    let oscSawtooth (f0 : Frequency) : SignalFunction<ControlValue, Sample> =
        arr (fun cv ->
            f0 * (2.0 ** cv))
            >>> integral
            >>^ (fun x -> 2.0 * (x - floor x) - 1.0)

    /// Moog transistor ladder filter.
    let moogVcf sr f0 r : SignalFunction<Sample * ControlValue, ControlValue> =

        let moogAux =
            let vt = 2.0 * 20000.0      // thermal voltage
            arr (fun ((x, g), ym1) ->   // ym1 = y(n-1)
                let y = ym1 + vt * g * (Math.Tanh(x/vt) - Math.Tanh(ym1/vt))
                y, y)
                |> loop 0.0

        let g =
            arr (fun cv ->
                let f = f0 * (2.0 ** cv)
                1.0 - Math.Exp(-2.0 * Math.PI * f / sr))

        let ya =
            arr (fun (x, g, yd) ->
                x - 4.0 * r * yd, g)
                >>> moogAux

        let pipeline =
            arr (fun ((x, g), yd) ->
                (((x, g, yd), g), g), g)
                >>> (first
                        (first
                            (first ya
                                >>> moogAux)   // yb
                                >>> moogAux)   // yc
                                >>> moogAux)   // yd
                >>> split
                |> loop 0.0
        second g >>> pipeline
