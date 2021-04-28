namespace FYampaSynth

open Arrow

type Time = float

type Event<'a> =
    | Evt of 'a
    | NoEvt

module Event =

    let tag evt value =
        match evt with
            | Evt _ -> Evt value
            | NoEvt -> NoEvt

    let rec never =
        SF (fun _ _ -> never, NoEvt)

    let rec switch (SF tf) f =
        SF (fun dt a ->
            let sf', (b, evt) = tf dt a
            let sf'' =
                match evt with
                    | Evt c -> f c
                    | NoEvt -> switch sf' f
            sf'', b)

    let rec afterEachCat qxs =

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
            SF (fun dt a ->
                let t' = t + dt
                if t' >= 0.0 then
                    emitEventsScheduleNext t' [x] qxs
                else
                    awaitNextEvent t' x qxs, NoEvt)

        match qxs with
            | (q, x) :: tail ->
                if q < 0.0 then failwith "Unexpected"
                elif q = 0.0 then emitEventsScheduleNext 0.0 [x] tail
                else awaitNextEvent (-q) qxs, NoEvt
            | [] -> never

    let afterEach qxs =
        afterEachCat qxs >>> arr List.head

