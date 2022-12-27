Imports System.IO
Imports System.Xml.Serialization

Public Class Main

    Public Device As USBDevice
    Private WithEvents InstallationProcess As Process

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Initialize()
        If Not IO.File.Exists("C:\Program Files (x86)\Measurement Computing\DAQ\inscal32.exe") Then
            Dim downloadpath As String = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) & "\Downloads\"
            IO.File.WriteAllBytes(downloadpath & "icalsetup.exe", My.Resources.icalsetup)
            If IO.File.Exists(downloadpath & "icalsetup.exe") Then _
            Process.Start(downloadpath & "icalsetup.exe")
        Else
            If Device.Ready Then
                Text = Device.Name
            Else
                Text = "No USB-Device connected!"
            End If
        End If
    End Sub

    Private Sub Initialize()
        Device = New USBDevice(0) 'Index 0 für erstes Board
        ZedGraphControl1.GraphPane.CurveList.Clear()
        For i As Integer = 0 To 7
            ZedGraphControl1.GraphPane.CurveList.Add(New ZedGraph.LineItem("Channel " & i + 1) With
                                                     {.Color = GetColor(i),
                                                     .Symbol = New ZedGraph.Symbol(ZedGraph.SymbolType.None, GetColor(i))})
            If Not Device.Channel(i).Connected Then
                Dim cb As CheckBox = Controls.Find("CheckBox" & i + 1, True).First
                cb.Checked = False
                cb.BackColor = Color.LightGray
            End If
        Next

        ZedGraphControl1.GraphPane.XAxis.Type = ZedGraph.AxisType.Date
        ZedGraphControl1.GraphPane.XAxis.Title.Text = "Date"
        ZedGraphControl1.GraphPane.YAxis.Title.Text = "Temperature [°C]"
        ZedGraphControl1.GraphPane.Title.Text = "USB temp Data Plot"
    End Sub

    Private Function GetColor(index As Integer) As Color
        Select Case index
            Case 0
                Return Color.Black
            Case 1
                Return Color.Red
            Case 2
                Return Color.Blue
            Case 3
                Return Color.Green
            Case 4
                Return Color.Brown
            Case 5
                Return Color.Pink
            Case 6
                Return Color.Orange
            Case 7
                Return Color.LightBlue
            Case 8
                Return Color.LightGreen
        End Select
    End Function

    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        Timer1.Start()
        ToolStripButton1.Enabled = False
        ToolStripButton2.Enabled = True
    End Sub

    Private Sub ToolStripButton2_Click(sender As Object, e As EventArgs) Handles ToolStripButton2.Click
        Timer1.Stop()
        ToolStripButton1.Enabled = True
        ToolStripButton2.Enabled = False
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        For i As Integer = 0 To 7
            Dim cb As CheckBox = Controls.Find("CheckBox" & i + 1, True).First
            If cb.Checked Then
                Dim data As datapoint = Device.Measure(i)
                ZedGraphControl1.GraphPane.CurveList(i).AddPoint(data.MeasurementDate.ToOADate, data.Value)
                ZedGraphControl1.AxisChange()
                ZedGraphControl1.Refresh()
                cb.Text = "Channel " & i + 1 & " [Last = " & data.Value & " °C]"
            End If
        Next
    End Sub

    Private Sub ToolStripButton3_Click(sender As Object, e As EventArgs) Handles ToolStripButton3.Click
        If MsgBox("Sicher alles löschen?", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
            Timer1.Stop()
            Device.Clear()
            For i As Integer = 0 To 7
                ZedGraphControl1.GraphPane.CurveList(i).Clear()
                Dim cb As CheckBox = Controls.Find("CheckBox" & i + 1, True).First
                cb.Text = "Channel " & i + 1
            Next i
            ZedGraphControl1.Refresh()
        End If
    End Sub

    Private Sub ToolStripButton4_Click(sender As Object, e As EventArgs) Handles ToolStripButton4.Click
        Using sfd As New SaveFileDialog
            If sfd.ShowDialog = DialogResult.OK Then Device.Export(sfd.FileName)
        End Using
    End Sub

    Private Sub ToolStripButton5_Click(sender As Object, e As EventArgs) Handles ToolStripButton5.Click
        Process.Start("C:\Program Files (x86)\Measurement Computing\DAQ\inscal32.exe")
    End Sub

    Private Sub ToolStripComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ToolStripComboBox1.SelectedIndexChanged
        Dim value As Integer = ToolStripComboBox1.Text
        Timer1.Interval = value * 1000
    End Sub

    Private Sub ToolStripButton6_Click(sender As Object, e As EventArgs) Handles ToolStripButton6.Click
        AboutBox1.ShowDialog(Me)
    End Sub

    Private Sub Main_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If MsgBox("Sure u want to close and stop measurement?", vbYesNo) = MsgBoxResult.Yes Then
            e.Cancel = False
        Else
            e.Cancel = True
        End If
    End Sub

End Class

Public Class USBDevice
    Private Index As Integer
    Private DaqBoard As MccDaq.MccBoard
    Public Channel(7) As Channel
    Public Ready As Boolean = True

    Public ReadOnly Property Name() As String
        Get
            Return DaqBoard.BoardName & " :ID=" & Index
        End Get
    End Property

    Public Sub New()
    End Sub

    Public Sub New(Index As Integer)
        Me.Index = Index
        DaqBoard = New MccDaq.MccBoard(Index)
        For i As Integer = 0 To 7
            Channel(i) = New Channel
            If GetTemperature(i).Value = -9999 Then Channel(i).Connected = False
            If GetTemperature(i).Value = 0 Then
                Channel(i).Connected = False
                Ready = False
            End If
        Next
    End Sub

    Private Function GetTemperature(ChannelIndex As Integer) As datapoint
        Dim Value As Single
        Try
            DaqBoard.TIn(ChannelIndex, MccDaq.TempScale.Celsius, Value, MccDaq.ThermocoupleOptions.NoFilter)
        Catch ex As Exception
            Value = -9000
        End Try
        Return New datapoint(Date.Now, Value)
    End Function

    Public Sub Clear()
        For i As Integer = 0 To 7
            Channel(i).data.Clear()
        Next i
    End Sub

    Public Function Measure(Index As Integer) As datapoint
        Dim dp As datapoint = GetTemperature(Index)
        Channel(Index).data.Add(dp)
        Return dp
    End Function

    Public Sub Export(FileName As String)
        Dim xml As New XmlSerializer(GetType(USBDevice))
        Using fs As New FileStream(IO.Path.ChangeExtension(FileName, ".xml"), FileMode.Create)
            xml.Serialize(fs, Me)
            fs.Close()
        End Using
    End Sub

    Public Shared Function Load(ByVal filename As String) As USBDevice
        Dim xml As New XmlSerializer(GetType(USBDevice))
        Using fs As New FileStream(filename, FileMode.Open)
            Dim q As USBDevice = xml.Deserialize(fs)
            fs.Close()
            Return q
        End Using
    End Function

End Class

Public Class Channel
    Public Connected As Boolean = True
    Public data As New List(Of datapoint)
    Public Name As String
    Public Sub New()
    End Sub
End Class

Public Class datapoint
    Public MeasurementDate As Date
    Public Value As Double
    Public Sub New()
    End Sub
    Public Sub New(Value As Double)
        MeasurementDate = Date.Now
        Me.Value = Value
    End Sub

    Public Sub New(meas_date As Date, value As Double)
        Me.MeasurementDate = meas_date 'New Date(meas_date.Year, meas_date.Month, meas_date.Day, meas_date.Hour, meas_date.Minute, meas_date.Second)
        Me.Value = value
    End Sub

End Class
