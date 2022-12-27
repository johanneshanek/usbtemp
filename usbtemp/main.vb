Imports System.IO
Imports System.Xml.Serialization

Public Class Form1

    Public Device As New USBDevice

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Text = Device.Name
        Initialize()
    End Sub

    Private Sub Initialize()
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
    End Sub

    Private Sub ToolStripButton2_Click(sender As Object, e As EventArgs) Handles ToolStripButton2.Click
        Timer1.Stop()
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
        Timer1.Stop()
        Device.Clear()
        For i As Integer = 0 To 7
            ZedGraphControl1.GraphPane.CurveList(i).Clear()
            Dim cb As CheckBox = Controls.Find("CheckBox" & i + 1, True).First
            cb.Text = "Channel " & i + 1
        Next i
        ZedGraphControl1.Refresh()
    End Sub

    Private Sub ToolStripButton4_Click(sender As Object, e As EventArgs) Handles ToolStripButton4.Click
        Using sfd As New SaveFileDialog
            If sfd.ShowDialog = DialogResult.OK Then Device.Export(sfd.FileName)
        End Using
    End Sub

End Class

Public Class USBDevice
    Private DaqBoard As New MccDaq.MccBoard(0)
    Public Channel(7) As Channel

    Public ReadOnly Property Name() As String
        Get
            Return DaqBoard.BoardName
        End Get
    End Property

    Public Sub New()
        For i As Integer = 0 To 7
            Channel(i) = New Channel
            If GetTemperature(i).Value = -9999 Then Channel(i).Connected = False
        Next
    End Sub

    Private Function GetTemperature(ChannelIndex As Integer) As datapoint
        Dim Value As Single
        DaqBoard.TIn(ChannelIndex, MccDaq.TempScale.Celsius, Value, MccDaq.ThermocoupleOptions.NoFilter)
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
End Class

Public Class datapoint
    Public MeasurementDate As Date
    Public Value As Double
    Public Sub New(Value As Double)
        MeasurementDate = Date.Now
        Me.Value = Value
    End Sub

    Public Sub New(meas_date As Date, value As Double)
        Me.MeasurementDate = meas_date 'New Date(meas_date.Year, meas_date.Month, meas_date.Day, meas_date.Hour, meas_date.Minute, meas_date.Second)
        Me.Value = value
    End Sub

End Class
