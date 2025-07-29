// SPDX-License-Identifier: Apache-2.0
// SPDX-FileCopyrightText: Copyright (c) 2025 ArmoryNode
namespace DeBOM.Application

open Argu

type CLIArguments =
    | [<MainCommand; ExactlyOnce>] Path of path: string
    | [<AltCommandLine("-c")>] Copy
    | [<AltCommandLine("-r")>] Recursive
    | [<AltCommandLine("-p")>] Pattern of pattern: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Path _ -> "A directory or file to process."
            | Copy -> "Stores a copy of the processed files instead of overwriting the original(s). (Default: false)"
            | Recursive -> "Search recursively in subdirectories. This option is ignored when a file path is provided. (Default: false)"
            | Pattern _ -> $"Pattern to match files against. This option is ignored when a file path is provided. (Default: *)"
