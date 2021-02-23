﻿module DLCBuilder.Views.ToneEditor

open Avalonia.Layout
open Avalonia.Media
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open System
open Rocksmith2014.Common.Manifest
open Rocksmith2014.Common
open DLCBuilder
open ToneGear

let private toggleButton (content: string) (isChecked: bool) onChecked =
    ToggleButton.create [
        ToggleButton.margin (0., 2.)
        ToggleButton.minHeight 30.
        ToggleButton.fontSize 14.
        ToggleButton.content content
        ToggleButton.isChecked isChecked
        ToggleButton.onChecked onChecked
        ToggleButton.onClick (fun e ->
            let b = e.Source :?> ToggleButton
            b.IsChecked <- true
            e.Handled <- true
        )
    ]

let private gearTemplate =
    DataTemplateView<GearData>.create (fun gear ->
        match gear.Type with
        | "Amps" | "Cabinets" ->
            let prefix = if String.startsWith "bass" gear.Key then "(Bass) " else String.Empty
            TextBlock.create [ TextBlock.text $"{prefix}{gear.Name}" ]
        | _ ->
            TextBlock.create [ TextBlock.text $"{gear.Category}: {gear.Name}" ])

let private gearTypeHeader locName =
    TextBlock.create [
        TextBlock.fontSize 14.
        TextBlock.margin (0., 2.)
        TextBlock.text (translate locName)
      ] |> generalize

let private pedalSelectors dispatch selectedGearType tone (locName, pedalFunc) =
    [ gearTypeHeader locName

      for index in 0..3 do
        let gearType = pedalFunc index
        let content =
            match getGearDataForCurrentPedal tone gearType with
            | Some data -> data.Name
            | None -> String.Empty
        toggleButton content (gearType = selectedGearType) (fun _ -> gearType |> SetSelectedGearType |> dispatch)
        |> generalize ]

let private gearTypeSelector state dispatch (tone: Tone) =
    StackPanel.create [
        StackPanel.children [
            gearTypeHeader "amp"
            toggleButton ampDict.[tone.GearList.Amp.Key].Name
                         (state.SelectedGearType = Amp)
                         (fun _ -> Amp |> SetSelectedGearType |> dispatch)

            gearTypeHeader "cabinet"
            toggleButton (let c = cabinetDict.[tone.GearList.Cabinet.Key] in $"{c.Name} ({c.Category})")
                         (state.SelectedGearType = Cabinet)
                         (fun _ -> Cabinet |> SetSelectedGearType |> dispatch)

            yield! [ ("prePedals", PrePedal); ("loopPedals", PostPedal); ("rack", Rack) ]
                   |> List.collect (pedalSelectors dispatch state.SelectedGearType tone)
        ]
    ] 

let private gearSelector dispatch (tone: Tone) gearType =
    let gearData = getGearDataForCurrentPedal tone gearType

    ComboBox.create [
        ComboBox.virtualizationMode ItemVirtualizationMode.Simple
        ComboBox.dataItems (
            match gearType with
            | Amp -> amps
            | Cabinet -> cabinetChoices
            | PrePedal _ | PostPedal _ -> pedals
            | Rack _ -> racks)
        ComboBox.itemTemplate gearTemplate
        ComboBox.selectedItem (
            match gearData with
            | Some data when data.Type = "Cabinets" ->
                cabinetChoices
                |> Array.find (fun x -> x.Name = data.Name)
            | Some data -> data
            | None -> Unchecked.defaultof<GearData>)
        ComboBox.onSelectedItemChanged (fun item ->
            match item with
            | :? GearData as gear -> Some gear
            | _ -> None
            |> SetSelectedGear |> dispatch)
    ]

let private formatValue v (step: float) minValue unitType =
    let unit = if unitType = "number" then String.Empty else " " + unitType
    match Math.Ceiling step > step, minValue < 0.0f with
    | true, true -> sprintf "%+.1f%s" v unit
    | true, false -> sprintf "%.1f%s" v unit
    | false, true -> sprintf "%+.0f%s" v unit
    | false, false -> sprintf "%.0f%s" v unit

let private knobSliders dispatch (tone: Tone) gearType gear =
    match gear with
    // Cabinets
    | { Knobs = None } as cabinet ->
        StackPanel.create [
            StackPanel.children [
                let micPositions = micPositionsForCabinet.[cabinet.Name]
                if micPositions.Length = 1 then
                    TextBlock.create [
                        TextBlock.margin (0., 4.)
                        TextBlock.text (translate "nothingToConfigure")
                    ]
                else
                    TextBlock.create [
                        TextBlock.margin (0., 4.)
                        TextBlock.text (translate "micPosition")
                    ]
                    StackPanel.create [
                        StackPanel.children [
                            yield! micPositions
                            |> Array.map (fun cab ->
                                RadioButton.create [
                                    RadioButton.content cab.Category
                                    RadioButton.isChecked (tone.GearList.Cabinet.Key = cab.Key)
                                    // onChecked can cause an infinite update loop
                                    RadioButton.onClick ((fun _ ->
                                        cab |> SetPedal |> EditTone |> dispatch
                                    ), SubPatchOptions.Always)
                                ] |> generalize
                            )
                        ]
                    ]
            ]
        ]
        |> generalize
        |> Array.singleton
    // Everything else
    | { Knobs = Some knobs } ->
        knobs
        |> Array.mapi (fun i knob ->
            let currentValue =
                getKnobValuesForGear tone gearType
                |> Option.bind (Map.tryFind knob.Key)
                |> Option.defaultValue knob.DefaultValue

            let bg = if i % 2 = 0 then SolidColorBrush.Parse "#303030" else SolidColorBrush.Parse "#383838"

            StackPanel.create [
                StackPanel.background bg
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text knob.Name
                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                    ]

                    match knob.EnumValues with
                    | Some enums ->
                        ComboBox.create [
                            ComboBox.dataItems enums
                            ComboBox.selectedIndex (int currentValue)
                            ComboBox.onSelectedIndexChanged ((fun index ->
                                SetKnobValue (knob.Key, float32 index) |> EditTone |> dispatch),
                                SubPatchOptions.Always
                            )
                        ]
                    | None ->
                        TextBlock.create [
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                            TextBlock.text (formatValue currentValue (float knob.ValueStep) knob.MinValue knob.UnitType)
                        ]

                        Grid.create [
                            Grid.margin (6., -15., 6., 0.)
                            Grid.columnDefinitions "auto,*,auto"
                            Grid.children [
                                TextBlock.create [
                                    TextBlock.text (string knob.MinValue)
                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                ]
                                TextBlock.create [
                                    Grid.column 2
                                    TextBlock.text (string knob.MaxValue)
                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                ]
                                Slider.create [
                                    Grid.column 1
                                    Slider.margin (4., 0.)
                                    Slider.isSnapToTickEnabled true
                                    Slider.tickFrequency (float knob.ValueStep)
                                    Slider.smallChange (float knob.ValueStep)
                                    Slider.maximum (float knob.MaxValue)
                                    Slider.minimum (float knob.MinValue)
                                    Slider.value (float currentValue)
                                    Slider.onValueChanged ((fun value ->
                                        SetKnobValue (knob.Key, float32 value) |> EditTone |> dispatch),
                                        SubPatchOptions.Always
                                    )
                                ]
                            ]
                        ]
                ]
            ] |> generalize)

let view state dispatch tone =
    Grid.create [
        Grid.width 620.
        Grid.minHeight 660.
        Grid.columnDefinitions "*,*"
        Grid.rowDefinitions "*,auto"
        Grid.children [
            gearTypeSelector state dispatch tone

            StackPanel.create [
                Grid.column 1
                StackPanel.margin (16., 0., 0., 0.)
                StackPanel.children [
                    yield gearSelector dispatch tone state.SelectedGearType

                    match state.SelectedGear with
                    | Some gear ->
                        yield (
                            Button.create [
                                Button.margin (0., 2.)
                                Button.content (translate "remove")
                                Button.isVisible (match state.SelectedGearType with Amp | Cabinet -> false | _ -> true)
                                Button.onClick (fun _ -> RemovePedal |> EditTone |> dispatch)
                            ])
                        yield! knobSliders dispatch tone state.SelectedGearType gear
                    | None ->
                        ()
                ]
            ]

            Button.create [
                Grid.row 1
                Grid.columnSpan 2
                Button.margin 4.
                Button.fontSize 16.
                Button.padding (50., 10.)
                Button.content (translate "close")
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.onClick (fun _ -> CloseOverlay |> dispatch)
            ]
        ]
    ] |> generalize
