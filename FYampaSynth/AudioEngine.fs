namespace FYampaSynth

open System

open NAudio.Wave
open NAudio.Wave.SampleProviders

/// https://markheath.net/post/fire-and-forget-audio-playback-with
type AudioEngine() =

    let output = new WaveOutEvent()
    let mixer =
        let format = WaveFormat.CreateIeeeFloatWaveFormat(Sample.rate, 2)
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
