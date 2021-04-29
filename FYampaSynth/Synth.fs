namespace FYampaSynth

open System
open NAudio.Wave

type Frequency = float
type ControlValue = float
type Sample = float

module Sample =

    /// Samples/second.
    let rate = 44100   // CD quality

/// Synthesizes sound using the given signal function.
type Synth(sf) =

    let numChannels = 2
    let dt = 1.0 / float Sample.rate

    /// Populates the given buffer using the given signal function.
    let read sf (buffer : float32[]) offset count =
        assert(count % numChannels = 0)
        let numSamples = count / numChannels
        (sf, seq { 0 .. numSamples - 1 })
            ||> Seq.fold (fun (SF tf) iSample ->
                let sf', (sample : float) = tf dt 0.0   // feed zeros in for now
                let sample = float32 sample
                let idx = numChannels * (offset + iSample)
                for iChannel = 0 to numChannels - 1 do
                    buffer.[idx + iChannel] <- sample
                sf')

    /// Current state of the signal function.
    let mutable sfCur = sf

    interface ISampleProvider with

        member __.WaveFormat =
            WaveFormat.CreateIeeeFloatWaveFormat(Sample.rate, numChannels)

        member __.Read(buffer, offset, count) =
            sfCur <- read sfCur buffer offset count
            count

/// https://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.159.2277&rep=rep1&type=pdf
module Synth =

    open Arrow

    let private pi = Math.PI

    /// Integrates a signal over time.
    let integral =
        let rec loop acc prev =
            SF (fun dt cur ->
                let acc' = acc + (float dt * prev)   // rectangle rule
                loop acc' cur, acc')
        loop 0.0 0.0

    /// Sine wave oscillator with dynamically controllable frequency.
    let oscSine (f0 : Frequency) : SignalFunction<ControlValue, Sample> =
        arr (fun cv ->
            let f = f0 * (2.0 ** cv)
            2.0 * pi * f)
            >>> integral
            >>^ sin

    /// Sawtooth wave oscillator with dynamically controllable frequency.
    let oscSawtooth (f0 : Frequency) : SignalFunction<ControlValue, Sample> =
        arr (fun cv ->
            f0 * (2.0 ** cv))
            >>> integral
            >>^ (fun x -> 2.0 * (x - floor x) - 1.0)

    /// Moog transistor ladder filter.
    let moogVcf f0 r : SignalFunction<Sample * ControlValue, ControlValue> =

        let moogAux =
            let vt = 2.0 * 20000.0      // thermal voltage
            arr (fun ((x, g), ym1) ->   // ym1 = y(n-1)
                let y = ym1 + vt * g * (tanh (x/vt) - tanh (ym1/vt))
                y, y)
                |> loop 0.0

        let g =
            let sr = float Sample.rate
            arr (fun cv ->
                let f = f0 * (2.0 ** cv)
                1.0 - exp (-2.0 * pi * f / sr))

        let ya =
            arr (fun (x, g, yd) ->
                x - 4.0 * r * yd, g)
                >>> moogAux
        let yb = first ya >>> moogAux
        let yc = first yb >>> moogAux
        let yd = first yc >>> moogAux

        let phaseShift =
            split
                >>> first (delay 0.0)
                >>> arr (fun (x, y) -> (x + y) / 2.0)
                >>> delay 0.0

        let pipeline =
            arr (fun ((x, g), yd) ->
                (((x, g, yd), g), g), g)
                >>> yd
                >>> phaseShift
                >>> split
                |> loop 0.0

        second g >>> pipeline

    let private envGenAux l0 tls =

        let rec trAux (t : Time) (l : ControlValue) = function
            | (t', l') :: tls ->
                let r : float = (l' - l) / t'
                (t, r) :: trAux t' l' tls
            | [] -> [t, 0.0]

        let toRates (l0 : ControlValue) = function
            | (t, l) :: tls ->
                let r : float = (l - l0) / t
                r, trAux t l tls
            | [] -> 0.0, []

        let r0, trs = toRates l0 tls
        Event.afterEach trs
            >>> Event.hold r0
            >>> integral
            >>> arr ((+) l0)

    let envGen l0 tls = function
        | Some n ->
            let tls1, tls2 = List.splitAt n tls
            Event.switch
                (envGenAux l0 tls1 &&& identity
                    >>> arr (fun (l, (noteOff : Event<Unit>)) ->
                            (l, NoEvt), noteOff |> Event.tag l))
                (fun l ->
                    envGenAux l tls2
                        &&& Event.after (tls2 |> List.sumBy fst) ())
        | None ->
            envGenAux l0 tls
                &&& Event.after (tls |> List.sumBy fst) ()
