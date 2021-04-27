namespace FYampaSynth

open System

open NAudio.Wave
open NAudio.Wave.SampleProviders

type AudioEngine() =

    let output = new WaveOutEvent()
    let mixer =
        let format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)
        MixingSampleProvider(format, ReadFully = true)

    do
        output.Init(mixer)
        output.Play()

    member __.AddInput(input : ISampleProvider) =
        mixer.AddMixerInput(input)

    member __.RemoveInput(input : ISampleProvider) =
        mixer.RemoveMixerInput(input)

    member __.RemoveAllInputs() =
        mixer.RemoveAllMixerInputs()

    interface IDisposable with
        member __.Dispose() = output.Dispose()

module Program =

    open Arrow

    [<EntryPoint>]
    let main argv =
        use engine = new AudioEngine()
        let synth =
            let cv = Synth.oscSine 1.0
            let note = 220.0
            let sawtooth = Synth.oscSawtooth note
            // (cv >>> sawtooth)
            (sawtooth &&& cv) >>> Synth.moogVcf 44100.0 (4.0 * note) 0.5
                >>^ (*) 0.02
                |> Synth
        engine.AddInput(synth)
        Console.ReadLine() |> ignore
        0
