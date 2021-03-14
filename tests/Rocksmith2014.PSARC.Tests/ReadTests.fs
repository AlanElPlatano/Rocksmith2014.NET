﻿module ReadTests

open Expecto
open System.IO
open Rocksmith2014.PSARC

[<Tests>]
let readTests =
    testList "Read and Extract Files" [
        testCase "Can read PSARC with encrypted TOC" <| fun _ ->
            use file = File.OpenRead "test_p.psarc"
            use psarc = PSARC.Read file
            Expect.equal psarc.Manifest.[0] "gfxassets/album_art/album_testtest_64.dds" "First file name is correct"
        
        testAsync "Can extract all files from PSARC" {
            use file = File.OpenRead "test_p.psarc"
            use psarc = PSARC.Read file
            let tempPath = Path.Combine(Path.GetTempPath(), "extractTest")
            Directory.CreateDirectory tempPath |> ignore
            
            do! psarc.ExtractFiles tempPath
        
            let fileCount = Directory.EnumerateFiles(tempPath, "*.*", SearchOption.AllDirectories) |> Seq.length
            Expect.equal fileCount psarc.TOC.Count "All files were extracted"
            Directory.Delete(tempPath, true) }
    ]
