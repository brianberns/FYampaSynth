namespace FYampaSynth

/// http://www.cs.nott.ac.uk/~psznhn/ITU-FRP2010/LectureNotes/lecture05.pdf

type DTime = float

/// Converts an input signal to an output signal.
type SignalFunction<'a, 'b> =
    SF of TransitionFunction<'a, 'b>

/// Generates an output value from an input value, and
/// updates the signal function.
and TransitionFunction<'a, 'b> =
    DTime                                  // time since previous sample
        -> 'a                              // current input
        -> (SignalFunction<'a, 'b> * 'b)   // updated signal function * current output

/// Arrow implementation.
module Arrow =

    /// Creates a signal function from a plain function.
    ///
    ///       +---+
    ///    -->| f |-->
    ///       +---+
    ///
    let rec arr f =
        SF (fun _ a -> arr f, f a)

    /// Composes two signal functions, left to right.
    ///
    ///       +-----+   +-----+
    ///    -->| sf1 |-->| sf2 |-->
    ///       +-----+   +-----+
    ///
    let rec (>>>) (SF tf1) (SF tf2) =
        SF (fun dt a ->
            let sf1', b = tf1 dt a
            let sf2', c = tf2 dt b
            (sf1' >>> sf2'), c)

    /// Composes two signal functions, right to left.
    ///
    ///       +-----+   +-----+
    ///    <--| sf2 |<--| sf1 |<--
    ///       +-----+   +-----+
    ///
    let rec (<<<) (SF tf2) (SF tf1) =
        SF (fun dt a ->
            let sf1', b = tf1 dt a
            let sf2', c = tf2 dt b
            (sf2' <<< sf1'), c)

    /// Combines two signal functions in parallel.
    ///
    ///       +-----+
    ///    -->| sf1 |-->
    ///       +-----+
    ///       +-----+
    ///    -->| sf2 |-->
    ///       +-----+
    ///    
    let rec ( ***) (SF tf1) (SF tf2) =
        SF (fun dt (a, b) ->
            let sf1', c = tf1 dt a
            let sf2', d = tf2 dt b
            sf1' *** sf2', (c, d))

    /// Composes a plain function with a signal function, left to right.
    let (^>>) f sf =
        arr f >>> sf

    /// Composes a signal function with a plain function, left to right.
    let (>>^) sf f =
        sf >>> arr f

    /// Shares an input between two signal functions.
    ///
    ///           +-----+
    ///       +-->| sf1 |-->
    ///       |   +-----+
    ///       |
    ///    -->+
    ///       |
    ///       |   +-----+
    ///       +-->| sf2 |-->
    ///           +-----+
    ///    
    let rec (&&&) sf1 sf2 =
        (fun a -> (a, a)) ^>> (sf1 *** sf2)

    ///
    /// Widens a signal function.
    ///
    ///       +----+
    ///    -->| sf |-->
    ///       +----+
    ///       
    ///    ----------->
    ///    
    let rec first sf =
        sf *** arr id

    ///
    /// Widens a signal function.
    ///
    ///    ----------->
    ///
    ///       +----+
    ///    -->| sf |-->
    ///       +----+
    ///       
    let rec second sf =
        arr id *** sf

    let arr2 f =
        let uncurry f (a, b) = f a b
        arr (uncurry f)

    let identity<'a> =
        arr id<'a>

    let constant value =
        arr (fun _ -> value)
