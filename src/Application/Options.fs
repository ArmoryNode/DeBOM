// SPDX-License-Identifier: Apache-2.0
// SPDX-FileCopyrightText: Copyright (c) 2025 ArmoryNode
namespace DeBOM.Application

open System.IO
open DeBOM

[<Struct>]
type Options =
    { OutputMode: OutputMode
      Path: string
      Recursive: bool
      Pattern: string }

    /// <summary>
    /// Creates a directory path for storing copies of the files relative to the provided path.
    /// </summary>
    /// <param name="path">The path to create a copy directory in</param>
    static member GetCopyDirectory path =
        if File.Exists path then
            Path.Join(FileInfo(path).DirectoryName, Constants.COPY_DIRECTORY_NAME) |> DirectoryInfo
        else
            Path.Join(path, Constants.COPY_DIRECTORY_NAME) |> DirectoryInfo

    static member Default =
        { Path = "./"
          OutputMode = Overwrite
          Recursive = false
          Pattern = "*" }

    /// <summary>
    /// Parses the provided command line arguments into an <see cref="Options"/> record.
    /// </summary>
    /// <param name="args">The CLI argument list</param>
    static member Parse args =
        (Options.Default, args)
        ||> List.fold (fun opts arg ->
            match arg with
            | CLIArguments.Path p -> { opts with Path = p.Trim() }
            | CLIArguments.Copy ->
                { opts with
                    OutputMode = Copy(Options.GetCopyDirectory opts.Path) }
            | CLIArguments.Recursive -> { opts with Recursive = true }
            | CLIArguments.Pattern p -> { opts with Pattern = p })

    static member TryParseArgs args =
        let opts = args |> Options.Parse

        match opts with
        | { Path = "" } -> Error "Path cannot be empty."
        | _ -> Ok opts
