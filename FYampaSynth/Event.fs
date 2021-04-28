namespace FYampaSynth

open Arrow

type Time = float

type Event<'a> =
    | Evt of 'a
    | NoEvt

module Event =

    let map f = function
        | Evt a -> Evt (f a)
        | NoEvt -> NoEvt

    let tag value =
        map (fun _ -> value)

    let rec never =
        SF (fun _ _ -> never, NoEvt)

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

    let rec afterEachCat (qxs : List<Time * 'b>) : SignalFunction<'a, Event<List<'b>>> =

        let rec emitEventsScheduleNext t xs = function
            | (q, x) :: qxs ->
                if q < 0.0 then failwith "Unexpected"
                else
                    let t' = t - q
                    if t' >= 0.0 then
                        emitEventsScheduleNext t' (x :: xs) qxs
                    else
                        awaitNextEvent t' x qxs, Evt (List.rev xs)
            | [] -> never, Evt (List.rev xs)

        and awaitNextEvent t x qxs =
            SF (fun dt _ ->
                let t' = t + dt
                if t' >= 0.0 then
                    emitEventsScheduleNext t' [x] qxs
                else
                    awaitNextEvent t' x qxs, NoEvt)

        match qxs with
            | (q, x) :: tail ->
                SF (fun _ _ ->
                    if q < 0.0 then failwith "Unexpected"
                    elif q <= 0.0 then emitEventsScheduleNext 0.0 [x] tail
                    else awaitNextEvent -q x qxs, NoEvt)
            | [] -> never

    let afterEach (qxs : List<Time * 'b>) : SignalFunction<'a, Event<'b>> =
        afterEachCat qxs >>> arr (map List.head)
