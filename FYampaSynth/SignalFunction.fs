namespace FYampaSynth

/// http://www.cs.nott.ac.uk/~psznhn/ITU-FRP2010/LectureNotes/lecture05.pdf

type DTime = float

type SignalFunction<'a, 'b> =
    SF of TransitionFunction<'a, 'b>

and TransitionFunction<'a, 'b> =
    DTime                                  // time since previous sample
        -> 'a                              // current input
        -> (SignalFunction<'a, 'b> * 'b)   // updated signal function, current output
