namespace FYampaSynth

/// http://www.cs.nott.ac.uk/~psznhn/ITU-FRP2010/LectureNotes/lecture05.pdf

/// Delta time (i.e. duration).
type DTime = float

/// Converts an input signal to an output signal.
type SignalFunction<'a, 'b> =
    SF of TransitionFunction<'a, 'b>

/// Generates an output value from an input value, and updates the
/// signal function.
and TransitionFunction<'a, 'b> =
    DTime                                  // time since previous sample
        -> 'a                              // current input
        -> (SignalFunction<'a, 'b> * 'b)   // updated signal function * current output

/// Arrow implementation.
// https://wiki.haskell.org/Arrow_tutorial
module Arrow =

    /// Creates a signal function from a plain function.
    //
    //       ┌───────────┐
    //       │   ╭───╮   │
    //    ───┼──►│ f │───┼──►
    //       │   ╰───╯   │
    //       └───────────┘
    //
    let rec arr f =
        SF (fun _ a -> arr f, f a)

    /// Composes two signal functions, left to right.
    //
    //       ┌───────────────────────┐
    //       │   ┌─────┐   ┌─────┐   │
    //    ───┼──►│ sf1 │──►│ sf2 │───┼──►
    //       │   └─────┘   └─────┘   │
    //       └───────────────────────┘
    //
    let rec (>>>) (SF tf1) (SF tf2) =
        SF (fun dt a ->
            let sf1', b = tf1 dt a
            let sf2', c = tf2 dt b
            (sf1' >>> sf2'), c)

    /// Composes two signal functions, right to left.
    //
    //       ┌───────────────────────┐
    //       │   ┌─────┐   ┌─────┐   │
    //    ◄──┼───│ sf2 │◄──│ sf1 │◄──┼───
    //       │   └─────┘   └─────┘   │
    //       └───────────────────────┘
    //
    let rec (<<<) (SF tf2) (SF tf1) =
        SF (fun dt a ->
            let sf1', b = tf1 dt a
            let sf2', c = tf2 dt b
            (sf2' <<< sf1'), c)

    /// Combines two signal functions in parallel.
    //
    //       ┌─────────────┐
    //       │   ┌─────┐   │
    //    ───┼──►│ sf1 │───┼──►
    //       │   └─────┘   │
    //       │   ┌─────┐   │
    //    ───┼──►│ sf2 │───┼──►
    //       │   └─────┘   │
    //       └─────────────┘
    //    
    let rec ( ***) (SF tf1) (SF tf2) =
        SF (fun dt (a, b) ->
            let sf1', c = tf1 dt a
            let sf2', d = tf2 dt b
            sf1' *** sf2', (c, d))

    /// Composes a plain function with a signal function, left to right.
    //
    //       ┌────────────────────┐
    //       │   ╭───╮   ┌────┐   │
    //    ───┼──►│ f │──►│ sf │───┼──►
    //       │   ╰───╯   └────┘   │
    //       └────────────────────┘
    //
    let (^>>) f sf =
        arr f >>> sf

    /// Composes a signal function with a plain function, left to right.
    //
    //       ┌────────────────────┐
    //       │   ┌────┐   ╭───╮   │
    //    ───┼──►│ sf │──►│ f │───┼──►
    //       │   └────┘   ╰───╯   │
    //       └────────────────────┘
    //
    let (>>^) sf f =
        sf >>> arr f

    /// Splits a signal into two streams.
    //
    //       ┌───────┐
    //       │       │
    //       │   ┌───┼──►
    //       │   │   │
    //    ───┼───┤   │
    //       │   │   │
    //       │   └───┼──►
    //       │       │
    //       └───────┘
    //    
    let split<'a> =
        arr (fun (a : 'a) -> (a, a))

    /// Shares a single input between two signal functions.
    //
    //       ┌─────────────────┐
    //       │       ┌─────┐   │
    //       │   ┌──►│ sf1 │───┼──►
    //       │   │   └─────┘   │
    //    ───┼───┤             │
    //       │   │   ┌─────┐   │
    //       │   └──►│ sf2 │───┼──►
    //       │       └─────┘   │
    //       └─────────────────┘
    //    
    let rec (&&&) sf1 sf2 =
        split >>> (sf1 *** sf2)

    /// Identity arrow: output is same as input.
    let identity<'a> =
        arr id<'a>

    ///
    /// Widens a signal function.
    //
    //       ┌────────────┐
    //       │   ┌────┐   │
    //    ───┼──►│ sf │───┼──►
    //       │   └────┘   │
    //    ───┼────────────┼──►
    //       │            │
    //       └────────────┘
    //    
    let rec first sf =
        sf *** identity

    ///
    /// Widens a signal function.
    //
    //       ┌────────────┐
    //       │            │
    //    ───┼────────────┼──►
    //       │   ┌────┐   │
    //    ───┼──►│ sf │───┼──►
    //       │   └────┘   │
    //       └────────────┘
    //       
    let rec second sf =
        identity *** sf

    /// Arrowizes a function of two arguments.
    //
    //       ┌───────────┐
    //       │   ╭───╮   │
    //    ───┼──►│   │   │
    //       │   │ f │───┼──►
    //    ───┼──►│   │   │
    //       │   ╰───╯   │
    //       └───────────┘
    //    
    let arr2 f =
        let uncurry f (a, b) = f a b
        arr (uncurry f)

    /// Feeds the given signal function back into itself, offset
    /// by one time step, filling the initial time step with the
    /// given value.
    //
    //       ┌────────────────┐
    //       │     ┌────┐     │
    //    ───┼────►│ sf │─────┼──►
    //       │ ╭──►│    │───╮ │
    //       │ │   └────┘   │ │
    //       │ ╰────────────╯ │
    //       └────────────────┘
    //    
    let loop fill sf =
        let rec loop' (SF tf) c =
            SF (fun dt a ->
                let sf', (b, c) = tf dt (a, c)
                loop' sf' c, b)
        loop' sf fill

    /// Constant arrow: ignores input.
    let constant value =
        arr (fun _ -> value)

    /// Delays a signal by one time step, filling the initial
    /// time step with the given value.
    let delay fill =
        let rec loop prev =
            SF (fun _ cur -> loop cur, prev)
        loop fill

    /// Outputs the given value now, and then behaves like the
    /// given signal function.
    let (-->) b sf =
        SF (fun _ _ -> sf, b)

    /// Overrides the initial value of the input signal.
    let initially a =
        a --> identity
