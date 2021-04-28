namespace FYampaSynth

type Event<'a> =
    | Evt of 'a
    | NoEvt

module Event =

    let tag evt value =
        match evt with
            | Evt _ -> Evt value
            | NoEvt -> NoEvt

    let rec switch (SF tf) f =
        SF (fun dt a ->
            let sf', (b, evt) = tf dt a
            let sf'' =
                match evt with
                    | Evt c -> f c
                    | NoEvt -> switch sf' f
            sf'', b)
