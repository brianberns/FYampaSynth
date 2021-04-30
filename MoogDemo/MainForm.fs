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

/// Variation types.
type VariationType =
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
    let labelWidth = 55

    /// Slider that determines note to play.
    let trackNote =

        let label =
            new Label(
                Text = "Note",
                TextAlign = ContentAlignment.MiddleLeft,
                Location = Point(padding, padding),
                Width = labelWidth)
                |> Control.addTo this

        new TrackBar(
            Minimum = 21,
            Maximum = 108,
            Value = 69,
            TickFrequency = 12,
            SmallChange = 1,
            LargeChange = 12,
            Size =
                Size(
                    this.ClientSize.Width
                        - label.Location.X
                        - label.Width
                        - padding,
                    0),
            Location =
                Point(label.Width + padding, label.Location.Y))
            |> Control.addTo this

    /// Slider that determines filter cutoff (i.e. "corner")
    /// frequency.
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
                Minimum = 21,
                Maximum = 108,
                Value = trackNote.Value + 12,
                TickFrequency = 12,
                SmallChange = 1,
                LargeChange = 12,
                Size =
                    Size(
                        this.ClientSize.Width
                            - chkFilter.Location.X
                            - chkFilter.Width
                            - padding,
                        0),
                Location =
                    Point(chkFilter.Width + padding, chkFilter.Location.Y))
                |> Control.addTo this

        chkFilter, trackCuttof
       
    /// Slider that determines variation frequency.
    let rbVariationTypes, trackVariation =

        let label =
            new Label(
                Text = "Variation",
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
                Location =
                    Point(label.Width + padding, label.Location.Y))
                |> Control.addTo this
        let variationTypes =
            [|
                VariationType.Constant
                VariationType.Sine
                VariationType.Sawtooth
            |]

        let rbVariationTypes =
            variationTypes
                |> Array.map (fun variType ->
                    new RadioButton(
                        Checked = (variType = VariationType.Sine),
                        Text = string variType,
                        Tag = variType,
                        Location = Point(padding, 20 * int variType))
                        |> Control.addTo panel)

        let trackVariation =
            new TrackBar(
                Minimum = -8,
                Maximum = 8,
                Value = 0,
                TickFrequency = 4,
                SmallChange = 1,
                LargeChange = 4,
                Size =
                    Size(
                        this.ClientSize.Width
                            - panel.Location.X
                            - panel.Width
                            - padding,
                        0),
                Location =
                    Point(
                        panel.Location.X + panel.Width,
                        label.Location.Y))
                |> Control.addTo this

        rbVariationTypes, trackVariation

    /// Converts variation to frequency on a logarithmic scale.
    let getVariationFrequency value =
        2.0 ** (float value / 4.0)

    /// Slider that determines volume.
    let trackVolume =

        let label =
            let panel = rbVariationTypes.[0].Parent
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

    /// Converts volume to gain.
    let getGain volume =
        float volume / (10.0 * float trackVolume.Maximum)

    /// On/Off button.
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

    let btnTest =
        new Button(
            Text = "Test",
            Size = btnPlay.Size,
            Location = btnPlay.Location + Size(btnPlay.Width + padding, 0))
            |> Control.addTo this

    /// Builds a Moog synthesizer from the given values.
    let makeSynth noteFreq filterFreqOpt variType variFreq gain =
        let note = Synth.oscSawtooth noteFreq
        let variation =
            match variType with
                | VariationType.Constant -> constant 0.0
                | VariationType.Sine -> Synth.oscSine variFreq
                | VariationType.Sawtooth -> Synth.oscSawtooth variFreq
                | _ -> failwith "Unexpected"
        let pipeline =
            match filterFreqOpt with
                | Some filterFreq ->
                    let resonance = 0.5
                    (note &&& variation)
                        >>> Synth.moogVcf filterFreq resonance
                | None ->
                    variation >>> note
        pipeline
            >>^ (*) gain
            |> Synth

    let makeBell gain =

        let envBell =
            Synth.envGen 0.0 [(0.1, 1.0); (1.5, 0.0)] None

        let playNote freq =
            let s =
                constant 0.0
                    >>> Synth.oscSine 5.0
                    >>^ ((*) 0.05)
                    >>> Synth.oscSine freq
            let e =
                constant NoEvt
                    >>> envBell
                    >>^ fst
            (s &&& e) >>^ (fun (x, y) -> x * y)

        let playNotes : SignalFunction<Event<Frequency>, Sample> =

            let rec playNotesRec freq =
                Event.switch
                    (playNote freq &&& Event.notYet)
                    playNotesRec

            Event.switch
                (constant 0.0 &&& identity)
                playNotesRec

        let notes =
            [(0.0, 60); (2.0, 62); (2.0, 64); (2.0, 65); (2.0, 67); (2.0, 69); (2.0, 71); (2.0, 72)]
                |> List.map (fun (time, note) -> time, Midi.toFreq note)
        Event.afterEach notes
            >>> playNotes
            >>^ (*) gain
            |> Synth

    let engine = new AudioEngine()

    /// Rebuild synthesizer when a parameter has changed.
    let onParamChanged _ =
        engine.RemoveAllInputs()
        if btnPlay.Checked then
            let noteFreq = Midi.toFreq trackNote.Value
            let filterFreqOpt =
                if chkFilter.Checked then
                    Midi.toFreq trackFilter.Value |> Some
                else None
            let variType =
                rbVariationTypes
                    |> Seq.where (fun rb -> rb.Checked)
                    |> Seq.map (fun rb -> rb.Tag :?> VariationType)
                    |> Seq.exactlyOne
            let variFreq = getVariationFrequency trackVariation.Value
            let gain = getGain trackVolume.Value
            makeSynth noteFreq filterFreqOpt variType variFreq gain
                |> engine.AddInput

    let onLoad _ =
        btnPlay.Select()

    let onTest _ =
        engine.RemoveAllInputs()
        makeBell 0.2 |> engine.AddInput

    do
        [
            btnPlay.CheckedChanged
            trackNote.ValueChanged
            chkFilter.CheckedChanged
            trackFilter.ValueChanged
            trackVariation.ValueChanged
            trackVolume.ValueChanged
            yield! rbVariationTypes
                |> Seq.map (fun rb -> rb.CheckedChanged)
        ] |> Seq.iter (fun evt -> evt.Add(onParamChanged))
        this.Load.Add(onLoad)
        btnTest.Click.Add(onTest)
