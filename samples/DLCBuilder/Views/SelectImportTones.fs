﻿module DLCBuilder.Views.SelectImportTones

open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls
open Avalonia.Layout
open Rocksmith2014.Common.Manifest
open DLCBuilder

let view (state: State) dispatch (tones: Tone array) =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            TextBlock.create [
                TextBlock.fontSize 16.
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "selectImportTone")
            ]
            ListBox.create [
                ListBox.name "tonesListBox"
                ListBox.dataItems tones
                // Multiple selection mode is broken in Avalonia FuncUI
                // https://github.com/AvaloniaUI/Avalonia/issues/3497
                ListBox.selectionMode SelectionMode.Single
                ListBox.maxHeight 300.
                ListBox.onSelectedItemChanged (ImportTonesChanged >> dispatch)
            ]
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.spacing 8.
                StackPanel.children [
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (30., 10.)
                        Button.content (translate "import")
                        Button.onClick (fun _ -> state.ImportTones |> ImportTones |> dispatch)
                        Button.isDefault true
                    ]
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (20., 10.)
                        Button.content (translate "all")
                        Button.onClick (fun _ -> tones |> List.ofArray |> ImportTones |> dispatch)
                        Button.isDefault true
                    ]
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (30., 10.)
                        Button.content (translate "cancel")
                        Button.onClick (fun _ -> dispatch CloseOverlay)
                    ]
                ]
            ]
        ]
    ] :> IView
