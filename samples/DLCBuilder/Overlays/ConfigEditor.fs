module DLCBuilder.Views.ConfigEditor

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open Rocksmith2014.DD
open System
open System.IO
open System.Text.RegularExpressions
open DLCBuilder

let private tryFindWwiseExecutable basePath =
    let ext = PlatformSpecific.Value(mac = "sh", windows = "exe", linux = "exe")
    Directory.EnumerateFiles(basePath, $"WwiseConsole.{ext}", SearchOption.AllDirectories)
    |> Seq.tryHead

let private generalConfig state dispatch =
    vStack [
        Grid.create [
            Grid.columnDefinitions "auto,5,*"
            Grid.rowDefinitions "auto,auto,auto,auto,auto"
            Grid.children [
                // Language
                locText "Language" [
                    TextBlock.verticalAlignment VerticalAlignment.Center
                ]
                FixedComboBox.create [
                    Grid.column 2
                    ComboBox.verticalAlignment VerticalAlignment.Center
                    ComboBox.dataItems Locales.All
                    FixedComboBox.selectedItem state.Config.Locale
                    FixedComboBox.onSelectedItemChanged (function
                        | :? Locale as locale ->
                            locale |> ChangeLocale |> dispatch
                        | _ ->
                            ())
                ]

                // Charter Name
                locText "CharterName" [
                    Grid.row 1
                    TextBlock.verticalAlignment VerticalAlignment.Center
                ]
                DockPanel.create [
                    Grid.column 2
                    Grid.row 1
                    DockPanel.children [
                        HelpButton.create [
                            DockPanel.dock Dock.Right
                            HelpButton.margin (0., 4.)
                            HelpButton.helpText (translate "CharterNameHelp")
                        ]

                        FixedTextBox.create [
                            TextBox.margin (0., 4.)
                            FixedTextBox.text state.Config.CharterName
                            FixedTextBox.onTextChanged (SetCharterName >> EditConfig >> dispatch)
                        ]
                    ]
                ]
            ]
        ]

        // Calculate Volumes Automatically
        CheckBox.create [
            CheckBox.content (translate "CalculateVolumesAutomatically")
            CheckBox.isChecked state.Config.AutoVolume
            CheckBox.onChecked (fun _ -> true |> SetAutoVolume |> EditConfig |> dispatch)
            CheckBox.onUnchecked (fun _ -> false |> SetAutoVolume |> EditConfig |> dispatch)
        ]

        // Load Previously Opened Project Automatically
        CheckBox.create [
            CheckBox.content (translate "LoadPreviousProjectAutomatically")
            CheckBox.isChecked state.Config.LoadPreviousOpenedProject
            CheckBox.onChecked (fun _ -> true |> SetLoadPreviousProject |> EditConfig |> dispatch)
            CheckBox.onUnchecked (fun _ -> false |> SetLoadPreviousProject |> EditConfig |> dispatch)
        ]

        // Auto Save
        CheckBox.create [
            CheckBox.content (translate "AutoSaveProject")
            CheckBox.isChecked state.Config.AutoSave
            CheckBox.onChecked (fun _ -> true |> SetAutoSave |> EditConfig |> dispatch)
            CheckBox.onUnchecked (fun _ -> false |> SetAutoSave |> EditConfig |> dispatch)
        ]

        // Show Advanced Features
        CheckBox.create [
            CheckBox.content (translate "ShowAdvancedFeatures")
            CheckBox.isChecked state.Config.ShowAdvanced
            CheckBox.onChecked (fun _ -> true |> SetShowAdvanced |> EditConfig |> dispatch)
            CheckBox.onUnchecked (fun _ -> false |> SetShowAdvanced |> EditConfig |> dispatch)
        ]
    ]

let private headerWithLine (locString: string) =
    DockPanel.create [
        DockPanel.margin (0., 8., 0., 2.)
        DockPanel.children [
            TextBlock.create [
                DockPanel.dock Dock.Left
                TextBlock.fontSize 16.
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate locString)
            ]

            Rectangle.create [
                Rectangle.height 1.
                Rectangle.fill Brushes.Gray
                Rectangle.margin (8., 0.)
            ]
        ]
    ]

let private pathsConfig state dispatch focusedSetting =
    vStack [
        headerWithLine "ProfilePath"

        DockPanel.create [
            Grid.column 2
            Grid.row 2
            DockPanel.children [
                Button.create [
                    DockPanel.dock Dock.Right
                    Button.margin (0., 4.)
                    Button.content "..."
                    Button.onClick (fun _ -> Dialog.ProfileFile |> ShowDialog |> dispatch)
                ]

                FixedTextBox.create [
                    TextBox.margin (0., 4.)
                    FixedTextBox.validation (fun path ->
                        if String.IsNullOrEmpty(path) then
                            true
                        else
                            String.endsWith "_PRFLDB" path)
                    FixedTextBox.validationErrorMessage (translate "ProfileFilenameValidationError")
                    FixedTextBox.text state.Config.ProfilePath
                    FixedTextBox.onTextChanged (SetProfilePath >> EditConfig >> dispatch)
                    FixedTextBox.watermark (translate "ProfilePathPlaceholder")
                    FixedTextBox.autoFocus (Option.contains FocusedSetting.ProfilePath focusedSetting)
                ]
            ]
        ]

        headerWithLine "TestFolder"

        DockPanel.create [
            Grid.column 2
            Grid.row 3
            DockPanel.children [
                Button.create [
                    DockPanel.dock Dock.Right
                    Button.margin (0., 4.)
                    Button.content "..."
                    Button.onClick (fun _ -> Dialog.TestFolder |> ShowDialog |> dispatch)
                ]
                FixedTextBox.create [
                    TextBox.margin (0., 4.)
                    FixedTextBox.text state.Config.TestFolderPath
                    TextBox.watermark (translate "TestFolderPlaceholder")
                    FixedTextBox.onTextChanged (SetTestFolderPath >> EditConfig >> dispatch)
                    FixedTextBox.autoFocus (Option.contains FocusedSetting.TestFolder focusedSetting)
                ]
            ]
        ]

        headerWithLine "WwiseConsolePath"

        DockPanel.create [
            Grid.column 2
            Grid.row 4
            DockPanel.children [
                Button.create [
                    DockPanel.dock Dock.Right
                    Button.margin (0., 4.)
                    Button.content "..."
                    Button.onClick (fun _ -> Dialog.WwiseConsole |> ShowDialog |> dispatch)
                ]
                FixedTextBox.create [
                    TextBox.margin (0., 4.)
                    TextBox.watermark (translate "WwiseConsolePathPlaceholder")
                    FixedTextBox.text (Option.toObj state.Config.WwiseConsolePath)
                    FixedTextBox.onTextChanged (SetWwiseConsolePath >> EditConfig >> dispatch)
                    TextBox.onLostFocus (fun e ->
                        let t = e.Source :?> TextBox
                        if Directory.Exists(t.Text) then
                            tryFindWwiseExecutable t.Text
                            |> Option.iter (SetWwiseConsolePath >> EditConfig >> dispatch))
                    ToolTip.tip (translate "WwiseConsolePathToolTip")
                ]
            ]
        ]
    ]

let private importConfig state dispatch =
    let localize conv =
        match conv with
        | None -> "NoConversion"
        | Some ToOgg -> "ToOggFiles"
        | Some ToWav -> "ToWaveFiles"
        |> translate

    vStack [
        // Header
        headerWithLine "PSARCImportHeader"

        StackPanel.create [
            StackPanel.margin (8., 4.)
            StackPanel.children [
                hStack [
                    // Convert Audio Options
                    locText "ConvertWemOnImport" [
                        TextBlock.margin (0., 0., 8., 0.)
                        TextBlock.verticalAlignment VerticalAlignment.Center
                    ]
                    vStack [
                        yield! [ None; Some ToOgg; Some ToWav ]
                        |> List.map (fun conv ->
                            RadioButton.create [
                                RadioButton.isChecked (state.Config.ConvertAudio = conv)
                                RadioButton.content (localize conv)
                                RadioButton.onChecked (fun _ -> conv |> SetConvertAudio |> EditConfig |> dispatch)
                            ] |> generalize)
                    ]
                ]

                // Remove DD Levels
                CheckBox.create [
                    CheckBox.content (translate "RemoveDDLevels")
                    CheckBox.isChecked state.Config.RemoveDDOnImport
                    CheckBox.onChecked (fun _ -> true |> SetRemoveDDOnImport |> EditConfig |> dispatch)
                    CheckBox.onUnchecked (fun _ -> false |> SetRemoveDDOnImport |> EditConfig |> dispatch)
                ]

                // Create EOF Project
                CheckBox.create [
                    CheckBox.content (translate "CreateEOFProject")
                    CheckBox.isChecked state.Config.CreateEOFProjectOnImport
                    CheckBox.onChecked (fun _ -> true |> SetCreateEOFProjectOnImport |> EditConfig |> dispatch)
                    CheckBox.onUnchecked (fun _ -> false |> SetCreateEOFProjectOnImport |> EditConfig |> dispatch)
                ]
            ]
        ]
    ]

let private ddConfig state dispatch =
    vStack [
        // Find Similar Phrases
        DockPanel.create [
            DockPanel.margin (0., 2.)
            DockPanel.children [
                CheckBox.create [
                    DockPanel.dock Dock.Left
                    CheckBox.fontSize 16.
                    CheckBox.verticalAlignment VerticalAlignment.Center
                    CheckBox.content (translate "FindSimilarPhrases")
                    CheckBox.isChecked state.Config.DDPhraseSearchEnabled
                    CheckBox.onChecked (fun _ -> true |> SetDDPhraseSearchEnabled |> EditConfig |> dispatch)
                    CheckBox.onUnchecked (fun _ -> false |> SetDDPhraseSearchEnabled |> EditConfig |> dispatch)
                ]

                Rectangle.create [
                    Rectangle.height 1.
                    Rectangle.fill Brushes.Gray
                    Rectangle.margin (8., 0.)
                ]
            ]
        ]

        // Similarity Threshold
        StackPanel.create [
            StackPanel.orientation Orientation.Horizontal
            StackPanel.margin (8., 4.)
            StackPanel.isEnabled state.Config.DDPhraseSearchEnabled
            StackPanel.children [
                locText "SimilarityThreshold" [
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    TextBlock.isEnabled state.Config.DDPhraseSearchEnabled
                    TextBlock.fontSize 16.
                ]
                FixedNumericUpDown.create [
                    NumericUpDown.margin (6., 2.)
                    NumericUpDown.width 140.
                    NumericUpDown.minimum 0.
                    NumericUpDown.maximum 100.
                    NumericUpDown.formatString "F0"
                    FixedNumericUpDown.value (float state.Config.DDPhraseSearchThreshold)
                    FixedNumericUpDown.onValueChanged (int >> SetDDPhraseSearchThreshold >> EditConfig >> dispatch)
                ]
                TextBlock.create [
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    TextBlock.fontSize 16.
                    TextBlock.text "%"
                ]
                HelpButton.create [
                    HelpButton.helpText (translate "DDPhraseSimilarityHelp")
                ]
            ]
        ]

        // Level Count Generation
        headerWithLine "PhraseLevelCountGeneration"

        vStack (
            [ LevelCountGeneration.Simple; LevelCountGeneration.MLModel ]
            |> List.map (fun option ->
                RadioButton.create [
                    RadioButton.verticalAlignment VerticalAlignment.Center
                    RadioButton.margin (8., 2.)
                    RadioButton.content (
                        StackPanel.create [
                            StackPanel.children [
                                locText (string option) [
                                    TextBlock.fontSize 16.
                                    TextBlock.padding (0., 4., 0., 0.)
                                    TextBlock.margin 0.
                                ]
                                locText $"{option}Help" [
                                    TextBlock.padding (5., 5.)
                                    TextBlock.textWrapping TextWrapping.Wrap
                                ]
                            ]
                        ])
                    RadioButton.isChecked (state.Config.DDLevelCountGeneration = option)
                    RadioButton.onClick (fun _ ->
                        option |> SetDDLevelCountGeneration |> EditConfig |> dispatch)
                ] |> generalize)
        )
    ]

let private buildConfig state dispatch =
    vStack [
        // Common Options
        headerWithLine "Common"

        hStack [
            // Apply Improvements
            CheckBox.create [
                CheckBox.verticalAlignment VerticalAlignment.Center
                CheckBox.margin (8., 2., 4., 2.)
                CheckBox.content (translate "ApplyImprovements")
                CheckBox.isChecked state.Config.ApplyImprovements
                CheckBox.onChecked (fun _ -> true |> SetApplyImprovements |> EditConfig |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> SetApplyImprovements |> EditConfig |> dispatch)
            ]
            HelpButton.create [
                HelpButton.helpText (translate "ApplyImprovementsHelp")
            ]
        ]

        // Release Build Options
        headerWithLine "Release"

        StackPanel.create [
            StackPanel.margin (8., 2.)
            StackPanel.children [
                hStack [
                    // Release Platforms
                    locText "Platforms" [
                        TextBlock.margin (0., 0., 10., 0.)
                        TextBlock.verticalAlignment VerticalAlignment.Center
                    ]
                    CheckBox.create [
                        CheckBox.margin 2.
                        CheckBox.minWidth 0.
                        CheckBox.content "PC"
                        CheckBox.isEnabled (state.Config.ReleasePlatforms |> Set.contains Mac)
                        CheckBox.isChecked (state.Config.ReleasePlatforms |> Set.contains PC)
                        CheckBox.onChecked (fun _ -> PC |> AddReleasePlatform |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> PC |> RemoveReleasePlatform |> EditConfig |> dispatch)
                    ]
                    CheckBox.create [
                        CheckBox.margin 2.
                        CheckBox.minWidth 0.
                        CheckBox.content "Mac"
                        CheckBox.isEnabled (state.Config.ReleasePlatforms |> Set.contains PC)
                        CheckBox.isChecked (state.Config.ReleasePlatforms |> Set.contains Mac)
                        CheckBox.onChecked (fun _ -> Mac |> AddReleasePlatform |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> Mac |> RemoveReleasePlatform |> EditConfig |> dispatch)
                    ]
                ]

                // Open Containing Folder
                CheckBox.create [
                    CheckBox.verticalAlignment VerticalAlignment.Center
                    CheckBox.content (translate "OpenContainingFolderAfterBuild")
                    CheckBox.isChecked state.Config.OpenFolderAfterReleaseBuild
                    CheckBox.onChecked (fun _ -> true |> SetOpenFolderAfterReleaseBuild |> EditConfig |> dispatch)
                    CheckBox.onUnchecked (fun _ -> false |> SetOpenFolderAfterReleaseBuild |> EditConfig |> dispatch)
                ]
            ]
        ]

        // Test Build Options
        headerWithLine "Test"

        StackPanel.create [
            StackPanel.margin (8., 2.)
            StackPanel.children [
                hStack [
                    // App ID
                    locText "AppID" [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.margin (0., 0., 10., 0.)
                    ]
                    vStack [
                        RadioButton.create [
                            RadioButton.content $"Cherub Rock ({AppId.toString AppId.CherubRock})"
                            RadioButton.isChecked state.Config.CustomAppId.IsNone
                            RadioButton.onChecked (fun _ -> None |> SetCustomAppId |> EditConfig |> dispatch)
                        ]
                        RadioButton.create [
                            RadioButton.isChecked state.Config.CustomAppId.IsSome
                            RadioButton.content (
                                hStack [
                                    locText "Custom" [
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                    ]
                                    FixedTextBox.create [
                                        TextBox.verticalAlignment VerticalAlignment.Center
                                        TextBox.width 120.
                                        TextBox.onTextInput (fun e -> e.Text <- Regex.Replace(e.Text, "[^0-9]", ""))
                                        FixedTextBox.text (state.Config.CustomAppId |> Option.map AppId.toString |> Option.toObj)
                                        FixedTextBox.onTextChanged (AppId.ofString >> SetCustomAppId >> EditConfig >> dispatch)
                                    ]
                                ]
                            )
                        ]
                    ]
                ]

                // Generate DD
                CheckBox.create [
                    CheckBox.verticalAlignment VerticalAlignment.Center
                    CheckBox.content (translate "GenerateDDLevels")
                    CheckBox.isChecked state.Config.GenerateDD
                    CheckBox.onChecked (fun _ -> true |> SetGenerateDD |> EditConfig |> dispatch)
                    CheckBox.onUnchecked (fun _ -> false |> SetGenerateDD |> EditConfig |> dispatch)
                ]

                // Save Debug Files
                hStack [
                    CheckBox.create [
                        CheckBox.verticalAlignment VerticalAlignment.Center
                        CheckBox.content (translate "SaveDebugFiles")
                        CheckBox.isChecked state.Config.SaveDebugFiles
                        CheckBox.onChecked (fun _ -> true |> SetSaveDebugFiles |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> false |> SetSaveDebugFiles |> EditConfig |> dispatch)
                    ]
                    HelpButton.create [
                        HelpButton.helpText (translate "SaveDebugFilesHelp")
                    ]
                ]
            ]
        ]
    ]

let private tabHeader (icon: Geometry) locKey =
    vStack [
        Path.create [
            Path.fill Brushes.DarkGray
            Path.data icon
            Path.horizontalAlignment HorizontalAlignment.Center
        ]

        locText locKey [
            TextBlock.horizontalAlignment HorizontalAlignment.Center
            TextBlock.margin (0., 4., 0., 0.)
        ]
    ]

let view state dispatch focusedSetting =
    DockPanel.create [
        DockPanel.width 600.
        DockPanel.height 480.
        DockPanel.children [
            // Close button
            Button.create [
                DockPanel.dock Dock.Bottom
                Button.fontSize 16.
                Button.padding (50., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (translate "Close")
                Button.onClick (fun _ -> (CloseOverlay OverlayCloseMethod.OverlayButton) |> dispatch)
            ]

            TabControl.create [
                TabControl.viewItems [
                    TabItem.create [
                        TabItem.horizontalAlignment HorizontalAlignment.Center
                        TabItem.header (tabHeader Media.Icons.cog "General")
                        TabItem.content (generalConfig state dispatch)
                    ]
                    TabItem.create [
                        TabItem.isSelected (Option.isSome focusedSetting)
                        TabItem.horizontalAlignment HorizontalAlignment.Center
                        TabItem.header (tabHeader Media.Icons.folderOpen "Paths")
                        TabItem.content (pathsConfig state dispatch focusedSetting)
                    ]
                    TabItem.create [
                        TabItem.horizontalAlignment HorizontalAlignment.Center
                        TabItem.header (tabHeader Media.Icons.package "Build")
                        TabItem.content (buildConfig state dispatch)
                    ]
                    TabItem.create [
                        TabItem.horizontalAlignment HorizontalAlignment.Center
                        TabItem.header (tabHeader Media.Icons.bars "DD")
                        TabItem.content (ddConfig state dispatch)
                    ]
                    TabItem.create [
                        TabItem.horizontalAlignment HorizontalAlignment.Center
                        TabItem.header (tabHeader Media.Icons.import "ImportHeader")
                        TabItem.content (importConfig state dispatch)
                    ]
                ]
            ]
        ]
    ] |> generalize
