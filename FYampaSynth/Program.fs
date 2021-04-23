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

    [<EntryPoint>]
    let main argv =
        use engine = new AudioEngine()
        let sp = SampleProvider(Synth.oscSine 440.0)
        engine.AddInput(sp :> ISampleProvider)
        Console.ReadLine() |> ignore
        0
