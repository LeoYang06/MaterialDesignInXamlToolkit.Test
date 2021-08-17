# MaterialDesignInXamlToolkit.Test


#### 这是一篇博客的源代码：[MaterialDesignInXamlToolkit日期选择器绑定失败解决思路](https://www.cnblogs.com/leolion/p/14958360.html)

##### 前言：

> 最初是想解答园友“[小代码大世界](https://q.cnblogs.com/u/1009224/) ”的[问题](https://q.cnblogs.com/q/135881/)，后来想举一反三，将这个问题简单剖析下，做到知其所以然。



MaterialDesignInXAML 控件库高度封装，有一些控件在使用过程中有隐晦前提条件，使用前还得多读读源码。

针对这个问题，我分步骤说明一下解决思路。

###### 1.首先解释下“无法绑定到目标方法，因其签名或安全透明度与委托类型的签名或安全透明度不兼容”异常。字面看是提示有属性绑定失败，而且提到是委托类型，由此需检查哪些属性是绑定到委托类型的。

![image-20210701112724399](https://cdn.jsdelivr.net/gh/LeoYang-Chuese/ImageHosting/cnblogs/img/image-20210701112724399.png)

DialogOpenedAttached、DialogClosingAttached是DialogHost控件的两个附件属性，官方示例是绑定到CombinedDialogOpenedEventHandler、CombinedDialogClosingEventHandler上，这两个事件处理函数是需要我们在后置代码中去定义的。我们可以在这两个事件处理函数中添加一些自定义逻辑。

```C#
public void CombinedDialogOpenedEventHandler(object sender, DialogOpenedEventArgs eventArgs)
{
    CombinedCalendar.SelectedDate = _mainViewModel.Date;
    CombinedClock.Time = _mainViewModel.Time;
}

public void CombinedDialogClosingEventHandler(object sender, DialogClosingEventArgs eventArgs)
{
    if (Convert.ToBoolean(Convert.ToUInt16(eventArgs.Parameter)) && CombinedCalendar.SelectedDate is DateTime selectedDate)
    {
        DateTime combined = selectedDate.AddSeconds(CombinedClock.Time.TimeOfDay.TotalSeconds);
        _mainViewModel.Time = combined;
        _mainViewModel.Date = combined;
    }
}
```

编译不再报错，这样最直接的问题就解决了。

###### 2.接下来还会遇到一个问题，就是弹出时间选择器对话框的按钮是禁用状态。

![image-20210701113431685](https://cdn.jsdelivr.net/gh/LeoYang-Chuese/ImageHosting/cnblogs/img/image-20210701113431685.png)

这个问题在控件GitHub主页的[wiki](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/wiki/FAQ#when-attaching-routed-command-such-as-dialoghostopendialogcommand-my-button-shows-as-disabled)中有详细解答，[issues](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/issues/2155)中也有相关问题。

翻译如下：

> 1. 第一种类型是一个简单的ICommand实现，通过视图模型（或类似的）提供。这些命令在执行时通常只是调用一个方法。尽管WPF并没有为此提供一个本地实现，但许多流行的MVVM库确实有一些ICommand接口的实现，旨在使方法调用变得简单。
> 2. 路由命令的设置是为了明确地在调用命令的元素和处理命令执行的元素之间造成分离。当一个路由命令被调用时，WPF通过可视化树（从调用命令的元素开始）寻找一个可以处理该命令的元素。如果没有找到处理程序，那么该命令将导致该按钮被禁用。在DialogHost.OpenDialogCommand的例子中，这种情况经常发生，因为在视觉树中没有找到DialogHost实例。你也可以通过使用CommandTarget属性来指定另一个命令目标，以找到RoutedCommand的处理程序。

根本原因就是“因为在视觉树中没有找到DialogHost实例”。这就是隐晦前提条件。

GitHub主页的[wiki](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/wiki/Dialogs)中介绍了打开对话框的4种方式，分别如下：

> 1. DialogHost.OpenDialogCommand
>
>    ```xaml
>    <Button Command="{x:Static md:DialogHost.OpenDialogCommand}" />
>    ```
>
>    RoutedCommand 通常从一个按钮中使用，可通过CommandParameter提供可选的内容。
>
> 2. DialogHost.IsOpen
>
>    ```xaml
>    <md:DialogHost IsOpen="True" />
>    ```
>
>    依赖属性，可以从XAML触发，从代码后台或通过绑定设置。内容必须在DialogHost.DialogContent中设置。
>
> 3. DialogHost.Show
>
>    ```C#
>    DialogHost.Show(viewOrModel);
>    ```
>
>    基于异步/等待的静态API，可以纯粹地在代码中使用（例如从视图模型中）。内容可以直接传递给对话框。注意，如果你有多个窗口和多个DialogHost实例，你可以设置DialogHost.Identifier属性，并向.Show(..)方法提供标识符，以帮助找到所需的DialogHost。
>
>    如果有多个可能的对话主机实例，在正确的窗口显示对话框的替代方法是使用扩展方法。
>
> 4. .Show() 扩展方法
>    该方法同时扩展了Window和DepedencyObject，将尝试定位最合适的DialogHost来显示对话框。

但文档还漏了一种很实用也很常见的方式，官方的示例就是这种用法。将 MainWindow 的根容器定义为 DialogHost。

```xaml
<materialDesign:DialogHost DialogTheme="Inherit"
                            Identifier="RootDialog">
    <!--Content-->
</materialDesign:DialogHost>
```

这样在视觉树中始终能找到DialogHost实例，对于实现弹框全局模态效果，这种方式值得推荐。

![image-20210701122439844](https://cdn.jsdelivr.net/gh/LeoYang-Chuese/ImageHosting/cnblogs/img/image-20210701122439844.png)

代码如下：

```xaml
<materialDesign:DialogHost DialogTheme="Inherit"
                       Identifier="RootDialog">
    <Grid>
        <StackPanel HorizontalAlignment="Center"
                VerticalAlignment="Center"
                Orientation="Horizontal">
            <TextBlock VerticalAlignment="Center"
                   FontSize="24"
                   Text="{Binding Date, StringFormat={}{0:yyyy-MM-dd HH:mm:ss}}" />
            <Button Margin="8 0 0 0"
                materialDesign:DialogHost.DialogClosingAttached="CombinedDialogClosingEventHandler"
                materialDesign:DialogHost.DialogOpenedAttached="CombinedDialogOpenedEventHandler"
                Command="{x:Static materialDesign:DialogHost.OpenDialogCommand}"
                Content="...">
                <Button.CommandParameter>
                    <Grid Margin="-1">
                        <Grid.RowDefinitions>
                            <RowDefinition Height="*" />
                            <RowDefinition Height="Auto" />
                        </Grid.RowDefinitions>
                        <StackPanel Grid.Row="0"
                                Orientation="Horizontal">
                            <Calendar x:Name="CombinedCalendar"
                                  Margin="-1 -4 -1 0" />
                            <materialDesign:Clock x:Name="CombinedClock"
                                              DisplayAutomation="CycleWithSeconds"
                                              Is24Hours="True" />
                        </StackPanel>
                        <StackPanel Grid.Row="1"
                                Margin="8"
                                HorizontalAlignment="Right"
                                Orientation="Horizontal">
                            <Button Command="{x:Static materialDesign:DialogHost.CloseDialogCommand}"
                                CommandParameter="0"
                                Content="CANCEL"
                                Style="{DynamicResource MaterialDesignFlatButton}" />
                            <Button Command="{x:Static materialDesign:DialogHost.CloseDialogCommand}"
                                CommandParameter="1"
                                Content="OK"
                                Style="{DynamicResource MaterialDesignFlatButton}" />
                        </StackPanel>
                    </Grid>
                </Button.CommandParameter>
            </Button>
        </StackPanel>
    </Grid>
</materialDesign:DialogHost>
```



###### 3.除了定义DialogHost作为根容器，还可以通过指定CommandTarget的来解决。

```xaml
<Grid>
    <StackPanel HorizontalAlignment="Center"
                VerticalAlignment="Center"
                Orientation="Horizontal">
        <TextBlock VerticalAlignment="Center"
                   FontSize="24"
                   Text="{Binding Date, StringFormat={}{0:yyyy-MM-dd HH:mm:ss}}" />
        <Button Margin="8 0 0 0"
                materialDesign:DialogHost.DialogClosingAttached="CombinedDialogClosingEventHandler"
                materialDesign:DialogHost.DialogOpenedAttached="CombinedDialogOpenedEventHandler"
                Command="{x:Static materialDesign:DialogHost.OpenDialogCommand}"
                CommandTarget="{Binding ElementName=DatePickerDialogHost}"
                Content="...">
            <Button.CommandParameter>
                <Grid Margin="-1">
                    <Grid.RowDefinitions>
                        <RowDefinition Height="*" />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>

                    <StackPanel Grid.Row="0"
                                Orientation="Horizontal">
                        <Calendar x:Name="CombinedCalendar"
                                  Margin="-1 -4 -1 0" />

                        <materialDesign:Clock x:Name="CombinedClock"
                                              DisplayAutomation="CycleWithSeconds"
                                              Is24Hours="True" />
                    </StackPanel>

                    <StackPanel Grid.Row="1"
                                Margin="8"
                                HorizontalAlignment="Right"
                                Orientation="Horizontal">
                        <Button Command="{x:Static materialDesign:DialogHost.CloseDialogCommand}"
                                CommandParameter="0"
                                Content="CANCEL"
                                Style="{DynamicResource MaterialDesignFlatButton}" />

                        <Button Command="{x:Static materialDesign:DialogHost.CloseDialogCommand}"
                                CommandParameter="1"
                                Content="OK"
                                Style="{DynamicResource MaterialDesignFlatButton}" />
                    </StackPanel>
                </Grid>
            </Button.CommandParameter>
        </Button>
    </StackPanel>

    <materialDesign:DialogHost x:Name="DatePickerDialogHost">
    </materialDesign:DialogHost>
</Grid>
```

这种方式的Content部分是通过CommandParameter传递给Command的。下面的源代码一目了然。

```C#
CommandBindings.Add(new CommandBinding(OpenDialogCommand, OpenDialogHandler));

private void OpenDialogHandler(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
{
    if (executedRoutedEventArgs.Handled) return;
    if (executedRoutedEventArgs.OriginalSource is DependencyObject dependencyObject)
    {
        _attachedDialogOpenedEventHandler = GetDialogOpenedAttached(dependencyObject);
        _attachedDialogClosingEventHandler = GetDialogClosingAttached(dependencyObject);
    }
    if (executedRoutedEventArgs.Parameter != null)
    {
        AssertTargetableContent();
        if (_popupContentControl != null)
        {
            _popupContentControl.DataContext = OpenDialogCommandDataContextSource switch
            {
                DialogHostOpenDialogCommandDataContextSource.SenderElement
                    => (executedRoutedEventArgs.OriginalSource as FrameworkElement)?.DataContext,
                DialogHostOpenDialogCommandDataContextSource.DialogHostInstance => DataContext,
                DialogHostOpenDialogCommandDataContextSource.None => null,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
        DialogContent = executedRoutedEventArgs.Parameter;
    }
    SetCurrentValue(IsOpenProperty, true);
    executedRoutedEventArgs.Handled = true;
}
```



###### 4.如果不想通过CommandParameter传递内容参数，还可以直接在DialogContent定义内容部分。这是使用Dialog控件的常见方式。

```xaml
<Grid>
    <materialDesign:DialogHost>
        <materialDesign:DialogHost.DialogContent>
            <Grid Margin="-1">
                <Grid.RowDefinitions>
                    <RowDefinition Height="*" />
                    <RowDefinition Height="Auto" />
                </Grid.RowDefinitions>

                <StackPanel Grid.Row="0"
                            Orientation="Horizontal">
                    <Calendar x:Name="CombinedCalendar"
                              Margin="-1 -4 -1 0" />

                    <materialDesign:Clock x:Name="CombinedClock"
                                          DisplayAutomation="CycleWithSeconds"
                                          Is24Hours="True" />
                </StackPanel>

                <StackPanel Grid.Row="1"
                            Margin="8"
                            HorizontalAlignment="Right"
                            Orientation="Horizontal">
                    <Button Command="{x:Static materialDesign:DialogHost.CloseDialogCommand}"
                            CommandParameter="0"
                            Content="CANCEL"
                            Style="{DynamicResource MaterialDesignFlatButton}" />

                    <Button Command="{x:Static materialDesign:DialogHost.CloseDialogCommand}"
                            CommandParameter="1"
                            Content="OK"
                            Style="{DynamicResource MaterialDesignFlatButton}" />
                </StackPanel>
            </Grid>
        </materialDesign:DialogHost.DialogContent>

        <StackPanel HorizontalAlignment="Center"
                    VerticalAlignment="Center"
                    Orientation="Horizontal">
            <TextBlock VerticalAlignment="Center"
                       FontSize="24"
                       Text="{Binding Date, StringFormat={}{0:yyyy-MM-dd HH:mm:ss}}" />
            <Button Margin="8 0 0 0"
                    materialDesign:DialogHost.DialogClosingAttached="CombinedDialogClosingEventHandler"
                    materialDesign:DialogHost.DialogOpenedAttached="CombinedDialogOpenedEventHandler"
                    Command="{x:Static materialDesign:DialogHost.OpenDialogCommand}"
                    CommandTarget="{Binding ElementName=DatePickerDialogHost}"
                    Content="..." />
        </StackPanel>
    </materialDesign:DialogHost>
</Grid>
```

