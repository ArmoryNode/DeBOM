// SPDX-License-Identifier: Apache-2.0
// SPDX-FileCopyrightText: Copyright (c) 2025 ArmoryNode
namespace DeBOM.Application

open DeBOM.Application

[<Struct>]
type Summary =
    { Processed: int
      Skipped: int
      Failed: int }

    static member Empty = { Processed = 0; Skipped = 0; Failed = 0 }

    static member Print summary =
        printfn $"\nProcessed: %d{summary.Processed}, Skipped: %d{summary.Skipped}, Failed: %d{summary.Failed}"

    static member PrintResults(results: FileOpResult array) =
        (Summary.Empty, results)
        ||> Array.fold (fun acc res ->
            match res with
            | Continue _ -> { acc with Processed = acc.Processed + 1 }
            | Skipped _ -> { acc with Skipped = acc.Skipped + 1 }
            | Failed _ -> { acc with Failed = acc.Failed + 1 })
        |> Summary.Print
