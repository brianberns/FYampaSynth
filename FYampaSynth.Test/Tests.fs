namespace FYampaSynth.Test

open Microsoft.VisualStudio.TestTools.UnitTesting
open FYampaSynth
open Arrow

module Test =

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

[<TestClass>]
type SignalFunctionTest() =

    [<TestMethod>]
    member __.Arr() =
        let sf = arr (fun n -> 2 * n)
        Assert.AreEqual(
            [0; 2; 4],
            Test.run [0; 1; 2] sf)

    [<TestMethod>]
    // fsharplint:disable ReimplementsFunction
    member __.Compose() =
        let sf1 = arr (fun x -> int x)
        let sf2 = arr (fun n -> string n)
        Assert.AreEqual(
            ["0"; "1"; "1"],
            Test.run [0.5; 1.0; 1.5] (sf1 >>> sf2))
        Assert.AreEqual(
            ["0"; "1"; "1"],
            Test.run [0.5; 1.0; 1.5] (sf2 <<< sf1))

    [<TestMethod>]
    member __.Parallel() =
        let sf1 = arr (fun n -> n % 2 = 0)
        let sf2 = arr (fun n -> n % 2 = 1)
        let input1 = [3; 1; 4]
        let input2 = [3; 2; 1]
        Assert.AreEqual(
            [(false, true); (false, false); (true, true)],
            Test.run (List.zip input1 input2) (sf1 *** sf2))
        Assert.AreEqual(
            [(false, true); (false, true); (true, false)],
            Test.run input1 (sf1 &&& sf2))

    [<TestMethod>]
    // 3 + 10, 3 - 10 -> 13, -7
    // 1 + -7, 1 - -7 -> -6,  8
    // 4 +  8, 4 -  8 -> 12, -4
    member __.Loop() =
        let sf =
            arr (fun (x, y) -> x + y, x - y)
                |> loop 10
        Assert.AreEqual(
            [13; -6; 12],
            Test.run [3; 1; 4] sf)

[<TestClass>]
type EventTest() =

    [<TestMethod>]
    member __.Hold() =
        let sf = Event.hold 0
        Assert.AreEqual(
            [0; 0; 1; 1; 1; 2; 3; 3],
            Test.run [
                NoEvt
                NoEvt
                Evt 1
                NoEvt
                Evt 1
                Evt 2
                Evt 3
                NoEvt ] sf)

    [<TestMethod>]
    member __.Switch() =
        let sf =
            identity &&&
                arr (fun n ->
                    if n % 5 = 0 then Evt (n / 5)
                    else NoEvt)
        let f x = arr (fun y -> x + y)
        Assert.AreEqual(
            [8; 9; 12; 13; 14],
            Test.run
                [8; 9; 10; 11; 12]
                (Event.switch sf f))

    [<TestMethod>]
    member __.AfterEach() =
        let sf =
            Event.afterEach
                [
                    1.5, "hello"   // t = 1.5
                    1.5, "world"   // t = 3.0
                ]
        let expected =
            [
                NoEvt              // t = 0.0
                NoEvt              // t = 1.0
                Evt "hello"        // t = 2.0
                Evt "world"        // t = 3.0
                NoEvt              // 4 = 4.0
            ]
        let actual = Test.run [ 0; 0; 0; 0; 0 ] sf
        Assert.AreEqual(expected, actual)
