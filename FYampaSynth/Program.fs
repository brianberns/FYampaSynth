namespace FYampaSynth
open System
open System.Threading

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
        let sp =
            Synth.oscSine 5.0
                >>^ (*) 0.05
                >>> Synth.oscSine 440.0
                >>^ (*) 0.02
                |> SampleProvider
        engine.AddInput(sp)
        Console.ReadLine() |> ignore
        0
