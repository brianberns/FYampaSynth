namespace FYampaSynth

open Arrow

type Time = float

/// An event occurs or it doesn't.
type Event<'a> =
    | Evt of 'a
    | NoEvt

module Event =

    /// Maps the given function over an event.
    let map f = function
        | Evt a -> Evt (f a)
        | NoEvt -> NoEvt

    /// Tags an event occurrence with the given value.
    let tag value =
        map (fun _ -> value)

    /// No event ever occurs.
    let rec never =
        SF (fun _ _ -> never, NoEvt)

    /// Repeats the given value until an event occurs.
    let rec hold a =
        SF (fun _ evt ->
            match evt with
                | Evt a' -> hold a', a'
                | NoEvt -> hold a, a)

    let rec switch (SF tf) f =
        SF (fun dt a ->
            let sf', (b, evt) = tf dt a
            let sf'' =
                match evt with
                    | Evt c -> f c
                    | NoEvt -> switch sf' f
            sf'', b)

    // https://hackage.haskell.org/package/Yampa-0.13.1/docs/src/FRP.Yampa.EventS.html
    let rec afterEachCat qxs =

        let rec emitEventsScheduleNext (t : Time) xs = function
            | (q, x) :: qxs ->
                if q < 0.0 then failwith "Unexpected"
                else
                    let t' = t - q
                    if t' >= 0.0 then
                        emitEventsScheduleNext t' (x :: xs) qxs
                    else
                        awaitNextEvent t' x qxs, Evt (List.rev xs)
            | [] -> never, Evt (List.rev xs)

        and awaitNextEvent (t : Time) x qxs =
            SF (fun dt _ ->
                let t' = t + dt
                if t' >= 0.0 then
                    emitEventsScheduleNext t' [x] qxs
                else
                    awaitNextEvent t' x qxs, NoEvt)

        match qxs with
            | (q : Time, x) :: tail ->
                SF (fun _ _ ->
                    if q < 0.0 then failwith "Unexpected"
                    elif q <= 0.0 then emitEventsScheduleNext 0.0 [x] tail
                    else awaitNextEvent -q x qxs, NoEvt)
            | [] -> never

    let afterEach qxs =
        afterEachCat qxs >>> arr (map List.head)

    let after q x =
        afterEach [q, x]

    /// Suppresses an initial event.
    let notYet<'a> : SignalFunction<Event<'a>, Event<'a>> =
        initially NoEvt
