Option Explicit

' Legacy cleanup VBScript for IPPopper MSI installer.
' Called by WiX v4 as: VBScriptCall="Call Main()"
'
' IMPORTANT:
' - No top-level execution (MSI CA must be deterministic)
' - No UI output (no WScript.Echo)
' - Log to C:\Temp\IPPopperCleanup.log

Sub Main()
    On Error Resume Next

    Dim fso
    Dim shell
    Dim logPath
    Dim legacyPath
    Dim runValuePath
    Dim commonStartMenu
    Dim shortcutPath

    Set fso = CreateObject("Scripting.FileSystemObject")
    Set shell = CreateObject("WScript.Shell")

    logPath = "C:\Temp\IPPopperCleanup.log"
    legacyPath = "C:\IPPopper"
    runValuePath = "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run\IPPopper"
    commonStartMenu = shell.SpecialFolders("AllUsersStartMenu")
    shortcutPath = commonStartMenu & "\Programs\IPPopper.lnk"

    EnsureFolderExists fso, "C:\Temp"

    LogMessage fso, logPath, "Legacy cleanup starting..."

    StopProcess shell, fso, logPath, "IPPopper.exe"
    DeleteFolderIfExists fso, logPath, legacyPath
    DeleteRegistryValue shell, fso, logPath, runValuePath
    DeleteFileIfExists fso, logPath, shortcutPath

    LogMessage fso, logPath, "Legacy cleanup completed successfully."
End Sub

Private Sub EnsureFolderExists(ByVal fso, ByVal folderPath)
    On Error Resume Next
    If Not fso.FolderExists(folderPath) Then
        fso.CreateFolder folderPath
    End If
End Sub

Private Sub LogMessage(ByVal fso, ByVal logPath, ByVal message)
    On Error Resume Next

    Dim logFile
    Set logFile = fso.OpenTextFile(logPath, 8, True)
    logFile.WriteLine CStr(Now) & " [IPPopper] " & message
    logFile.Close
End Sub

Private Sub StopProcess(ByVal shell, ByVal fso, ByVal logPath, ByVal imageName)
    On Error Resume Next

    LogMessage fso, logPath, "Stopping process (if running): " & imageName
    shell.Run "taskkill /IM " & QuoteArg(imageName) & " /F /T", 0, True

    ' Wait a moment for processes to release file locks
    WScript.Sleep 1500
End Sub

Private Sub DeleteFolderIfExists(ByVal fso, ByVal logPath, ByVal path)
    On Error Resume Next

    If fso.FolderExists(path) Then
        LogMessage fso, logPath, "Removing legacy folder: " & path
        fso.DeleteFolder path, True
        If Err.Number = 0 Then
            LogMessage fso, logPath, "Legacy folder removed successfully."
        Else
            LogMessage fso, logPath, "Warning: Could not remove legacy folder: " & Err.Description
            Err.Clear
        End If
    Else
        LogMessage fso, logPath, "Legacy folder not found: " & path
    End If
End Sub

Private Sub DeleteFileIfExists(ByVal fso, ByVal logPath, ByVal path)
    On Error Resume Next

    If fso.FileExists(path) Then
        LogMessage fso, logPath, "Removing legacy Start Menu shortcut: " & path
        fso.DeleteFile path, True
        If Err.Number = 0 Then
            LogMessage fso, logPath, "Legacy Start Menu shortcut removed."
        Else
            LogMessage fso, logPath, "Warning: Could not remove shortcut: " & Err.Description
            Err.Clear
        End If
    Else
        LogMessage fso, logPath, "Legacy Start Menu shortcut not found: " & path
    End If
End Sub

Private Sub DeleteRegistryValue(ByVal shell, ByVal fso, ByVal logPath, ByVal valuePath)
    On Error Resume Next

    LogMessage fso, logPath, "Checking for legacy startup registry value..."
    shell.RegDelete valuePath
    If Err.Number = 0 Then
        LogMessage fso, logPath, "Legacy startup registry value removed."
    Else
        LogMessage fso, logPath, "Legacy startup registry value not found (or could not be removed): " & Err.Description
        Err.Clear
    End If
End Sub

Private Function QuoteArg(ByVal value)
    QuoteArg = Chr(34) & Replace(value, Chr(34), Chr(34) & Chr(34)) & Chr(34)
End Function
