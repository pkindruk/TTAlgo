;--------------------------------------------
;-----Functions to manage window service-----

!macro _InstallService Name DisplayName ServiceType StartType BinPath TimeOut
    SimpleSC::ExistsService ${Name}
    Pop $0
    ${If} $0 == 0
        SimpleSC::RemoveService ${Name}
        Pop $0
        ${If} $0 != 0
            Push $0
            SimpleSC::GetErrorMessage
            Pop $1
            Abort "$(ServiceUninstallFailMessage) $0 $1"
        ${EndIf}
    ${EndIf}
    
    SimpleSC::InstallService "${Name}" "${DisplayName}" "${ServiceType}" "${StartType}" ${BinPath} "" "" ""
    Pop $0
    ${If} $0 != 0
        Push $0
        SimpleSC::GetErrorMessage
        Pop $1
        Abort "$(ServiceInstallFailMessage) $0 $1"
    ${EndIf}
!macroend

!macro _ConfigureService Name
    SimpleSC::ExistsService ${Name}
    Pop $0
    ${If} $0 == 0
        SimpleSC::SetServiceFailure ${Name} 0 "" "" 1 60000 1 60000 0 60000
        Pop $0
        ${If} $0 != 0
            Abort "$(ServiceConfigFailMessage) $0"
        ${EndIf}
    ${EndIf}
!macroend

!macro _StartService Name TimeOut
    SimpleSC::ExistsService ${Name}
    Pop $0
    ${If} $0 == 0
        SimpleSC::StartService "${Name}" "" ${TimeOut}
    Pop $0
        ${If} $0 != 0
            Abort "$(ServiceStartFailMessage) $0"
        ${EndIf}
    ${EndIf}
!macroend

!macro _StopService Name TimeOut
    SimpleSC::ExistsService ${Name}
    Pop $0
    ${If} $0 == 0
        SimpleSC::ServiceIsStopped ${Name}
        Pop $0
        Pop $1
        ${If} $1 == 0
            SimpleSC::StopService "${SERVICE_NAME}" 1 ${TimeOut}
            Pop $0
            ${If} $0 != 0
                Abort "$(ServiceStopFailMessage) $0"
            ${EndIf}
        ${EndIf}
    ${EndIf}
!macroend

!macro _UninstallService Name TimeOut
    SimpleSC::ExistsService ${Name}
    Pop $0
    ${If} $0 == 0
        SimpleSC::ServiceIsStopped ${Name}
        Pop $0
        Pop $1
        ${If} $1 == 0
            SimpleSC::StopService "${SERVICE_NAME}" 1 ${TimeOut}
            Pop $0
            ${If} $0 != 0
                Abort "$(ServiceStopFailMessage) $0"
            ${EndIf}
        ${EndIf}
     
        SimpleSC::RemoveService ${Name}
        Pop $0
        ${If} $0 != 0
            Push $0
            SimpleSC::GetErrorMessage
            Pop $1
            Abort "$(ServiceUninstallFailMessage) $0 $1"
        ${EndIf}
    ${EndIf}
!macroend

!define InstallService '!insertmacro "_InstallService"'
!define StartService '!insertmacro "_StartService"'
!define StopService '!insertmacro "_StopService"'
!define UninstallService '!insertmacro "_UninstallService"'
!define ConfigureService '!insertmacro "_ConfigureService"'

;---END Functions to manage window service---

;--------------------------------------------
;-----Functions to manage sections-----

!define SECTION_ENABLE 0xFFFFFFEF # remove read-only flag
!define GROUP_REMOVE 0xFFFFFFFD # remove group flag
 
!macro SecSelect SecId
    Push $7
    SectionGetFlags ${SecId} $7
    IntOp $7 $7 | ${SF_SELECTED}
    SectionSetFlags ${SecId} $7
    Pop $7
!macroend

!macro SecUnselect SecId
    Push $7
    SectionGetFlags ${SecId} $7
    IntOp $7 $7 & ${SECTION_OFF}
    SectionSetFlags ${SecId} $7
    Pop $7
!macroend

!macro SecRO SecId
    Push $7
    SectionGetFlags ${SecId} $7
    IntOp $7 $7 | ${SF_RO}
    SectionSetFlags ${SecId} $7
    Pop $7
!macroend

!macro SecDisable SecId
    Push $7
    SectionGetFlags ${SecId} $7
    IntOp $7 $7 & ${SECTION_OFF}
    IntOp $7 $7 | ${SF_RO}
    SectionSetFlags ${SecId} $7
    Pop $7
!macroend

!macro SecRemoveRO SecId
    Push $7
    SectionGetFlags ${SecId} $7
    IntOp $7 $7 & ${SECTION_ENABLE}
    SectionSetFlags ${SecId} $7
    Pop $7
!macroend

!macro SecExpand SecId
    Push $7
    SectionGetFlags ${SecId} $7
    IntOp $7 $7 | ${SF_EXPAND}
    SectionSetFlags ${SecId} $7
    Pop $7
!macroend

!macro SecManageBegin
    Push $7
!macroend

!macro SecManageEnd
    Pop $7
!macroend

!define SelectSection '!insertmacro SecSelect'
!define UnselectSection '!insertmacro SecUnselect'
!define ReadOnlySection '!insertmacro SecRO'
!define DisableSection '!insertmacro SecDisable'
!define EnableSection '!insertmacro SecRemoveRO'
!define ExpandSection '!insertmacro SecExpand'
!define BeginSectionManagement '!insertmacro SecManageBegin'
!define EndSectionManagement '!insertmacro SecManageEnd'

;---END Functions to manage sections---