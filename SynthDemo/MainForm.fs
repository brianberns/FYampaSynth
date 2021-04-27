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

type ControlType =
    | Constant = 0
    | Sine = 1
    | Sawtooth = 2

type MainForm() as this =
    inherit Form(
        Text = "Moog Demo",
        Size = Size(800, 400),
        FormBorderStyle = FormBorderStyle.Fixed3D,
        StartPosition = FormStartPosition.CenterScreen)

    let padding = 10
    let labelWidth = 75

    let trackNote =

        let label =
            new Label(
                Text = "Note",
                Location = Point(padding, padding),
                Width = labelWidth)
                |> Control.addTo this

        new TrackBar(
            Minimum = 1,
            Maximum = 88,
            Value = 49,
            TickFrequency = 12,
            SmallChange = 1,
            LargeChange = 12,
            Size = Size(this.ClientSize.Width - label.Width - 3 * padding, 0),
            Location = Point(label.Width + padding, label.Location.Y))
            |> Control.addTo this

    let trackCutoff =

        let label =
            new Label(
                Text = "Cutoff",
                Location =
                    Point(
                        padding,
                        trackNote.Location.Y + trackNote.Height),
                Width = labelWidth)
                |> Control.addTo this

        new TrackBar(
            Minimum = 1,
            Maximum = 88,
            Value = trackNote.Value + 2 * 12,
            TickFrequency = 12,
            SmallChange = 1,
            LargeChange = 12,
            Size = Size(this.ClientSize.Width - label.Width - 3 * padding, 0),
            Location = Point(label.Width + padding, label.Location.Y))
            |> Control.addTo this

    let getNoteFrequency note =
        440.0 * (2.0 ** ((1.0/12.0) * (float note - 49.0)))
        
    let trackControl =

        let label =
            new Label(
                Text = "Control",
                Location =
                    Point(
                        padding,
                        trackCutoff.Location.Y + trackCutoff.Height),
                Width = labelWidth)
                |> Control.addTo this

        new TrackBar(
            Minimum = -8,
            Maximum = 8,
            Value = 0,
            TickFrequency = 4,
            SmallChange = 1,
            LargeChange = 4,
            Size = Size(this.ClientSize.Width - label.Width - 3 * padding, 0),
            Location = Point(label.Width + padding, label.Location.Y))
            |> Control.addTo this

    let getControlFrequency value =
        2.0 ** (float value / 4.0)

    let trackVolume =

        let label =
            new Label(
                Text = "Volume",
                Location =
                    Point(
                        padding,
                        trackControl.Location.Y + trackControl.Height),
                Width = labelWidth)
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
        float volume / (10.0 * float trackVolume.Maximum)

    let makeSynth noteFreq cutoffFreq ctrlType ctrlFreq gain =
        let note = Synth.oscSawtooth noteFreq
        let ctrl =
            match ctrlType with
                | ControlType.Constant -> constant 0.0
                | ControlType.Sine -> Synth.oscSine ctrlFreq
                | ControlType.Sawtooth -> Synth.oscSawtooth ctrlFreq
                | _ -> failwith "Unexpected"
        let resonance = 0.5
        (note &&& ctrl) >>> Synth.moogVcf 44100.0 cutoffFreq resonance
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
            let noteFreq = getNoteFrequency trackNote.Value
            let cutoffFreq = getNoteFrequency trackCutoff.Value
            let ctrlType = ControlType.Sine
            let ctrlFreq = getControlFrequency trackControl.Value
            let gain = getGain trackVolume.Value
            makeSynth noteFreq cutoffFreq ctrlType ctrlFreq gain
                |> engine.AddInput

    do
        [
            btnPlay.CheckedChanged
            trackNote.ValueChanged
            trackCutoff.ValueChanged
            trackControl.ValueChanged
            trackVolume.ValueChanged
        ] |> Seq.iter (fun evt -> evt.Add(onParamChanged))
