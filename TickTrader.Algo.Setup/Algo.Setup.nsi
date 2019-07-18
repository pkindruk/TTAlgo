;--------------------------
; Includes

!addplugindir "Plugins"

!include "LogicLib.nsh"
!include "MUI.nsh"
!include "x64.nsh"

!include "Algo.Utils.nsh"
!include "Resources\Resources.en.nsi"
!include "Algo.Setup.nsh"

;--------------------------
; Main Install settings

Name "${PRODUCT_NAME}"
Icon "${ICONS_DIR}\softfx.ico"
BrandingText "${PRODUCT_PUBLISHER}"
OutFile "${OUTPUT_DIR}\${SETUP_FILENAME}"
InstallDir ${BASE_INSTDIR}

VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "CompanyName" "${PRODUCT_PUBLISHER}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalCopyright" "Copyright © ${PRODUCT_PUBLISHER} 2019"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileDescription" "${TERMINAL_NAME} and ${AGENT_NAME} installer"
VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductVersion" "${PRODUCT_BUILD}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileVersion" "${PRODUCT_BUILD}"

VIProductVersion "${PRODUCT_BUILD}"
VIFileVersion "${PRODUCT_BUILD}"

;--------------------------
; Modern interface settings

!define MUI_ABORTWARNING
!define MUI_COMPONENTSPAGE_SMALLDESC
!define MUI_ICON "${ICONS_DIR}\softfx.ico"
!define MUI_FINISHPAGE_NOAUTOCLOSE
!define MUI_UNFINISHPAGE_NOAUTOCLOSE

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${LICENSE_FILE}"
!define MUI_PAGE_CUSTOMFUNCTION_LEAVE DirectoryOnLeave
!insertmacro MUI_PAGE_DIRECTORY
!define MUI_PAGE_CUSTOMFUNCTION_LEAVE ComponentsOnLeave
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

; Set languages(first is default language)
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_RESERVEFILE_LANGDLL

;--------------------------
; Installation types

InstType Standard
InstType Minimal
InstType Terminal
InstType Agent
InstType Full

!define StandardInstall 0
!define MinimalInstall 1
!define TerminalInstall 2
!define AgentInstall 3
!define FullInstall 4

!define StandardInstallBitFlag 1
!define MinimalInstallBitFlag 2
!define TerminalInstallBitFlag 4
!define AgentInstallBitFlag 8
!define FullInstallBitFlag 16

;--------------------------
; Init

Function .onInit

    InstTypeSetText ${StandardInstall} $(StandardInstallText)
    InstTypeSetText ${MinimalInstall} $(MinimalInstallText)
    InstTypeSetText ${TerminalInstall} $(TerminalInstallText)
    InstTypeSetText ${AgentInstall} $(AgentInstallText)
    InstTypeSetText ${FullInstall} $(FullInstallText)

    Call ConfigureComponents
    Call ConfigureInstallTypes

    ${If} ${Runningx64}
        SetRegView 64
    ${EndIf}

FunctionEnd

Function un.onInit

    ${If} ${Runningx64}
        SetRegView 64
    ${EndIf}

FunctionEnd

;--------------------------
; Components

SectionGroup "Install BotTerminal" TerminalGroup

Section "Core files" TerminalCore

    Push $3

    DetailPrint "Installing BotTerminal"

    SetOutPath "$INSTDIR\${TERMINAL_NAME}"
    ReadRegStr $3 HKLM "${REG_TERMINAL_KEY}\$TerminalId" "Path"
    ${If} $OUTDIR == $3
        MessageBox MB_YESNO|MB_ICONQUESTION "$(UninstallPrevTerminal)" IDYES UninstallTerminalLabel IDNO SkipTerminalLabel
UninstallTerminalLabel:
        ${CheckTerminalLock} $(TerminalIsRunningInstall) UninstallTerminalLabel SkipTerminalLabel
        ${UninstallApp} $OUTDIR
    ${EndIf}

    !insertmacro UnpackTerminal
    !insertmacro RegWriteTerminal
    !insertmacro CreateTerminalShortcuts
    WriteUninstaller "$INSTDIR\${TERMINAL_NAME}\uninstall.exe"
    Goto TerminalInstallEnd
SkipTerminalLabel:
    DetailPrint "Skipped BotTerminal installation"
TerminalInstallEnd:

    Pop $3

SectionEnd

Section "Desktop Shortcut" TerminalDesktop

SectionEnd

Section "StartMenu Shortcut" TerminalStartMenu

SectionEnd

Section "Test Collection" TerminalTestCollection

    DetailPrint "Installing TestCollection"
    
    SetOutPath "$INSTDIR\${TERMINAL_NAME}\${REPOSITORY_DIR}"
    !insertmacro UnpackTestCollection

SectionEnd

SectionGroupEnd


SectionGroup "Install BotAgent" AgentGroup

Section "Core files" AgentCore

    Push $3

    DetailPrint "Installing BotAgent"

    SetOutPath "$INSTDIR\${AGENT_NAME}"
    ReadRegStr $3 HKLM "${REG_AGENT_KEY}\$AgentId" "Path"
    ${If} $OUTDIR == $3
        MessageBox MB_YESNO|MB_ICONQUESTION "$(UninstallPrevAgent)" IDYES UninstallAgentLabel IDNO SkipAgentLabel
UninstallAgentLabel:
        ${StopService} $AgentServiceId 80
        ${UninstallApp} $OUTDIR
    ${EndIf}

    !insertmacro UnpackAgent
    !insertmacro RegWriteAgent
    !insertmacro CreateConfiguratorShortcuts
    WriteUninstaller "$INSTDIR\${AGENT_NAME}\uninstall.exe"

    DetailPrint "Creating BotAgent service"
    ${InstallService} $AgentServiceId "${SERVICE_DISPLAY_NAME}" "16" "2" "$OUTDIR\${AGENT_EXE}" 80
    ${ConfigureService} $AgentServiceId    

    DetailPrint "Starting BotAgent service"
    ${StartService} $AgentServiceId 30
    Goto AgentInstallEnd
SkipAgentLabel:
    DetailPrint "Skipped BotAgent installation"
AgentInstallEnd:

    Pop $3

SectionEnd

SectionGroup "Install Configurator" ConfiguratorGroup

Section "Core files" ConfiguratorCore

SectionEnd

Section "Desktop Shortcut" ConfiguratorDesktop

SectionEnd

Section "StartMenu Shortcut" ConfiguratorStartMenu

SectionEnd

SectionGroupEnd

SectionGroupEnd


Section - FinishSection

SectionEnd

Section Uninstall

    ${FindTerminalId} $INSTDIR
    ${If} $TerminalId != ${EMPTY_APPID}
        
    RetryUninstallTerminal:
        ${CheckTerminalLock} $(TerminalIsRunningUninstall) RetryUninstallTerminal SkipUninstallTerminal

        ; Remove installed files, but leave generated
        !insertmacro DeleteTerminalFiles
        !insertmacro DeleteTerminalShortcuts
        
        ; Delete self
        Delete "$INSTDIR\uninstall.exe"
        
        ; Remove registry entries
        !insertmacro RegDeleteTerminal
        Goto TerminalUninstallEnd
    SkipUninstallTerminal:
        Abort $(UninstallCanceledMessage)
    TerminalUninstallEnd:

    ${EndIf}

    ${FindAgentId} $INSTDIR
    ${If} $AgentId != ${EMPTY_APPID}

        !insertmacro InitAgentServiceId
        
        ${StopService} $AgentServiceId 80
        ${UninstallService} $AgentServiceId 80

        ; Remove installed files, but leave generated
        !insertmacro DeleteConfiguratorShortcuts
        !insertmacro DeleteAgentFiles

        ; Delete self
        Delete "$INSTDIR\uninstall.exe"

        ; Remove registry entries
        !insertmacro RegDeleteAgent

    ${EndIf}

SectionEnd

;--------------------------
; Components description

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN

    !insertmacro MUI_DESCRIPTION_TEXT ${TerminalGroup} $(TerminalSection1Description)
    !insertmacro MUI_DESCRIPTION_TEXT ${TerminalCore} $(TerminalSection1Description)
    !insertmacro MUI_DESCRIPTION_TEXT ${TerminalDesktop} $(TerminalSection2Description)
    !insertmacro MUI_DESCRIPTION_TEXT ${TerminalStartMenu} $(TerminalSection3Description)
    !insertmacro MUI_DESCRIPTION_TEXT ${TerminalTestCollection} $(TerminalSection4Description)

    !insertmacro MUI_DESCRIPTION_TEXT ${AgentGroup} $(AgentSection1Description)
    !insertmacro MUI_DESCRIPTION_TEXT ${AgentCore} $(AgentSection1Description)

    !insertmacro MUI_DESCRIPTION_TEXT ${ConfiguratorGroup} $(ConfiguratorSection1Description)
    !insertmacro MUI_DESCRIPTION_TEXT ${ConfiguratorCore} $(ConfiguratorSection1Description)
    !insertmacro MUI_DESCRIPTION_TEXT ${ConfiguratorDesktop} $(ConfiguratorSection2Description)

!insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------
; Components configuration

Function ConfigureComponents

    ${BeginSectionManagement}

        ${ReadOnlySection} ${TerminalCore}
        ${ReadOnlySection} ${AgentCore}
        ${ReadOnlySection} ${ConfiguratorCore}
        ${ReadOnlySection} ${ConfiguratorGroup} ; configurator is always installed with agent

        ${ExpandSection} ${TerminalGroup}
        ${ExpandSection} ${AgentGroup}
        ${ExpandSection} ${ConfiguratorGroup}

    ${EndSectionManagement}

    SectionGetSize ${TerminalCore} $TerminalSize
    SectionGetSize ${AgentCore} $AgentSize

FunctionEnd

Function ConfigureInstallTypes

    Push $0

    StrCpy $0 ${FullInstallBitFlag}
    ; 010000b
    SectionSetInstTypes ${TerminalTestCollection} $0

    IntOp $0 $0 | ${StandardInstallBitFlag}
    IntOp $0 $0 | ${TerminalInstallBitFlag}
    ; 010101b
    SectionSetInstTypes ${TerminalDesktop} $0
    SectionSetInstTypes ${TerminalStartMenu} $0

    IntOp $0 $0 | ${MinimalInstallBitFlag}
    ; 010111b
    SectionSetInstTypes ${TerminalCore} $0

    IntOp $0 $0 ^ ${TerminalInstallBitFlag}
    IntOp $0 $0 | ${AgentInstallBitFlag}
    ; 011011b
    SectionSetInstTypes ${AgentCore} $0
    SectionSetInstTypes ${ConfiguratorCore} $0

    IntOp $0 $0 ^ ${MinimalInstallBitFlag}
    ; 011001b
    SectionSetInstTypes ${ConfiguratorDesktop} $0
    SectionSetInstTypes ${ConfiguratorStartMenu} $0

    Pop $0

    SetCurInstType ${StandardInstall}

FunctionEnd

!macro DisableTerminalSections
    ${DisableSection} ${TerminalDesktop}
    ${DisableSection} ${TerminalStartMenu}
    ${DisableSection} ${TerminalTestCollection}
!macroend

!macro EnableTerminalSections
    ${EnableSection} ${TerminalDesktop}
    ${EnableSection} ${TerminalStartMenu}
    ${EnableSection} ${TerminalTestCollection}
!macroend

!macro DisableConfiguratorSections
    ${DisableSection} ${ConfiguratorDesktop}
    ${DisableSection} ${ConfiguratorStartMenu}
!macroend

!macro EnableConfiguratorSections
    ${EnableSection} ${ConfiguratorDesktop}
    ${EnableSection} ${ConfiguratorStartMenu}
!macroend

!macro DisableAgentSections
    ; ${ReadOnlySection} ${ConfiguratorGroup}
    !insertmacro DisableConfiguratorSections
!macroend

!macro EnableAgentSections
    ; ${EnableSection} ${ConfiguratorGroup}
    !insertmacro EnableConfiguratorSections
!macroend

Function .onSelChange
    
    ${BeginSectionManagement}

        ${if} $0 == ${TerminalGroup}
            ; MessageBox MB_OK "Terminal Group"
            ${If} ${SectionIsSelected} ${TerminalCore}
                ${UnselectSection} ${TerminalCore}
                !insertmacro DisableTerminalSections
            ${Else}
                ${SelectSection} ${TerminalCore}
                !insertmacro EnableTerminalSections
            ${EndIf}
        ${EndIf}

        ${if} $0 == ${AgentGroup}
            ; MessageBox MB_OK "Agent Group"
            ${If} ${SectionIsSelected} ${AgentCore}
                ${UnselectSection} ${AgentCore}
                ${UnselectSection} ${ConfiguratorCore}
                !insertmacro DisableAgentSections
            ${Else}
                ${SelectSection} ${ConfiguratorCore}
                ${SelectSection} ${AgentCore}
                ${SelectSection} ${ConfiguratorCore}
                !insertmacro EnableAgentSections
            ${EndIf}
        ${EndIf}

        ${if} $0 == ${ConfiguratorGroup}
            ; MessageBox MB_OK "Configurator Group"
            ${If} ${SectionIsSelected} ${ConfiguratorCore}
                ${UnselectSection} ${ConfiguratorCore}
                !insertmacro DisableConfiguratorSections
            ${Else}
                ${SelectSection} ${ConfiguratorCore}
                !insertmacro EnableConfiguratorSections
            ${EndIf}
        ${EndIf}

        ${If} $0 == -1
            ; MessageBox MB_OK "Installation type change"
            ${If} ${SectionIsSelected} ${TerminalCore}
                !insertmacro EnableTerminalSections
            ${Else}
                !insertmacro DisableTerminalSections
            ${EndIf}
            ${If} ${SectionIsSelected} ${AgentCore}
                !insertmacro EnableAgentSections
            ${Else}
                !insertmacro DisableAgentSections
            ${EndIf}
            ${If} ${SectionIsSelected} ${ConfiguratorCore}
                !insertmacro EnableConfiguratorSections
            ${Else}
                !insertmacro DisableConfiguratorSections
            ${EndIf}

        ${EndIf}

    ${EndSectionManagement}

FunctionEnd

;--------------------------
; Callbacks

Function DirectoryOnLeave

    ${FindTerminalId} "$INSTDIR\${TERMINAL_NAME}"
    ${If} $TerminalId == ${EMPTY_APPID}
        ${CreateAppId} $TerminalId
    ${EndIf}

    ${FindAgentId} "$INSTDIR\${AGENT_NAME}"
    ${If} $AgentId == ${EMPTY_APPID}
        ${CreateAppId} $AgentId
    ${EndIf}
    !insertmacro InitAgentServiceId

FunctionEnd

Function ComponentsOnLeave

    ${If} ${SectionIsSelected} ${TerminalDesktop}
        StrCpy $TerminalDesktopSelected 1
    ${Else}
        StrCpy $TerminalDesktopSelected 0
    ${EndIf}

    ${If} ${SectionIsSelected} ${TerminalStartMenu}
        StrCpy $TerminalStartMenuSelected 1
    ${Else}
        StrCpy $TerminalStartMenuSelected 0
    ${EndIf}

    ${If} ${SectionIsSelected} ${ConfiguratorDesktop}
        StrCpy $ConfiguratorDesktopSelected 1
    ${Else}
        StrCpy $ConfiguratorDesktopSelected 0
    ${EndIf}

    ${If} ${SectionIsSelected} ${ConfiguratorStartMenu}
        StrCpy $ConfiguratorStartMenuSelected 1
    ${Else}
        StrCpy $ConfiguratorStartMenuSelected 0
    ${EndIf}

FunctionEnd
