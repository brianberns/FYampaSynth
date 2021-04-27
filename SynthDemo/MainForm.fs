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
        FormBorderStyle = FormBorderStyle.Fixed3D,
        StartPosition = FormStartPosition.CenterScreen)

    let padding = 10

    let trackNote =

        let label =
            new Label(
                Text = "Note",
                Location = Point(padding, padding),
                AutoSize = true)
                |> Control.addTo this

        new TrackBar(
            Minimum = 1,
            Maximum = 88,
            Value = 49,
            TickFrequency = 8,
            SmallChange = 1,
            LargeChange = 8,
            Size = Size(this.ClientSize.Width - label.Width - 3 * padding, 0),
            Location = Point(label.Width + padding, label.Location.Y))
            |> Control.addTo this

    let getNoteFrequency note =
        440.0 * 2.0 ** ((1.0/12.0) * (float note - 49.0))

    let trackVolume =

        let label =
            new Label(
                Text = "Volume",
                Location =
                    Point(
                        padding,
                        trackNote.Location.Y + trackNote.Height + padding),
                AutoSize = true)
                |> Control.addTo this

        new TrackBar(
            Minimum = 0,
            Maximum = 10,
            Value = 2,
            TickFrequency = 1,
            SmallChange = 1,
            LargeChange = 1,
            Size = Size(this.ClientSize.Width - label.Width - 3 * padding, 0),
            Location = Point(label.Width + padding, label.Location.Y))
            |> Control.addTo this

    let getGain volume =
        float volume / 100.0

    let makeSynth freq gain =
        let cv = Synth.oscSine 1.0
        let sawtooth = Synth.oscSawtooth freq
        (sawtooth &&& cv) >>> Synth.moogVcf 44100.0 (4.0 * freq) 0.5
            >>^ (*) gain
            |> Synth

    let btnPlay =
        new CheckBox(
            Text = "Play",
            Size = Size(100, 40),
            Location = Point(100, 200),
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter)
            |> Control.addTo this

    let engine = new AudioEngine()

    let onParamChanged _ =
        engine.RemoveAllInputs()
        if btnPlay.Checked then
            let freq = getNoteFrequency trackNote.Value
            let gain = getGain trackVolume.Value
            engine.AddInput(makeSynth freq gain)

    do
        btnPlay.CheckedChanged.Add(onParamChanged)
        trackNote.ValueChanged.Add(onParamChanged)
        trackVolume.ValueChanged.Add(onParamChanged)
