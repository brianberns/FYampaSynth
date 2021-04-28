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
        Size = Size(800, 300),
        FormBorderStyle = FormBorderStyle.Fixed3D,
        StartPosition = FormStartPosition.CenterScreen)

    let padding = 10
    let labelWidth = 50

    let trackNote =

        let label =
            new Label(
                Text = "Note",
                TextAlign = ContentAlignment.MiddleLeft,
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
            Size =
                Size(
                    this.ClientSize.Width - label.Location.X - label.Width - padding,
                    0),
            Location = Point(label.Width + padding, label.Location.Y))
            |> Control.addTo this

    let chkFilter, trackFilter =

        let chkFilter =
            new CheckBox(
                Text = "Filter",
                Checked = true,
                Location =
                    Point(
                        padding,
                        trackNote.Location.Y + trackNote.Height),
                Width = labelWidth,
                Appearance = Appearance.Button,
                TextAlign = ContentAlignment.MiddleLeft)
                |> Control.addTo this

        let trackCuttof =
            new TrackBar(
                Minimum = 1,
                Maximum = 88,
                Value = trackNote.Value + 12,
                TickFrequency = 12,
                SmallChange = 1,
                LargeChange = 12,
                Size =
                    Size(
                        this.ClientSize.Width - chkFilter.Location.X - chkFilter.Width - padding,
                        0),
                Location = Point(chkFilter.Width + padding, chkFilter.Location.Y))
                |> Control.addTo this

        chkFilter, trackCuttof

    let getNoteFrequency note =
        440.0 * (2.0 ** ((1.0/12.0) * (float note - 49.0)))
        
    let rbControlTypes, trackControl =

        let label =
            new Label(
                Text = "Control",
                TextAlign = ContentAlignment.MiddleLeft,
                Location =
                    Point(
                        padding,
                        trackFilter.Location.Y + trackFilter.Height),
                Width = labelWidth)
                |> Control.addTo this

        let panel =
            new Panel(
                Size = Size(80, 60),
                Location = Point(label.Width + padding, label.Location.Y))
                |> Control.addTo this
        let ctrlTypes =
            [|
                ControlType.Constant
                ControlType.Sine
                ControlType.Sawtooth
            |]

        let rbControlTypes =
            ctrlTypes
                |> Array.map (fun ctrlType ->
                    new RadioButton(
                        Checked = (ctrlType = ControlType.Sine),
                        Text = string ctrlType,
                        Tag = ctrlType,
                        Location = Point(padding, 20 * int ctrlType))
                        |> Control.addTo panel)

        let trackControl =
            new TrackBar(
                Minimum = -8,
                Maximum = 8,
                Value = 0,
                TickFrequency = 4,
                SmallChange = 1,
                LargeChange = 4,
                Size =
                    Size(
                        this.ClientSize.Width - panel.Location.X - panel.Width - padding,
                        0),
                Location =
                    Point(
                        panel.Location.X + panel.Width,
                        label.Location.Y))
                |> Control.addTo this

        rbControlTypes, trackControl

    let getControlFrequency value =
        2.0 ** (float value / 4.0)

    let trackVolume =

        let label =
            let panel = rbControlTypes.[0].Parent
            new Label(
                Text = "Volume",
                Location =
                    Point(
                        padding,
                        panel.Location.Y + panel.Height),
                Width = labelWidth)
                |> Control.addTo this

        new TrackBar(
            Minimum = 0,
            Maximum = 10,
            Value = 2,
            TickFrequency = 1,
            SmallChange = 1,
            LargeChange = 1,
            Size =
                Size(
                    this.ClientSize.Width - label.Location.X - label.Width - padding,
                    0),
            Location = Point(label.Width + padding, label.Location.Y))
            |> Control.addTo this

    let getGain volume =
        float volume / (10.0 * float trackVolume.Maximum)

    let btnPlay =
        let width = 100
        new CheckBox(
            Text = "Play",
            Size = Size(width, 40),
            Location =
                Point(
                    (this.ClientSize.Width - width) / 2,
                    trackVolume.Location.Y + trackVolume.Height),
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter)
            |> Control.addTo this

    let makeSynth noteFreq filterFreqOpt ctrlType ctrlFreq gain =
        let note = Synth.oscSawtooth noteFreq
        let ctrl =
            match ctrlType with
                | ControlType.Constant -> constant 0.0
                | ControlType.Sine -> Synth.oscSine ctrlFreq
                | ControlType.Sawtooth -> Synth.oscSawtooth ctrlFreq
                | _ -> failwith "Unexpected"
        let pipeline =
            match filterFreqOpt with
                | Some filterFreq ->
                    let resonance = 0.5
                    (note &&& ctrl)
                        >>> Synth.moogVcf 44100.0 filterFreq resonance
                | None ->
                    ctrl >>> note
        pipeline
            >>^ (*) gain
            |> Synth

    let engine = new AudioEngine()

    let onParamChanged _ =
        engine.RemoveAllInputs()
        if btnPlay.Checked then
            let noteFreq = getNoteFrequency trackNote.Value
            let filterFreqOpt =
                if chkFilter.Checked then
                    getNoteFrequency trackFilter.Value |> Some
                else None
            let ctrlType =
                rbControlTypes
                    |> Seq.where (fun rb -> rb.Checked)
                    |> Seq.map (fun rb -> rb.Tag :?> ControlType)
                    |> Seq.exactlyOne
            let ctrlFreq = getControlFrequency trackControl.Value
            let gain = getGain trackVolume.Value
            makeSynth noteFreq filterFreqOpt ctrlType ctrlFreq gain
                |> engine.AddInput

    let onLoad _ =
        btnPlay.Select()

    do
        [
            btnPlay.CheckedChanged
            trackNote.ValueChanged
            chkFilter.CheckedChanged
            trackFilter.ValueChanged
            trackControl.ValueChanged
            trackVolume.ValueChanged
            yield! rbControlTypes
                |> Seq.map (fun rb -> rb.CheckedChanged)
        ] |> Seq.iter (fun evt -> evt.Add(onParamChanged))
        this.Load.Add(onLoad)
