// SPDX-License-Identifier: Apache-2.0
// SPDX-FileCopyrightText: Copyright (c) 2025 ArmoryNode
module DeBOM.Helpers.ArguHelpers

open Argu
open System

let errorHandler =
    ProcessExiter(
        colorizer =
            function
            | ErrorCode.HelpText -> None
            | _ -> Some ConsoleColor.Red
    )

let parseCommandLineArgs<'a when 'a :> IArgParserTemplate> args =
    let parser =
        ArgumentParser.Create<'a>(programName = "DeBOM", errorHandler = errorHandler)

    try
        parser.ParseCommandLine(args).GetAllResults() |> Ok
    with ex ->
        printfn $"%s{parser.PrintUsage()}"
        Error ex.Message
