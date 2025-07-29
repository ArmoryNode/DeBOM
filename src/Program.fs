// SPDX-License-Identifier: Apache-2.0
// SPDX-FileCopyrightText: Copyright (c) 2025 ArmoryNode
module DeBOM.Program

open BlackFox.ColoredPrintf
open DeBOM.Application
open DeBOM.FileProcessing
open DeBOM.Helpers
open System.IO
open System.Threading

let semaphore = new SemaphoreSlim(1)

let safePrintResult filePath (result: FileOpResult) =
    async {
        do! semaphore.WaitAsync()

        try
            colorprintfn (filePath |> result.GetMessage)
        finally
            semaphore.Release() |> ignore
    }
    |> Async.StartImmediate

let overwriteFileInPlace stream =
    stream |> BOMProcessing.checkForBOM >>= BOMProcessing.shiftInPlace

let copyFileToNewLocation baseDir targetDirectory (stream: FileStream) =
    async {
        let copyPath =
            Path.getFileRelativePath baseDir stream.Name
            |> Result.bind (FileSystem.createCopyPath targetDirectory)

        match copyPath with
        | Error message -> return Failed message
        | Ok copyPath ->
            return!
                stream
                |> BOMProcessing.checkForBOM
                >>= BOMProcessing.copyRemainingBytes copyPath
    }

let processFile options filePath =
    async {
        match! FileSystem.openStream filePath with
        | Failed message -> return Failed message
        | Skipped reason -> return Skipped reason
        | Continue stream ->
            use stream = stream

            let! result =
                match options.OutputMode with
                | Overwrite -> stream |> overwriteFileInPlace
                | Copy targetDir -> stream |> copyFileToNewLocation options.Path targetDir.FullName

            do safePrintResult filePath result
            return result
    }

let runApplication options =
    async {
        let! results = Path.getFiles options |> Array.map (processFile options) |> Async.Parallel

        // Wait for all results to be printed before showing the summary
        do! semaphore.WaitAsync()

        results |> Summary.PrintResults

        match options.OutputMode with
        | Overwrite -> ()
        | Copy targetDirectory -> colorprintfn $"\n$blue[Wrote files to {targetDirectory}]"

        return 0
    }

[<EntryPoint>]
let main args =
    try
        let parsedOptions =
            args |> ArguHelpers.parseCommandLineArgs |> Result.bind Options.TryParseArgs

        match parsedOptions with
        | Ok options -> runApplication options |> Async.RunSynchronously
        | Error message ->
            colorprintfn $"\n$red[Error: {message}]\n"
            1
    finally
        semaphore.Dispose()
