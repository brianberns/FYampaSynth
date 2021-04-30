namespace FYampaSynth.Test

open Microsoft.VisualStudio.TestTools.UnitTesting
open FYampaSynth
open Arrow

[<TestClass>]
type SignalFunctionTest() =

    /// Runs the given signal function using the given inputs.
    let run inputs sf =
        let dt = 1.0
        let rec loop inputs (SF tf) =
            match inputs with
                | head :: tail ->
                    let sf', output = tf dt head
                    output :: (loop tail sf')
                | [] -> []
        loop inputs sf

    [<TestMethod>]
    member __.Arr() =
        let sf = arr (fun n -> 2 * n)
        Assert.AreEqual(
            [0; 2; 4],
            run [0; 1; 2] sf)

    [<TestMethod>]
    // fsharplint:disable ReimplementsFunction
    member __.Compose() =
        let sf1 = arr (fun x -> int x)
        let sf2 = arr (fun n -> string n)
        Assert.AreEqual(
            ["0"; "1"; "1"],
            run [0.5; 1.0; 1.5] (sf1 >>> sf2))
        Assert.AreEqual(
            ["0"; "1"; "1"],
            run [0.5; 1.0; 1.5] (sf2 <<< sf1))
