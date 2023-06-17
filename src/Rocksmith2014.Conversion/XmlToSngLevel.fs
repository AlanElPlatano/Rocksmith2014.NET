module Rocksmith2014.Conversion.XmlToSngLevel

open Rocksmith2014
open Rocksmith2014.Conversion.XmlToSng
open Rocksmith2014.Conversion.Utils
open Rocksmith2014.SNG
open Rocksmith2014.XML.Extension
open XmlToSngNote

/// Converts an XML level into an SNG level.
let convertLevel (accuData: AccuData) (piTimes: int array) (xmlArr: XML.InstrumentalArrangement) (xmlLevel: XML.Level) =
    let difficulty = int xmlLevel.Difficulty
    let xmlEntities = createXmlEntityArrayFromLevel xmlLevel
    let noteTimes = Array.map getTimeCode xmlEntities
    let isArpeggio (fp: FingerPrint) = xmlArr.ChordTemplates[int fp.ChordId].IsArpeggio

    let arpeggios, handShapes =
        xmlLevel.HandShapes
        |> mapToArray (convertHandshape noteTimes xmlEntities)
        |> Array.partition isArpeggio
    let fingerPrints = [| handShapes; arpeggios |]

    let convertNote' =
        convertNote noteTimes piTimes fingerPrints accuData NoteFlagFunctions.onAnchorChange xmlArr difficulty

    let notes = xmlEntities |> Array.mapi convertNote'

    let anchors =
        xmlLevel.Anchors
        |> mapiToArray (convertAnchor notes noteTimes xmlLevel xmlArr)

    let averageNotes =
        let phraseIterationNotes =
            accuData.NotesInPhraseIterationsAll[difficulty]

        Array.init xmlArr.Phrases.Count (fun phraseId ->
            phraseIterationNotes
            |> Array.choosei (fun piIndex numNotes ->
                if xmlArr.PhraseIterations[piIndex].PhraseId = phraseId then
                    Some (float32 numNotes)
                else
                    None)
            |> Array.tryAverage)

    { Difficulty = difficulty
      Anchors = anchors
      AnchorExtensions = accuData.AnchorExtensions[difficulty].ToArray()
      HandShapes = handShapes
      Arpeggios = arpeggios
      Notes = notes
      AverageNotesPerIteration = averageNotes
      NotesInPhraseIterationsExclIgnored = accuData.NotesInPhraseIterationsExclIgnored[difficulty]
      NotesInPhraseIterationsAll = accuData.NotesInPhraseIterationsAll[difficulty] }
