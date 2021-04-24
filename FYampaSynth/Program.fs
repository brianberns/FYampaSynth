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

    interface IDisposable with
        member __.Dispose() = output.Dispose()

module Program =

    open Arrow

    [<EntryPoint>]
    let main argv =
        use engine = new AudioEngine()
        let synth =
            Synth.oscSine 0.5
                >>^ (*) 0.5
                >>> Synth.oscSine 220.0
                >>^ (*) 0.02
                |> Synth
        engine.AddInput(synth)
        Console.ReadLine() |> ignore
        0
