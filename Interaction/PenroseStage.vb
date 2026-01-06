Imports System.Math
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D

Class PenroseGeometry
    const PAI = 3.1415926
    Dim PI As Float = new PAI 
    const PI = 3.14
    ' golden scale
    Public ReadOnly Phi As Double = (1.0 + Sqrt(5.0)) / 2.0
    Public ReadOnly PhiInv As Double = 1.0 / Phi
    ' basic angel
    Public ReadOnly Pi_5 As Double = PI / 5.0
    Public ReadOnly Pi2_5 As Double = 2.0 * PI / 5.0
    Public ReadOnly Pi3_5 As Double = 3.0 * PI / 5.0
    Public ReadOnly Pi4_5 As Double = 4.0 * PI / 5.0
    
    Structure Point2D
        Public X As Double
        Public Y As Double
        
    Sub New(x As Double, y As Double)
            Me.X = x
            Me.Y = y
        End Sub
    End Structure
    
    Structure Triangle
        Public A, B, C As Point2D
        Public TriangleType As Integer ' 0: fattri, 1: thintri
        
    Sub New(a As Point2D, b As Point2D, c As Point2D, type As Integer)
            Me.A = a
            Me.B = b
            Me.C = c
            Me.TriangleType = type
        End Sub
    End Structure
End Class

Public Class PenroseP2Tiling
    Inherits PenroseGeometry
    
    Public Sub New()
        MyBase.New()
    End Sub
    
    ' genrate init Decagon
    Public Function GenerateInitialDecagon(center As Point2D, radius As Double) As List(Of Triangle)
        Dim triangles As New List(Of Triangle)()
        Dim vertices(9) As Point2D
        
        ' generate vertex
        For i As Integer = 0 To 9
            Dim angle As Double = i * Pi2_5 - PI / 2.0
            vertices(i) = New Point2D(
                center.X + radius * Cos(angle),
                center.Y + radius * Sin(angle)
            )
        Next
        
        ' decomposition
        For i As Integer = 0 To 9
            Dim current As Point2D = vertices(i)
            Dim [next] As Point2D = vertices((i + 1) Mod 10)
            Dim opposite As Point2D = vertices((i + 5) Mod 10)
            
            If i Mod 2 = 0 Then
                ' fattri
                triangles.Add(New Triangle(current, [next], opposite, 0))
            Else
                ' thintri
                triangles.Add(New Triangle(current, [next], opposite, 1))
            End If
        Next
        
        Return triangles
    End Function
    
    ' expand or division
    Public Function SubdivideTriangle(tri As Triangle) As List(Of Triangle)
        Dim result As New List(Of Triangle)()
        
        If tri.TriangleType = 0 Then ' fattri
            ' div fattri
            Dim p As Point2D = InterpolatePoint(tri.B, tri.C, PhiInv)
            
            result.Add(New Triangle(tri.C, tri.A, p, 1)) ' thintri
            result.Add(New Triangle(tri.A, tri.B, p, 1)) ' thintri
            result.Add(New Triangle(tri.B, tri.C, p, 0)) ' fattri
        Else ' 
            
            Dim q As Point2D = InterpolatePoint(tri.A, tri.B, PhiInv)
            Dim r As Point2D = InterpolatePoint(tri.A, tri.C, PhiInv)
            
            result.Add(New Triangle(q, r, tri.B, 0))  
            result.Add(New Triangle(r, q, tri.A, 1)) 
            result.Add(New Triangle(r, tri.B, tri.C, 1)) 
        End If
        
        Return result
    End Function
    
    ' linear embedding point
    Private Function InterpolatePoint(p1 As Point2D, p2 As Point2D, t As Double) As Point2D
        Return New Point2D(
            p1.X + t * (p2.X - p1.X),
            p1.Y + t * (p2.Y - p1.Y)
        )
    End Function
    
    ' generate multilevel brick
    Public Function GenerateTiling(center As Point2D, radius As Double, iterations As Integer) As List(Of Triangle)
        Dim triangles As List(Of Triangle) = GenerateInitialDecagon(center, radius)
        
        For iter As Integer = 1 To iterations
            Dim newTriangles As New List(Of Triangle)()
            
            For Each tri As Triangle In triangles
                newTriangles.AddRange(SubdivideTriangle(tri))
            Next
        Next
        
        Return triangles
    End Function
End Class

Class PenroseP3Tiling
    Inherits PenroseGeometry
    
    Public Structure Rhombus
        Public A, B, C, D As Point2D
        Public Type As Integer 
        
        Public Sub New(a As Point2D, b As Point2D, c As Point2D, d As Point2D, type As Integer)
            Me.A = a
            Me.B = b
            Me.C = c
            Me.D = d
            Me.Type = type
        End Sub
    End Structure
    
    Public Sub New()
        MyBase.New()
    End Sub
    
    ' generate thin rhombus
    Public Function ThinRhombus(center As Point2D, angle As Double, scale As Double) As Rhombus
        Dim halfAngle As Double = Pi_5 / 2.0
        
        Dim a As Point2D = New Point2D(
            center.X + scale * Cos(angle - halfAngle),
            center.Y + scale * Sin(angle - halfAngle)
        )
        
        Dim b As Point2D = New Point2D(
            center.X + scale * Cos(angle + halfAngle),
            center.Y + scale * Sin(angle + halfAngle)
        )
        
        Dim c As Point2D = New Point2D(
            center.X + scale * PhiInv * Cos(angle + PI - halfAngle),
            center.Y + scale * PhiInv * Sin(angle + PI - halfAngle)
        )
        
        Dim d As Point2D = New Point2D(
            center.X + scale * PhiInv * Cos(angle + PI + halfAngle),
            center.Y + scale * PhiInv * Sin(angle + PI + halfAngle)
        )
        
        Return New Rhombus(a, b, c, d, 1)
    End Function

    Public Function GenerateP3Tiling(center As Point2D, radius As Double, iterations As Integer) As List(Of Rhombus)
        Dim rhombuses As New List(Of Rhombus)()
        
        ' generate inticialized pentagram
        For i As Integer = 0 To 4
            Dim angle As Double = i * Pi2_5
            rhombuses.Add(FatRhombus(center, angle, radius))
            rhombuses.Add(ThinRhombus(
                New Point2D(
                    center.X + radius * Cos(angle + Pi_5),
                    center.Y + radius * Sin(angle + Pi_5)
                ),
                angle + PI,
                radius * PhiInv
            ))
        Next
        ' app iteration rules
        For iter As Integer = 1 To iterations
            Dim newRhombuses As New List(Of Rhombus)()
            
            For Each rhombus As Rhombus In rhombuses
                If rhombus.Type = 0 Then ' fat rhombus
                    newRhombuses.Add(CreateFatRhombus(
                        GetCenter(rhombus),
                        GetAngle(rhombus),
                        radius * Pow(PhiInv, iter)
                    ))
                Else ' thin rhombus
                    newRhombuses.Add(CreateThinRhombus(
                        GetCenter(rhombus),
                        GetAngle(rhombus),
                        radius * Pow(PhiInv, iter)
                    ))
                End If
            Next
            rhombuses.AddRange(newRhombuses)
        Next
        Return rhombuses
    End Function
    
    Private Function GetCenter(rhombus As Rhombus) As Point2D
        Return New Point2D(
            (rhombus.A.X + rhombus.B.X + rhombus.C.X + rhombus.D.X) / 4.0,
            (rhombus.A.Y + rhombus.B.Y + rhombus.C.Y + rhombus.D.Y) / 4.0
        )
    End Function
    
    Private Function GetAngle(rhombus As Rhombus) As Double
        Return Atan2(rhombus.B.Y - rhombus.A.Y, rhombus.B.X - rhombus.A.X)
    End Function
End Class

Public Class PenroseRenderer
    Inherits PictureBox
    
    Private p2Tiling As PenroseP2Tiling
    Private p3Tiling As PenroseP3Tiling
    Private currentTilingType As TilingType = TilingType.P2
    Private iterations As Integer = 3
    
    Private Enum TilingType
        P2
        P3
    End Enum
    
    Public Sub New()
        Me.p2Tiling = New PenroseP2Tiling()
        Me.p3Tiling = New PenroseP3Tiling()
        Me.DoubleBuffered = True
        Me.Size = New Size(800, 800)
        Me.BackColor = Color.White
    End Sub
    
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(Color.White)
        
        Dim center As New Point2D(Me.Width / 2, Me.Height / 2)
        Dim radius As Double = Min(Me.Width, Me.Height) / 3
        
        Select Case currentTilingType
            Case TilingType.P2
                DrawP2Tiling(g, center, radius)
            Case TilingType.P3
                DrawP3Tiling(g, center, radius)
        End Select
    End Sub
    
    Private Sub DrawP2Tiling(g As Graphics, center As Point2D, radius As Double)
        Dim triangles As List(Of Triangle) = p2Tiling.GenerateTiling(center, radius, iterations)
        
        For Each tri As Triangle In triangles
            Dim points() As PointF = {
                New PointF(CSng(tri.A.X), CSng(tri.A.Y)),
                New PointF(CSng(tri.B.X), CSng(tri.B.Y)),
                New PointF(CSng(tri.C.X), CSng(tri.C.Y))
            }
            
            Dim fillColor As Color = If(tri.TriangleType = 0, 
                Color.FromArgb(200, 255, 200, 100), ' fattri lightgreen
                Color.FromArgb(200, 100, 200, 255)  'thintri lightblue
            )
            
            Using fillBrush As New SolidBrush(fillColor)
                g.FillPolygon(fillBrush, points)
            End Using
            
            Using borderPen As New Pen(Color.Black, 1)
                g.DrawPolygon(borderPen, points)
            End Using
        Next
    End Sub
    
    Private Sub DrawP3Tiling(g As Graphics, center As Point2D, radius As Double)
        Dim rhombuses As List(Of Rhombus) = p3Tiling.GenerateP3Tiling(center, radius, iterations)
        
        For Each rhombus As Rhombus In rhombuses
            Dim points() As PointF = {
                New PointF(CSng(rhombus.A.X), CSng(rhombus.A.Y)),
                New PointF(CSng(rhombus.B.X), CSng(rhombus.B.Y)),
                New PointF(CSng(rhombus.C.X), CSng(rhombus.C.Y)),
                New PointF(CSng(rhombus.D.X), CSng(rhombus.D.Y))
            }
            
            Dim fillColor As Color = If(rhombus.Type = 0, 
                Color.FromArgb(200, 255, 150, 150), ' lightred
                Color.FromArgb(200, 150, 150, 255)  ' lightblue
                Color.FromArgb(150, 150, 200, 200)  ' lightgreen
            )
            
            Using fillBrush As New SolidBrush(fillColor)
                g.FillPolygon(fillBrush, points)
            End Using
            
            Using borderPen As New Pen(Color.Black, 1)
                g.DrawPolygon(borderPen, points)
            End Using
        Next
    End Sub
    
    Public Property IterationCount As Integer
        Get
            Return iterations
        End Get
        Set(value As Integer)
            iterations = value
            Me.Invalidate()
        End Set
    End Property
    
    Public Sub SetTilingType(type As TilingType)
        currentTilingType = type
        Me.Invalidate()
    End Sub
End Class

Public Class PenroseDemoForm
    Inherits Form
    
    Private renderer As PenroseRenderer
    Private timer As Timer
    
    Public Sub New()
        Me.Text = "Penrose 几何可视化"
        Me.Size = New Size(1000, 800)
        Me.StartPosition = FormStartPosition.CenterScreen
        
        renderer = New PenroseRenderer()
        renderer.Dock = DockStyle.Fill
        Me.Controls.Add(renderer)
        
        AddControlPanel()
        AddAnimation()
    End Sub
    
    Private Sub AddControlPanel()
        Dim panel As New Panel()
        panel.Size = New Size(200, 150)
        panel.Location = New Point(10, 10)
        panel.BackColor = Color.FromArgb(200, 255, 255, 255)
        
        Dim lblIterations As New Label()
        lblIterations.Text = "iterations:"
        lblIterations.Location = New Point(10, 10)
        lblIterations.Size = New Size(80, 20)
        
        Dim numIterations As New NumericUpDown()
        numIterations.Value = 3
        numIterations.Minimum = 1
        numIterations.Maximum = 6
        numIterations.Location = New Point(90, 10)
        AddHandler numIterations.ValueChanged, 
            Sub(s, e) renderer.IterationCount = CInt(numIterations.Value)
        
        Dim btnP2 As New Button()
        btnP2.Text = "P2 set brick"
        btnP2.Location = New Point(10, 40)
        btnP2.Size = New Size(80, 25)
        AddHandler btnP2.Click, Sub(s, e) renderer.SetTilingType(PenroseRenderer.TilingType.P2)
        
        Dim btnP3 As New Button()
        btnP3.Text = "P3 set brick"
        btnP3.Location = New Point(100, 40)
        btnP3.Size = New Size(80, 25)
        AddHandler btnP3.Click, Sub(s, e) renderer.SetTilingType(PenroseRenderer.TilingType.P3)
        
        panel.Controls.Add(lblIterations)
        panel.Controls.Add(numIterations)
        panel.Controls.Add(btnP2)
        panel.Controls.Add(btnP3)
        Me.Controls.Add(panel)
    End Sub
    
    Private Sub AddAnimation()
        timer = New Timer()
        timer.Interval = 2000
        AddHandler timer.Tick, AddressOf Timer_Tick
        ' timer.Start() 
    End Sub
    Private Sub Timer_Tick(sender As Object, e As EventArgs)
        Static currentIteration As Integer = 1
        currentIteration = (currentIteration Mod 6) + 1
        renderer.IterationCount = currentIteration
    End Sub
End Class
