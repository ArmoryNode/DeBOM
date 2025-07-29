// SPDX-License-Identifier: Apache-2.0
// SPDX-FileCopyrightText: Copyright (c) 2025 ArmoryNode
namespace DeBOM.Helpers

open System.Buffers

module ArrayPool =
    let useArrayFromPoolAsync (size: int64) (f: byte array -> Async<'T>) =
        async {
            let buffer = ArrayPool<byte>.Shared.Rent(int size)

            try
                return! f buffer
            finally
                ArrayPool<byte>.Shared.Return(buffer)
        }

    let useArrayFromPool size f=
        let buffer = ArrayPool<byte>.Shared.Rent(size)

        try
            f buffer
        finally
            ArrayPool<byte>.Shared.Return(buffer)
