[<AutoOpen>]
module FSharpExtensions

open System
open System.Collections.Generic
open System.IO

[<RequireQualifiedAccess>]
module Async =
    /// Maps the result of an asynchronous computation.
    let map f (task: Async<_>) = async {
        let! x = task
        return f x }

[<RequireQualifiedAccess>]
module File =
    /// Calls the map function if the file with the given path exists.
    let tryMap f path =
        if File.Exists path
        then Some (f path)
        else None

[<RequireQualifiedAccess>]
module ResizeArray =
    let init (size: int) f =
        let a = ResizeArray(size)
        for i = 0 to size - 1 do a.Add(f i)
        a

    let inline tryLast (a: ResizeArray<_>) =
        if a.Count = 0 then
            None
        else
            Some a.[a.Count - 1]

    let inline tryHead (a: ResizeArray<_>) =
        if a.Count = 0 then
            None
        else
            Some a.[0]

[<RequireQualifiedAccess>]
module Array =
    /// Returns a new array with the item at the given index replaced with the new one.
    let updateAt (index: int) newItem array =
        let arr = Array.copy array
        arr.[index] <- newItem
        arr

    /// Returns the average of the given array, zero for an empty array.
    let tryAverage = function
        | [||] -> 0.f
        | arr -> Array.average arr

    /// Returns true if all the elements in the array are the same or the array is empty.
    let allSame (array: 'a array) =
        array.Length <= 1
        ||
        array.AsSpan(1).TrimStart(array.[0]).IsEmpty

[<RequireQualifiedAccess>]
module Dictionary =
    /// Maps the result of IReadOnlyDictionary.TryGetValue into an option.
    let tryGetValue key (dict: IReadOnlyDictionary<_,_>) =
        match dict.TryGetValue key with
        | true, value -> Some value
        | false, _ -> None

[<RequireQualifiedAccess>]
module Option =
    /// Creates an option from a string, where a null or whitespace string equals None.
    let ofString s = if String.IsNullOrWhiteSpace s then None else Some s

    /// Creates an option from an array, where null or an empty array equals None.
    let ofArray a =
        match a with
        | null | [||] -> None
        | array -> Some array
