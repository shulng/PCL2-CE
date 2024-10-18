﻿Public Class PageDownloadCompFavorites

#Region "加载器信息"
    '加载器信息
    Public Shared Loader As New LoaderTask(Of List(Of String), List(Of CompProject))("CompProject Favorites", AddressOf CompFavoritesGet, AddressOf LoaderInput)

    Private Sub PageDownloadCompFavorites_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanContent, Nothing, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput)
    End Sub
    Private Sub PageDownloadCompFavorites_Loaded(sender As Object, e As EventArgs) Handles Me.Loaded
        SelectedItemList.Clear()
        RefreshBar()
        If Loader.Input IsNot Nothing AndAlso Not Loader.Input.Equals(CompFavorites.GetAll()) Then
            Loader.Start()
        End If
    End Sub

    Private Shared Function LoaderInput() As List(Of String)
        Return CompFavorites.GetAll().Clone() '复制而不是直接引用！
    End Function
    Private Shared Sub CompFavoritesGet(Task As LoaderTask(Of List(Of String), List(Of CompProject)))
        Task.Output = CompFavorites.GetAllCompProjects(Task.Input)
    End Sub
#End Region

    Private CompItemList As New List(Of MyMiniCompItem)
    Private SelectedItemList As New List(Of MyMiniCompItem)

#Region "UI 化"
    '结果 UI 化
    Private Sub Load_OnFinish()
        Try
            RemoveHandler PanSearchBox.TextChanged, AddressOf SearchRun
            PanSearchBox.Text = String.Empty
            AddHandler PanSearchBox.TextChanged, AddressOf SearchRun
            CompItemList.Clear()
            HintGetFail.Visibility = If(Loader.Input.Count = Loader.Output.Count, Visibility.Collapsed, Visibility.Visible)
            For Each item In Loader.Output
                Dim CompItem = item.ToMiniCompItem()

                '----添加按钮----
                '删除按钮
                Dim Btn_Delete As New MyIconButton
                Btn_Delete.Logo = Logo.IconButtonLikeFill
                Btn_Delete.ToolTip = "取消收藏"
                ToolTipService.SetPlacement(Btn_Delete, Primitives.PlacementMode.Center)
                ToolTipService.SetVerticalOffset(Btn_Delete, 30)
                ToolTipService.SetHorizontalOffset(Btn_Delete, 2)
                AddHandler Btn_Delete.Click, Sub(sender As Object, e As EventArgs)
                                                 Items_CancelFavorites(CompItem)
                                                 RefreshContent()
                                                 RefreshCardTitle()
                                                 RefreshBar()
                                             End Sub
                CompItem.Buttons = {Btn_Delete}
                '---操作逻辑---
                '右键查看详细信息界面
                AddHandler CompItem.MouseRightButtonUp, Sub(sender As Object, e As EventArgs)
                                                            FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.CompDetail,
                   .Additional = {CompItem.Entry, New List(Of String), String.Empty, CompModLoaderType.Any}})
                                                        End Sub
                '---其它事件---
                AddHandler CompItem.Changed, AddressOf ItemCheckStatusChanged
                CompItemList.Add(CompItem)
            Next
            If CompItemList.Any() Then '有收藏
                If Not IsSearching Then
                    PanSearchBox.Visibility = Visibility.Visible
                    CardProjectsMod.Visibility = Visibility.Visible
                    CardProjectsModpack.Visibility = Visibility.Visible
                    CardNoContent.Visibility = Visibility.Collapsed
                End If
            Else '没有收藏
                PanSearchBox.Visibility = Visibility.Collapsed
                CardProjectsMod.Visibility = Visibility.Collapsed
                CardProjectsModpack.Visibility = Visibility.Collapsed
                CardNoContent.Visibility = Visibility.Visible
            End If

            RefreshCardTitle()
            RefreshContent()
        Catch ex As Exception
            Log(ex, "可视化收藏夹列表出错", LogLevel.Feedback)
        End Try
    End Sub

    Private Sub RefreshContent()
        PanProjectsMod.Children.Clear()
        PanProjectsModpack.Children.Clear()
        Dim DataSource As List(Of MyMiniCompItem) = If(IsSearching, SearchResult, CompItemList)
        For Each item As MyMiniCompItem In DataSource
            If IsSearching Then
                CardProjectsMod.Visibility = Visibility.Visible
                CardProjectsModpack.Visibility = Visibility.Collapsed
                PanProjectsMod.Children.Add(item)
                Continue For
            Else
                CardProjectsModpack.Visibility = Visibility.Visible
                CardProjectsMod.Visibility = Visibility.Visible
            End If
            If item.Entry.Type = CompType.Mod Then
                PanProjectsMod.Children.Add(item)
            ElseIf item.Entry.Type = CompType.ModPack Then
                PanProjectsModpack.Children.Add(item)
            Else
                Log("[Favorites] 未知工程类型：" & item.Entry.Type)
            End If
        Next
    End Sub

    Private Sub RefreshCardTitle()
        If IsSearching Then
            CardProjectsMod.Title = $"搜索结果 ({SearchResult.Count})"
        Else
            Dim ModRes As Integer = 0
            Dim ModpackRes As Integer = 0
            ModRes = CompItemList.Where(Function(e) e.Entry.Type.Equals(CompType.Mod)).Count()
            CardProjectsMod.Title = $"Mod ({ModRes})"
            ModpackRes = CompItemList.Where(Function(e) e.Entry.Type.Equals(CompType.ModPack)).Count()
            CardProjectsModpack.Title = $"整合包 ({ModpackRes})"
        End If
    End Sub

    Private BottomBarShownCount As Integer = 0

    Private Sub RefreshBar()
        Dim NewCount As Integer = SelectedItemList.Count
        Dim Selected = NewCount > 0
        If Selected Then LabSelect.Text = $"已选择 {NewCount} 个收藏项目" '取消所有选择时不更新数字
        '更新显示状态
        If AniControlEnabled = 0 Then
            PanContentList.Margin = New Thickness(0, 0, 0, If(Selected, 80, 0))
            If Selected Then
                '仅在数量增加时播放出现/跳跃动画
                If BottomBarShownCount >= NewCount Then
                    BottomBarShownCount = NewCount
                    Return
                Else
                    BottomBarShownCount = NewCount
                End If
                '出现/跳跃动画
                CardSelect.Visibility = Visibility.Visible
                AniStart({
                    AaOpacity(CardSelect, 1 - CardSelect.Opacity, 60),
                    AaTranslateY(CardSelect, -27 - TransSelect.Y, 120, Ease:=New AniEaseOutFluent(AniEasePower.Weak)),
                    AaTranslateY(CardSelect, 3, 150, 120, Ease:=New AniEaseInoutFluent(AniEasePower.Weak)),
                    AaTranslateY(CardSelect, -1, 90, 270, Ease:=New AniEaseInoutFluent(AniEasePower.Weak))
                }, "CompFavorites Sidebar")
            Else
                '不重复播放隐藏动画
                If BottomBarShownCount = 0 Then Return
                BottomBarShownCount = 0
                '隐藏动画
                AniStart({
                    AaOpacity(CardSelect, -CardSelect.Opacity, 90),
                    AaTranslateY(CardSelect, -10 - TransSelect.Y, 90, Ease:=New AniEaseInFluent(AniEasePower.Weak)),
                    AaCode(Sub() CardSelect.Visibility = Visibility.Collapsed, After:=True)
                }, "CompFavorites Sidebar")
            End If
        Else
            AniStop("CompFavorites Sidebar")
            BottomBarShownCount = NewCount
            If Selected Then
                CardSelect.Visibility = Visibility.Visible
                CardSelect.Opacity = 1
                TransSelect.Y = -25
            Else
                CardSelect.Visibility = Visibility.Collapsed
                CardSelect.Opacity = 0
                TransSelect.Y = -10
            End If
        End If
    End Sub

#End Region

    '选中状态改变
    Private Sub ItemCheckStatusChanged(sender As Object, e As RouteEventArgs)
        Dim SenderItem As MyMiniCompItem = sender
        If SelectedItemList.Contains(SenderItem) Then SelectedItemList.Remove(SenderItem)
        If SenderItem.Checked Then SelectedItemList.Add(SenderItem)
        RefreshBar()
    End Sub

    '自动重试
    Private Sub Load_State(sender As Object, state As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Handles Load.StateChanged
        Select Case Loader.State
            Case LoadState.Failed
                Dim ErrorMessage As String = ""
                If Loader.Error IsNot Nothing Then ErrorMessage = Loader.Error.Message
                If ErrorMessage.Contains("不是有效的 json 文件") Then
                    Log("[Download] 下载的工程列表 JSON 文件损坏，已自动重试", LogLevel.Debug)
                    PageLoaderRestart()
                End If
        End Select
    End Sub

    Private Sub Btn_FavoritesCancel_Clicked(sender As Object, e As RouteEventArgs) Handles Btn_FavoritesCancel.Click
        For Each Items In SelectedItemList.Clone()
            Items_CancelFavorites(Items)
        Next
        If CompItemList.Any Then
            RefreshContent()
            RefreshCardTitle()
        Else
            Loader.Start()
        End If
        RefreshBar()
    End Sub

    Private Sub Btn_SelectCancel_Clicked(sender As Object, e As RouteEventArgs) Handles Btn_SelectCancel.Click
        SelectedItemList.Clear()
        Items_SetSelectAll(False)
    End Sub

    Private Sub Items_SetSelectAll(TargetStatus As Boolean)
        For Each item As MyMiniCompItem In PanProjectsMod.Children
            item.Checked = TargetStatus
        Next
        For Each item As MyMiniCompItem In PanProjectsModpack.Children
            item.Checked = TargetStatus
        Next
        SelectedItemList = CompItemList.Where(Function(e) e.Checked).ToList()
    End Sub

    Private Sub Items_CancelFavorites(Item As MyMiniCompItem)
        CompItemList.Remove(Item)
        If SelectedItemList.Contains(Item) Then SelectedItemList.Remove(Item)
        If SearchResult.Contains(Item) Then SearchResult.Remove(Item)
        CompFavorites.Del(Item.Entry.Id)
    End Sub

    Private Sub Page_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If My.Computer.Keyboard.CtrlKeyDown AndAlso e.Key = Key.A Then Items_SetSelectAll(True)
    End Sub

#Region "搜索"

    Private ReadOnly Property IsSearching As Boolean
        Get
            Return Not String.IsNullOrWhiteSpace(PanSearchBox.Text)
        End Get
    End Property

    Private SearchResult As New List(Of MyMiniCompItem)
    Public Sub SearchRun()
        If IsSearching Then
            '构造请求
            Dim QueryList As New List(Of SearchEntry(Of MyMiniCompItem))
            For Each Entry As MyMiniCompItem In CompItemList
                Dim SearchSource As New List(Of KeyValuePair(Of String, Double))
                SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Entry.RawName, 1))
                If Entry.Description IsNot Nothing AndAlso Entry.Description <> "" Then
                    SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Description, 0.4))
                End If
                If Entry.Entry.TranslatedName <> Entry.Entry.RawName Then SearchSource.Add(New KeyValuePair(Of String, Double)(Entry.Entry.TranslatedName, 1))
                SearchSource.Add(New KeyValuePair(Of String, Double)(String.Join("", Entry.Entry.Tags), 0.2))
                QueryList.Add(New SearchEntry(Of MyMiniCompItem) With {.Item = Entry, .SearchSource = SearchSource})
            Next
            '进行搜索
            SearchResult = Search(QueryList, PanSearchBox.Text, MaxBlurCount:=6, MinBlurSimilarity:=0.35).Select(Function(r) r.Item).ToList
        End If
        RefreshContent()
        RefreshCardTitle()
    End Sub

#End Region

End Class
