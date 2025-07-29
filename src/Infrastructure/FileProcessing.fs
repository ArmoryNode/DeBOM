// SPDX-License-Identifier: Apache-2.0
// SPDX-FileCopyrightText: Copyright (c) 2025 ArmoryNode
module DeBOM.FileProcessing

open System
open System.IO
open DeBOM.Helpers
open DeBOM.Application

module Path =
    /// <summary>
    /// Generates a file path relative to the specified base path.
    /// </summary>
    /// <returns>
    /// A <see cref="Result"/> containing the relative path of the file from the base path or an error message if the paths are invalid.
    /// </returns>
    /// <example>
    /// "/example/path/to/file.txt" relative to "/example/path" will return "to/file.txt".
    /// </example>
    /// <param name="basePath">The base path</param>
    /// <param name="filePath">The file path</param>
    let getFileRelativePath basePath filePath =
        if String.IsNullOrWhiteSpace basePath || String.IsNullOrWhiteSpace filePath then
            Error "Base path and file name cannot be null or empty."
        else
            try
                let basePath =
                    if Directory.Exists basePath then
                        basePath
                    else
                        Path.GetDirectoryName basePath

                Path.GetRelativePath(basePath, filePath) |> Ok
            with
            | :? ArgumentException -> Error "Could not get the relative file path. One or more paths contain invalid characters."
            | :? PathTooLongException -> Error $"Provided path is too long. basePath: {basePath.Length} fileName: {filePath.Length}"
            | ex -> Error ex.Message

    /// <summary>
    /// Combines two strings into a path.
    /// </summary>
    /// <param name="basePath">The base path</param>
    /// <param name="path">The path to combine</param>
    /// <returns>
    /// A <see cref="Result"/> containing the combined path or an error message.
    /// </returns>
    /// <remarks>
    /// This is a wrapper around <see cref="Path.Combine(string, string)"/>.
    /// </remarks>
    let combinePaths path basePath =
        if String.IsNullOrWhiteSpace path || String.IsNullOrWhiteSpace basePath then
            Error "Paths cannot be null or empty."
        else
            try
                Path.Combine(basePath, path) |> Ok
            with
            | :? ArgumentException -> Error $"Could not combine paths. One or more paths contain invalid characters."
            | ex -> Error ex.Message

    /// <summary>
    /// Gets a sequence of files from the specified path that match the given pattern.
    /// </summary>
    /// <param name="options">The program options</param>
    /// <returns>A sequence of file paths relative to the base directory</returns>
    let getFiles (options: Options) =
        if String.IsNullOrWhiteSpace options.Path then
            [||]
        elif Directory.Exists options.Path then
            let searchOption =
                match options.Recursive with
                | true -> SearchOption.AllDirectories
                | false -> SearchOption.TopDirectoryOnly

            DirectoryInfo(options.Path).EnumerateFiles(options.Pattern, searchOption)
            |> Seq.map _.FullName
            |> Seq.toArray
        elif File.Exists options.Path then
            [| options.Path |]
        else
            [||]

    /// <summary>
    /// Creates a <see cref="FileInfo"/> for the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the file</param>
    /// <returns>
    /// A <see cref="Result"/> with <see cref="FileInfo"/> on success, or an error message on failure.
    /// </returns>
    let inline getFileInfo filePath =
        if String.IsNullOrWhiteSpace filePath then
            Error "File path cannot be empty"
        else
            try
                FileInfo filePath |> Ok
            with
            | :? ArgumentException -> Error "Path contains invalid characters"
            | :? UnauthorizedAccessException -> Error "Access to the file is denied"
            | :? PathTooLongException -> Error "The specified path is too long"
            | ex -> Error ex.Message

module FileSystem =
    /// <summary>
    /// Returns the full path of the specified directory, and creates it if it doesn't exist.
    /// </summary>
    /// <param name="directoryPath">The path of the directory</param>
    let inline ensureDirectoryExists directoryPath =
        try
            let fileInfo = FileInfo directoryPath

            if fileInfo.Directory.Exists then
                fileInfo.FullName |> Ok
            else
                fileInfo.Directory.Create()
                fileInfo.FullName |> Ok
        with
        | :? IOException -> Error "Could not create directory"
        | ex -> Error ex.Message

    /// <summary>
    /// Creates a copy path for the specified file in the destination directory, and ensures that the file name is unique.
    /// </summary>
    /// <param name="destinationDirectory">The directory to point the copy path in</param>
    /// <param name="filePath">The file to create a copy path for</param>
    /// <returns>
    /// A <see cref="Result"/> containing the copy path or an error message if the operation fails.
    /// </returns>
    /// <remarks>
    /// This function <b>does not</b> move or copy the file, it only generates a destination for a file to be copied to.
    /// </remarks>
    let createCopyPath destinationDirectory filePath =
        let copyFilePath =
            destinationDirectory
            |> Path.combinePaths filePath
            |> Result.bind ensureDirectoryExists
            |> Result.bind Path.getFileInfo

        match copyFilePath with
        | Error message -> Error message
        | Ok copyFileInfo ->
            if copyFileInfo.Exists then
                let uniqueFileName =
                    $"%s{copyFileInfo.Name}_{DateTime.Now:yyyyMMdd_HHmmss}%s{copyFileInfo.Extension}"

                Path.combinePaths uniqueFileName destinationDirectory
            else
                copyFileInfo.FullName |> Ok

    /// <summary>
    /// Opens a new <see cref="FileStream"/> for the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the file to open the stream for</param>
    let openStream filePath =
        async {
            match Path.getFileInfo filePath with
            | Error message -> return Failed message
            | Ok fileInfo ->
                return
                    try
                        File.Open(filePath, FileMode.Open, FileAccess.ReadWrite) |> Continue
                    with
                    | :? UnauthorizedAccessException -> Failed "Access to the file is denied"
                    | :? DirectoryNotFoundException -> Failed $"Directory {fileInfo.Directory} not found"
                    | :? FileNotFoundException -> Failed $"File {fileInfo.FullName} not found"
                    | ex -> Failed ex.Message
        }

module BOMProcessing =
    /// <summary>
    /// Checks if the provided buffer contains a UTF-8 BOM (Byte Order Mark)
    /// </summary>
    /// <param name="buffer">The byte array to check for BOM</param>
    /// <returns><see cref="true"/> if BOM is found, otherwise <see cref="false">false</see></returns>
    /// <remarks>
    /// Because <see cref="ArrayPool.Shared.Rent"/> returns an array that's <b>not guaranteed to be the exact length as the size requested</b>,
    /// the buffer is sliced to the expected BOM length before it's compared.
    /// </remarks>
    let inline hasUTF8BOM buffer =
        Constants.UTF8_BOM_MARK.AsSpan().SequenceEqual(ReadOnlySpan(buffer, 0, Constants.UTF8_BOM_MARK_LENGTH))

    /// <summary>
    /// Checks the input stream for a UTF-8 BOM
    /// </summary>
    /// <param name="input">The input stream to check</param>
    /// <returns>
    /// A <see cref="Result"/> with the <paramref name="input"/> on success, or an error message on failure.
    /// </returns>
    let checkForBOM (input: FileStream) =
        ArrayPool.useArrayFromPoolAsync Constants.UTF8_BOM_MARK_LENGTH
        <| fun buffer ->
            async {
                let! bytesRead = input.ReadAsync(buffer)

                if bytesRead = 0 then return Skipped "File is empty"
                elif hasUTF8BOM buffer then return input |> Continue
                else return Skipped "UTF-8 BOM not found"
            }

    /// <summary>
    /// Shifts remaining file content in stream at the current position to the beginning of the file, effectively removing the BOM.
    /// </summary>
    /// <param name="input">The input file stream to process</param>
    /// <returns>
    /// A <see cref="Result"/> with the <paramref name="input"/> on success, or an error message on failure.
    /// </returns>
    let shiftInPlace (input: FileStream) =
        ArrayPool.useArrayFromPoolAsync Constants.MAX_BUFFER_SIZE
        <| fun buffer ->
            async {
                let fileLength = input.Length

                let rec shiftBytes readPos writePos =
                    async {
                        if readPos < fileLength then
                            input.Position <- readPos
                            let! bytesRead = input.ReadAsync(buffer, 0, Constants.MAX_BUFFER_SIZE)

                            if bytesRead > 0 then
                                input.Position <- writePos
                                do! input.WriteAsync(buffer, 0, bytesRead)
                                return! shiftBytes (readPos + int64 bytesRead) (writePos + int64 bytesRead)
                    }

                try
                    // Shift the content to the beginning of the file
                    do! shiftBytes Constants.UTF8_BOM_MARK_LENGTH 0L
                    input.SetLength(fileLength - (int64 Constants.UTF8_BOM_MARK_LENGTH))
                    return Continue input
                with ex ->
                    return Failed ex.Message
            }

    /// <summary>
    /// Copies the remaining bytes from the input stream to the target file.
    /// </summary>
    /// <param name="input">The input stream to read the bytes from</param>
    /// <param name="targetPath">The path to the target file</param>
    /// <returns>
    /// A <see cref="Result"/> with the <paramref name="input"/> on success, or an error message on failure.
    /// </returns>
    let copyRemainingBytes targetPath (input: FileStream) =
        async {
            match Path.getFileInfo targetPath with
            | Error message -> return Failed message
            | Ok fileInfo ->
                use output = new FileStream(fileInfo.FullName, FileMode.CreateNew, FileAccess.Write)

                return!
                    ArrayPool.useArrayFromPoolAsync Constants.MAX_BUFFER_SIZE
                    <| fun buffer ->
                        async {
                            let rec copyBytes () =
                                async {
                                    let! bytesRead = input.ReadAsync(buffer, 0, Constants.MAX_BUFFER_SIZE)

                                    if bytesRead > 0 then
                                        do! output.WriteAsync(buffer, 0, bytesRead)
                                        return! copyBytes ()
                                }

                            try
                                do! copyBytes ()
                                return Continue input
                            with ex ->
                                return Failed $"Could not copy remaining bytes: %s{ex.Message}"
                        }
        }
