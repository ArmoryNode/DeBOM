// SPDX-License-Identifier: Apache-2.0
// SPDX-FileCopyrightText: Copyright (c) 2025 ArmoryNode
module DeBOM.Constants

let LOADER = [| '\\'; '|'; '/'; '-' |]
let UTF8_BOM_MARK = [| 0xEFuy; 0xBBuy; 0xBFuy |]

[<Literal>]
let MAX_BUFFER_SIZE = 4096 // 4KB buffer size
[<Literal>]
let UTF8_BOM_MARK_LENGTH = 3 // Length of the UTF-8 BOM bytes
[<Literal>]
let COPY_DIRECTORY_NAME = "DeBOM Copied" // Name of the directory where files will be copied
