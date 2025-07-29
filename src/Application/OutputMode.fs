// SPDX-License-Identifier: Apache-2.0
// SPDX-FileCopyrightText: Copyright (c) 2025 ArmoryNode
namespace DeBOM.Application

open System.IO

type OutputMode =
    | Overwrite
    | Copy of destination: DirectoryInfo
