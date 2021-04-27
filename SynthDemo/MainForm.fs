namespace SynthDemo

open System.Drawing
open System.Windows.Forms

open FYampaSynth
open Arrow

module Control =

    /// Adds the given control to the given parent control fluently.
    let addTo (parent : Control) (control : 't when 't :> Control) =
        parent.Controls.Add(control)
        control

type MainForm() as this =
    inherit Form(
        Text = "Moog Demo",
        Size = Size(800, 400),
        StartPosition = FormStartPosition.CenterScreen)

    let engine = new AudioEngine()

    let trackNote =
        new TrackBar(
            Text = "Note",
            Minimum = 1,
            Maximum = 88,
            Value = 49,
            TickFrequency = 8,
            SmallChange = 1,
            LargeChange = 8,
            Size = Size(this.ClientSize.Width, 50),
            Location = Point(0, 100))
            |> Control.addTo this

    let getNoteFrequency note =
        440.0 * 2.0 ** ((1.0/12.0) * (float note - 49.0))

    let makeSynth freq =
        let cv = Synth.oscSine 1.0
        let sawtooth = Synth.oscSawtooth freq
        (sawtooth &&& cv) >>> Synth.moogVcf 44100.0 (4.0 * freq) 0.5
            >>^ (*) 0.02
            |> Synth

    let btnPlay =
        new CheckBox(
            Text = "Play",
            Size = Size(100, 40),
            Location = Point(100, 200),
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter)
            |> Control.addTo this

    let onParamChanged _ =
        engine.RemoveAllInputs()
        if btnPlay.Checked then
            let freq = getNoteFrequency trackNote.Value
            engine.AddInput(makeSynth freq)

    do
        btnPlay.CheckedChanged.Add(onParamChanged)
        trackNote.ValueChanged.Add(onParamChanged)
