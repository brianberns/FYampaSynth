namespace MoogDemo

open System
open System.Threading
open System.Windows.Forms

module Program =

    /// Handles an exception.
    let handleException (ex : exn) =
        let text =
            $"{ex.Message}\r\n\r\n{ex.StackTrace}"
        MessageBox.Show(text, ex.GetType().Name) |> ignore

    /// A thread exception has occurred.
    let onThreadException (args : ThreadExceptionEventArgs) =
        args.Exception
            |> handleException

    /// An unhandled exception has occurred.
    let onUnhandledException (args : UnhandledExceptionEventArgs) =
        args.ExceptionObject
            :?> exn
            |> handleException

    [<EntryPoint; STAThread>]
    let main argv =

        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(false)
        Application.SetHighDpiMode(HighDpiMode.DpiUnawareGdiScaled) |> ignore
        Application.ThreadException.Add(onThreadException)
        AppDomain.CurrentDomain.UnhandledException.Add(onUnhandledException)
        Application.Run(new MainForm())

        0
