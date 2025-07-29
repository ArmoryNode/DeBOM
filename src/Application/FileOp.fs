// SPDX-License-Identifier: Apache-2.0
// SPDX-FileCopyrightText: Copyright (c) 2025 ArmoryNode
namespace DeBOM.Application

open System.IO
open BlackFox.ColoredPrintf

[<AutoOpen; Struct>]
type FileOpResult =
    | Continue of stream: FileStream
    | Skipped of reason: string
    | Failed of message: string

    member this.GetMessage fileName =
        match this with
        | Continue _ -> $"%s{fileName}...$green[Done!]" |> ColorPrintFormat
        | Skipped reason -> $"%s{fileName}...$yellow[Skipped: %s{reason}]" |> ColorPrintFormat
        | Failed error -> $"%s{fileName}...$red[Failed: %s{error}]" |> ColorPrintFormat

module FileOp =
    let bind switchFunction =
        function
        | Continue stream -> switchFunction stream
        | Skipped reason -> Skipped reason
        | Failed message -> Failed message

    let bindAsync (switchFunction: FileStream -> Async<FileOpResult>) (input: Async<FileOpResult>) =
        async {
            match! input with
            | Continue stream -> return! switchFunction stream
            | Skipped reason -> return Skipped reason
            | Failed message -> return Failed message
        }

[<AutoOpen>]
module Helpers =
    let (>>=) (input: Async<FileOpResult>) (switchFunction: FileStream -> Async<FileOpResult>) = FileOp.bindAsync switchFunction input
