using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVSTests.TestInfrastructure;

namespace MVSTests.Helpers
{
    [TestClass]
    public class NavigationThemeTests
    {
        [TestMethod]
        public void Navigation_UsesLightWorkspaceAndRetainsSelectedContent()
        {
            StaTestHelper.Run(() =>
            {
                var resources = LoadTheme();
                var first = new TabItem { Header = "First", Content = new TextBlock { Text = "First content" } };
                var second = new TabItem { Header = "Second", Content = new TextBlock { Text = "Second content" } };
                first.Style = (Style)resources["SesNativeNavigationTabItemStyle"];
                second.Style = (Style)resources["SesNativeNavigationTabItemStyle"];
                var tabs = new TabControl
                {
                    Style = (Style)resources["SesNativeNavigationTabControlStyle"],
                    TabStripPlacement = Dock.Left
                };
                tabs.Resources.MergedDictionaries.Add(resources);
                tabs.Items.Add(first);
                tabs.Items.Add(second);
                Arrange(tabs);

                var workspace = (Border)tabs.Template.FindName("WorkspaceBorder", tabs);
                Assert.AreEqual(
                    ((SolidColorBrush)resources["SesSurfaceBrush"]).Color,
                    ((SolidColorBrush)workspace.Background).Color);
                Assert.AreSame(first.Content, tabs.SelectedContent);
                Assert.AreEqual(
                    ((SolidColorBrush)resources["SesTextPrimaryBrush"]).Color,
                    ((SolidColorBrush)((TextBlock)first.Content).Foreground).Color);
                var firstHeader = Descendants(tabs).OfType<TextBlock>().Single(element => element.Text == "First");
                var secondHeader = Descendants(tabs).OfType<TextBlock>().Single(element => element.Text == "Second");
                var firstBorder = (Border)first.Template.FindName("NavigationBorder", first);
                var secondBorder = (Border)second.Template.FindName("NavigationBorder", second);
                var firstPosition = firstBorder.TranslatePoint(new Point(), tabs);
                var secondPosition = secondBorder.TranslatePoint(new Point(), tabs);
                Assert.IsTrue(firstBorder.ActualHeight >= 42);
                var gap = secondPosition.Y - firstPosition.Y - firstBorder.ActualHeight;
                Assert.IsTrue(gap >= 5.9, $"Navigation gap was {gap} DIP; first {firstPosition}, height {firstBorder.ActualHeight}, second {secondPosition}, margin {first.Margin}.");
                Assert.AreEqual(
                    ((SolidColorBrush)resources["SesWhiteBrush"]).Color,
                    ((SolidColorBrush)firstHeader.Foreground).Color);
                Assert.AreEqual(FontWeights.Normal, ((TextBlock)first.Content).FontWeight);
                Assert.AreEqual(
                    ((SolidColorBrush)resources["SesWhiteBrush"]).Color,
                    ((SolidColorBrush)secondHeader.Foreground).Color);

                tabs.SelectedIndex = 1;
                Arrange(tabs);
                Assert.AreSame(second.Content, tabs.SelectedContent);
                Assert.AreEqual(
                    ((SolidColorBrush)resources["SesWhiteBrush"]).Color,
                    ((SolidColorBrush)secondHeader.Foreground).Color);
                Assert.AreEqual(FontWeights.Normal, ((TextBlock)second.Content).FontWeight);
            });
        }

        [TestMethod]
        public void Navigation_RailRespondsToMenuLabelVisibility()
        {
            StaTestHelper.Run(() =>
            {
                var resources = LoadTheme();
                var tabs = new TabControl
                {
                    Style = (Style)resources["SesNativeNavigationTabControlStyle"],
                    TabStripPlacement = Dock.Left
                };
                tabs.Resources.MergedDictionaries.Add(resources);
                tabs.Items.Add(new TabItem { Header = "First", Content = "Content" });

                var menuState = MVS.MenuStateVM.Instance;
                var original = menuState.showMenuLabels;
                try
                {
                    menuState.showMenuLabels = true;
                    Arrange(tabs);
                    var rail = (Border)tabs.Template.FindName("NavigationRail", tabs);
                    Assert.AreEqual(288d, rail.Width);

                    menuState.showMenuLabels = false;
                    Arrange(tabs);
                    Assert.AreEqual(80d, rail.Width);
                }
                finally
                {
                    menuState.showMenuLabels = original;
                }
            });
        }

        [TestMethod]
        public void ProjectSetup_CanScrollToDetailsInNarrowWorkspace()
        {
            StaTestHelper.Run(() =>
            {
                LoadTheme();
                var projects = new MVS.Projects();
                var pdf = (FrameworkElement)projects.FindName("inlinePdfViewer");
                ((Panel)pdf.Parent).Children.Remove(pdf);
                var grid = (DataGrid)projects.FindName("gvProjects");
                grid.ItemsSource = new[]
                {
                    new { Id = 1, Name = "Example", DateString = "2026-01-15", DataStatus = "Example", projectStatus = "OFF" }
                };

                projects.Measure(new Size(480, 600));
                projects.Arrange(new Rect(0, 0, 480, 600));
                projects.UpdateLayout();

                var scroll = (ScrollViewer)projects.FindName("setupHorizontalScroll");
                Assert.IsTrue(scroll.ScrollableWidth > 0);
                var projectName = (FrameworkElement)projects.FindName("tbDataSetName");
                Assert.IsTrue(projectName.ActualWidth >= 200);
                Assert.IsTrue(grid.Columns[1].ActualWidth >= 80);
                var row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
                Assert.IsNotNull(row);
                Assert.AreEqual(36d, row.ActualHeight);
                var cell = Descendants(row).OfType<DataGridCell>().First();
                Assert.AreEqual(VerticalAlignment.Center, cell.VerticalContentAlignment);
                foreach (var column in grid.Columns.OfType<DataGridTextColumn>())
                {
                    var text = (TextBlock)column.GetCellContent(row);
                    Assert.IsNotNull(text);
                    var top = text.TranslatePoint(new Point(), row).Y;
                    Assert.AreEqual(row.ActualHeight / 2, top + text.ActualHeight / 2, 1.5,
                        $"{column.Header} text should be vertically centered in the project row.");
                }

                projects.Measure(new Size(1000, 600));
                projects.Arrange(new Rect(0, 0, 1000, 600));
                projects.UpdateLayout();
                Assert.AreEqual(0d, scroll.ScrollableWidth);
                Assert.IsTrue(grid.Columns[1].ActualWidth >= 80);

                var step = (FrameworkElement)projects.FindName("panelStep1");
                step.Visibility = Visibility.Collapsed;
                projects.UpdateLayout();
                Assert.AreEqual(Visibility.Collapsed, scroll.Visibility);
            });
        }

        [TestMethod]
        public void SubTab_ContentDoesNotInheritSelectedHeaderColor()
        {
            StaTestHelper.Run(() =>
            {
                var resources = LoadTheme();
                var content = new TextBlock { Text = "Content" };
                var item = new TabItem
                {
                    Header = "Status",
                    Content = content,
                    Style = (Style)resources["SesNativeSubTabItemStyle"]
                };
                var tabs = new TabControl();
                tabs.Resources.MergedDictionaries.Add(resources);
                tabs.Items.Add(item);
                Arrange(tabs);

                Assert.AreEqual(
                    ((SolidColorBrush)resources["SesTextPrimaryBrush"]).Color,
                    ((SolidColorBrush)content.Foreground).Color);
                Assert.AreEqual(FontWeights.Normal, content.FontWeight);
            });
        }

        [TestMethod]
        public void SecondaryButton_ContentInheritsReadableForeground()
        {
            StaTestHelper.Run(() =>
            {
                var resources = LoadTheme();
                var button = new Button
                {
                    Content = "Action",
                    Style = (Style)resources["SesNativeSecondaryButtonStyle"]
                };
                button.Resources.MergedDictionaries.Add(resources);
                Arrange(button);

                var text = Descendants(button).OfType<TextBlock>().Single(element => element.Text == "Action");
                Assert.AreEqual(
                    ((SolidColorBrush)resources["SesWhiteBrush"]).Color,
                    ((SolidColorBrush)text.Foreground).Color);
            });
        }

        private static ResourceDictionary LoadTheme()
        {
            if (Application.Current == null)
            {
                var application = new MVS.App();
                application.InitializeComponent();
            }

            return new ResourceDictionary
            {
                Source = new Uri("/MVS;component/Themes/SesBrand.xaml", UriKind.Relative)
            };
        }

        private static void Arrange(FrameworkElement element)
        {
            element.Measure(new Size(800, 600));
            element.Arrange(new Rect(0, 0, 800, 600));
            element.UpdateLayout();
        }

        private static System.Collections.Generic.IEnumerable<DependencyObject> Descendants(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                yield return child;
                foreach (var descendant in Descendants(child))
                    yield return descendant;
            }
        }
    }
}
