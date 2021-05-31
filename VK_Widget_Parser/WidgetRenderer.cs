using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace VK_Widget_Parser {
    public class WidgetRenderer {
        public static FrameworkElement Render(Widget widget) {
            switch (widget.Item.Type) {
                case "universal_informer":
                    return BuildInformer(widget.Item.Payload);
                case "universal_counter":
                    return BuildCounter(widget.Item.Payload);
                case "universal_table":
                    return BuildTable(widget.Item.Payload);
                case "universal_internal":
                    return BuildInternal(widget.Item.Payload);
                default:
                    return new TextBlock { Text = "Unsupported widget type." };
            }
        }

        private static FrameworkElement BuildInformer(JObject payload) {
            StackPanel container = new StackPanel();

            var rootStyle = payload["root_style"][0];

            bool needTopPadding = false;
            bool needBottomPadding = false;
            if (payload.ContainsKey("header_title") && payload.ContainsKey("header_icon")) {
                var headerTitle = payload["header_title"];
                var headerIcon = payload["header_icon"];
                container.Children.Add(BuildHeader(headerTitle, headerIcon));
            } else {
                needTopPadding = true;
            }

            if (payload.ContainsKey("rows")) {
                foreach (var row in payload["rows"]) {
                    container.Children.Add(BuildWidgetInformerRow((JObject)row, rootStyle));
                }
            }

            if (payload.ContainsKey("footer")) {
                FrameworkElement footer = BuildFooter((JObject)payload["footer"]);
                if (footer != null) {
                    container.Children.Add(BuildSeparator());
                    container.Children.Add(footer);
                } else {
                    needBottomPadding = true;
                }
            } else {
                needBottomPadding = true;
            }

            container.Padding = new Thickness(0, needTopPadding ? 6 : 0, 0, needBottomPadding ? 6 : 0);
            return WrapToActionButton(container, (JObject)payload["action"]);
        }

        private static FrameworkElement BuildCounter(JObject payload) {
            StackPanel container = new StackPanel();
            var rootStyle = payload["root_style"];

            bool needTopPadding = false;
            bool needBottomPadding = false;
            if (payload.ContainsKey("header_title") && payload.ContainsKey("header_icon")) {
                var headerTitle = payload["header_title"];
                var headerIcon = payload["header_icon"];
                container.Children.Add(BuildHeader(headerTitle, headerIcon));
            } else {
                needTopPadding = true;
            }

            if (payload.ContainsKey("items")) {
                Grid countersGrid = new Grid();

                short i = 0;
                foreach (var item in payload["items"]) {
                    countersGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    var counterItem = BuildWidgetCounterItem((JObject)item, rootStyle);
                    Grid.SetColumn(counterItem, i);
                    countersGrid.Children.Add(counterItem);
                    i++;
                }
                container.Children.Add(countersGrid);
            }

            if (payload.ContainsKey("footer")) {
                FrameworkElement footer = BuildFooter((JObject)payload["footer"]);
                if (footer != null) {
                    container.Children.Add(BuildSeparator());
                    container.Children.Add(footer);
                } else {
                    needBottomPadding = true;
                }
            } else {
                needBottomPadding = true;
            }

            container.Padding = new Thickness(0, needTopPadding ? 6 : 0, 0, needBottomPadding ? 6 : 0);
            return WrapToActionButton(container, (JObject)payload["action"]);
        }

        private static FrameworkElement BuildTable(JObject payload) {
            var styles = payload["root_style"];
            var sizes = (JArray)styles["sizes"];
            var items = (JArray)payload["items"];

            int columns = sizes.Count;
            int rows = items.Count;

            Grid grid = new Grid();

            foreach (int columnSize in sizes) {
                grid.ColumnDefinitions.Add(new ColumnDefinition() { 
                    Width = new GridLength(columnSize, GridUnitType.Star) 
                });
            }

            int row = 0;
            foreach (var rowItem in items) {
                int column = 0;
                grid.RowDefinitions.Add(new RowDefinition() {
                    Height = GridLength.Auto
                });
                foreach (var columnItem in rowItem) {
                    bool isFirstColumn = column == 0;
                    bool isLastColumn = column == columns - 1;

                    var cell = BuildTableCellElement((JObject)columnItem, styles["columns"][column]);
                    cell.Margin = new Thickness(isFirstColumn ? 12 : 4, 4, isLastColumn ? 12 : 4, 6);
                    cell = WrapToActionButton(cell, (JObject)columnItem["action"]);

                    Grid.SetColumn(cell, column);
                    Grid.SetRow(cell, row);
                    grid.Children.Add(cell);
                    column++;
                }
                row++;
            }
            return grid;
        }

        private static FrameworkElement BuildInternal(JObject payload) {
            var rootStyle = payload["root_style"];
            var headerIcon = payload["header_icon"];
            var title = payload["title"]?["value"].Value<string>();
            var subtitle = payload["subtitle"]?["value"].Value<string>();

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            string iconUrl = headerIcon.Last["url"].Value<string>();
            Border icon = new Border {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(12, 12, 0, 12),
                VerticalAlignment = VerticalAlignment.Top,
                Background = new ImageBrush() { ImageSource = new BitmapImage(new Uri(iconUrl)) },
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            StackPanel middle = new StackPanel { Margin = new Thickness(10, 13, 2, 12) };
            if (!String.IsNullOrEmpty(title)) middle.Children.Add(ParseText(title, rootStyle["title"], 16));
            if (!String.IsNullOrEmpty(subtitle)) middle.Children.Add(ParseText(subtitle, rootStyle["subtitle"], 13));
            Grid.SetColumn(middle, 1);
            grid.Children.Add(middle);

            string arrowXaml = "<ContentPresenter xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Grid.Column=\"2\" Margin=\"0,12,12,0\" VerticalAlignment=\"Top\" Width=\"24\" Height=\"24\" Foreground=\"{ThemeResource VKIconTertiaryBrush}\" ContentTemplate=\"{StaticResource Icon24Chevron}\"/>";
            ContentPresenter arrow = XamlReader.Load(arrowXaml) as ContentPresenter;
            grid.Children.Add(arrow);

            return WrapToActionButton(grid, (JObject)payload["action"]);
        }

        #region Header & footer

        private static Grid BuildHeader(JToken headerTitle, JToken headerIcon) {
            string title = headerTitle.Value<string>().ToUpper();
            string iconUrl = headerIcon.Last["url"].Value<string>().Replace("&", "&amp;");
            string xaml = $@"
                <Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" Height=""44"">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width=""Auto""/>
                        <ColumnDefinition/>
                        <ColumnDefinition Width=""Auto""/>
                    </Grid.ColumnDefinitions>
                    <Border Margin=""12,12,0,0"" VerticalAlignment=""Top"" Width=""24"" Height=""24"" CornerRadius=""6"">
                        <Border.Background>
                            <ImageBrush>
                                <ImageBrush.ImageSource>
                                    <BitmapImage UriSource=""{iconUrl}"" DecodePixelType=""Logical"" DecodePixelWidth=""24"" DecodePixelHeight=""24""/>
                                </ImageBrush.ImageSource>
                            </ImageBrush>
                        </Border.Background>
                    </Border>
                    <TextBlock Grid.Column=""1"" Margin=""10,13,8,0"" VerticalAlignment=""Top"" Style=""{{StaticResource VKCaption1}}"" Foreground=""{{ThemeResource VKTextSecondaryBrush}}"" FontWeight=""SemiBold"" Text=""{title}""/>
                    <ContentPresenter Grid.Column=""2"" Margin=""0,12,12,0"" VerticalAlignment=""Top"" Width=""24"" Height=""24"" Foreground=""{{ThemeResource VKIconTertiaryBrush}}"" ContentTemplate=""{{StaticResource Icon24Chevron}}""/>
                </Grid>
";

            Grid header = XamlReader.Load(xaml) as Grid;
            return header;
        }

        private static FrameworkElement BuildFooter(JObject footer) {
            var type = footer["type"].Value<string>();
            var payload = footer["payload"];
            if (footer["type"].Value<string>() == "accent_button") {
                string title = payload["title"]["value"].Value<string>();

                Border border = new Border();
                border.Height = 48;

                string textBlockXaml = $"<TextBlock xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Margin=\"12,-2,12,0\" VerticalAlignment=\"Center\" FontSize=\"16\" Foreground=\"{{ThemeResource VKAccentBrush}}\" Text=\"{title}\"/>";
                border.Child = XamlReader.Load(textBlockXaml) as TextBlock;
                return WrapToActionButton(border, (JObject)payload["action"]);
            } else {
                return null;
            }
        }

        private static Border BuildSeparator() {
            string xaml = "<Border xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Height=\"1\" Margin=\"12,0,12,0\" Background=\"{ThemeResource VKSeparatorAlphaBrush}\"/>";
            return XamlReader.Load(xaml) as Border;
        }

        #endregion

        #region Informer-specific

        private static FrameworkElement BuildWidgetInformerRow(JObject row, JToken rootStyle) {
            FrameworkElement leftElement = null;
            FrameworkElement middleElement = null;
            FrameworkElement rightElement = null;
            if (row.ContainsKey("left")) {
                var left = row["left"];
                leftElement = BuildWidgetInformerSideElement(left, rootStyle["left"]);
            }
            if (row.ContainsKey("middle")) {
                var middle = row["middle"];
                middleElement = BuildWidgetInformerMiddleElement((JObject)middle, rootStyle["middle"]);
            }
            if (row.ContainsKey("right")) {
                var right = row["right"];
                rightElement = BuildWidgetInformerSideElement(right, rootStyle["right"]);
            }

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            if (leftElement != null) {
                Grid.SetColumn(leftElement, 0);
                grid.Children.Add(leftElement);
            }
            if (middleElement != null) {
                Grid.SetColumn(middleElement, 1);
                grid.Children.Add(middleElement);
            }
            if (rightElement != null) {
                Grid.SetColumn(rightElement, 2);
                grid.Children.Add(rightElement);
            }
            return WrapToActionButton(grid, (JObject)row["action"]);
        }

        private static FrameworkElement BuildWidgetInformerSideElement(JToken element, JToken style) {
            string type = element["type"].Value<string>();
            var payload = element["payload"];
            var elementStyle = style[type];

            switch (type) {
                case "icon":
                    string iconUrl = payload["items"].Last["url"].Value<string>();
                    return new Image {
                        Width = 24,
                        Height = 24,
                        VerticalAlignment = ParseVerticalAlignment(elementStyle["vertical_align"]),
                        Source = new BitmapImage(new Uri(iconUrl)),
                        Margin = new Thickness(12, 6, 0, 6)
                    };
                case "image":
                    string imageUrl = payload["items"].Last["url"].Value<string>();
                    return new Border() {
                        CornerRadius = new CornerRadius(6),
                        Width = ParseImageSize(style["size"]),
                        Height = ParseImageSize(style["size"]),
                        VerticalAlignment = ParseVerticalAlignment(elementStyle["vertical_align"]),
                        Background = new ImageBrush() { ImageSource = new BitmapImage(new Uri(imageUrl)) },
                        Margin = new Thickness(12, 6, 0, 6),
                    };
                case "counter":
                    TextBlock counter = ParseText(payload["value"].Value<string>(), elementStyle, 24);
                    counter.Margin = new Thickness(12);
                    return counter;
            }
            return new Border { Width = 0, Height = 0 };
        }

        private static FrameworkElement BuildWidgetInformerMiddleElement(JObject middle, JToken style) {
            string title = String.Empty, subtitle = String.Empty, secondSubtitle = String.Empty;
            if (middle.ContainsKey("title")) title = middle["title"]["value"].Value<string>();
            if (middle.ContainsKey("subtitle")) subtitle = middle["subtitle"]["value"].Value<string>();
            if (middle.ContainsKey("second_subtitle")) secondSubtitle = middle["second_subtitle"]["value"].Value<string>();

            StackPanel middleElement = new StackPanel() { Margin = new Thickness(12, 6, 12, 6) };
            middleElement.VerticalAlignment = ParseVerticalAlignment(style["vertical_align"], VerticalAlignment.Center);
            if (!String.IsNullOrEmpty(title)) middleElement.Children.Add(ParseText(title, style["title"], 16));
            if (!String.IsNullOrEmpty(subtitle)) middleElement.Children.Add(ParseText(subtitle, style["subtitle"], 13));
            if (!String.IsNullOrEmpty(secondSubtitle)) middleElement.Children.Add(ParseText(secondSubtitle, style["second_subtitle"], 13));
            return middleElement;
        }

        #endregion

        #region Counter-specific

        private static FrameworkElement BuildWidgetCounterItem(JObject item, JToken style) {
            StackPanel panel = new StackPanel() { Margin = new Thickness(12, 0, 12, 6) };
            string counter = String.Empty, title = String.Empty, subtitle = String.Empty;
            if (item.ContainsKey("counter")) counter = item["counter"]["value"].Value<string>();
            if (item.ContainsKey("title")) title = item["title"]["value"].Value<string>();
            if (item.ContainsKey("subtitle")) subtitle = item["subtitle"]["value"].Value<string>();

            if (!String.IsNullOrEmpty(counter)) panel.Children.Add(ParseText(counter, style["counter"], 32, "light"));
            if (!String.IsNullOrEmpty(title)) panel.Children.Add(ParseText(title, style["title"], 16));
            if (!String.IsNullOrEmpty(subtitle)) panel.Children.Add(ParseText(subtitle, style["subtitle"], 13));
            return WrapToActionButton(panel, (JObject)item["action"]);
        }

        #endregion

        #region Table-specific

        private static FrameworkElement BuildTableCellElement(JObject item, JToken style) {
            string title = String.Empty, subtitle = String.Empty;
            if (item.ContainsKey("title")) title = item["title"]["value"].Value<string>();
            if (item.ContainsKey("subtitle")) subtitle = item["subtitle"]["value"].Value<string>();

            StackPanel cell = new StackPanel();
            cell.VerticalAlignment = ParseVerticalAlignment(style["vertical_align"], VerticalAlignment.Center);
            if (!String.IsNullOrEmpty(title)) {
                TextBlock ttb = ParseText(title, style["title"], 16);
                ttb.TextAlignment = ParseTextAlignment(style["align"]);
                cell.Children.Add(ttb);
            }
            if (!String.IsNullOrEmpty(subtitle)) {
                TextBlock stb = ParseText(subtitle, style["subtitle"], 13);
                stb.TextAlignment = ParseTextAlignment(style["align"]);
                cell.Children.Add(stb);
            }
            return cell;
        }

        #endregion

        private static FrameworkElement WrapToActionButton(FrameworkElement element, JObject action) {
            if (action != null && action["type"].Value<string>() == "open_url") {
                Button button = new Button {
                    Style = Application.Current.Resources["VKButtonTertiaryMedium"] as Style,
                    Padding = new Thickness(0),
                    HorizontalAlignment = element.HorizontalAlignment,
                    VerticalAlignment = element.VerticalAlignment,
                    HorizontalContentAlignment = element.HorizontalAlignment,
                    VerticalContentAlignment = element.VerticalAlignment,
                    Content = element
                };
                button.Click += async (a, b) => {
                    string url = action["url"].Value<string>();
                    await new MessageDialog(url, "Open url").ShowAsync();
                };
                return button;
            } else {
                return element;
            }
        }

        private static TextBlock ParseText(string value, JToken elementStyle, double defaultSize, string defaultWeight = "regular") {
            string color = ParseColor(elementStyle?["color"]);
            string weight = ParseWeight(elementStyle?["weight"] ?? defaultWeight);
            string size = ParseTextSize(elementStyle?["size"]);
            if (String.IsNullOrEmpty(size)) size = defaultSize.ToString();

            string xaml = $@"
                <TextBlock xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" TextWrapping=""Wrap"" MaxLines=""5"" FontSize=""{size}"" Foreground=""{color}"" FontWeight=""{weight}"" VerticalAlignment=""Center"">{ParseStyledTextForXaml(value)}</TextBlock>
";
            TextBlock tb = XamlReader.Load(xaml) as TextBlock;
            return tb;
        }

        private static TextAlignment ParseTextAlignment(JToken alignment, TextAlignment defaultAlignment = TextAlignment.Left) {
            if (alignment == null) return defaultAlignment;
            switch (alignment.Value<string>()) {
                case "left": return TextAlignment.Left;
                case "center": return TextAlignment.Center;
                case "right": return TextAlignment.Right;
                default: return defaultAlignment;
            }
        }

        private static VerticalAlignment ParseVerticalAlignment(JToken alignment, VerticalAlignment defaultAlignment = VerticalAlignment.Stretch) {
            if (alignment == null) return defaultAlignment;
            switch (alignment.Value<string>()) {
                case "top": return VerticalAlignment.Top;
                case "center": return VerticalAlignment.Center;
                case "bottom": return VerticalAlignment.Bottom;
                default: return defaultAlignment;
            }
        }

        private static string ParseColor(JToken color) {
            return ParseNamedColor(color?.Value<string>());
        }

        private static string ParseWeight(JToken weight) {
            if (weight == null) return "Normal";
            switch (weight.Value<string>()) {
                case "light": return "Light";
                case "medium": return "Medium";
                case "semi_bold": return "SemiBold";
                default: return "Normal";
            }
        }

        private static string ParseTextSize(JToken size) {
            if (size == null) return null;
            switch (size.Value<string>()) {
                case "small": return "12";
                case "large": return "24";
                default: return "16";
            }
        }

        private static double ParseImageSize(JToken size) {
            if (size == null) return 56;
            switch (size.Value<string>()) {
                case "small": return 48;
                case "large": return 88;
                default: return 56;
            }
        }

        #region Text style parser

        static Regex styleRegex = new Regex(@"\[style[\s\S]*?\]([\s\S]*?)\[\/style\]");
        static Regex propsRegex = new Regex("(font-weight\\s*=\\s*\"(\\w+)\"|color\\s*=\\s*\"(\\w+|#(?:[0-9a-fA-F]{3,8}))\")");

        private static string ParseStyledTextForXaml(string plain) {
            var matches = styleRegex.Matches(plain);

            foreach (Match match in matches) {
                plain = plain.Replace(match.Value, ParseStyle(match));
            }
            plain = plain.Replace("\n", "<LineBreak/>");
            return plain;
        }

        private static string ParseStyle(Match match) {
            string text = match.Groups[1].Value;
            var props = propsRegex.Matches(match.Value);
            List<string> propsXaml = new List<string>();

            foreach (Match prop in props) {
                string[] split = prop.Value.Split('=');
                string key = split[0];
                string value = split[1].Substring(1, split[1].Length - 2);

                switch (key) {
                    case "font-weight":
                        propsXaml.Add(ParseFontWeight(value));
                        break;
                    case "color":
                        propsXaml.Add(ParseColor(value));
                        break;
                }
            }

            return $"<Run {String.Join(" ", propsXaml)}>{text}</Run>";
        }

        private static string ParseFontWeight(string weight) {
            string xweight = "Normal";
            switch (weight) {
                case "medium":
                    xweight = "Medium";
                    break;
            }
            return $"FontWeight=\"{xweight}\"";
        }

        private static string ParseColor(string color) {
            if (String.IsNullOrEmpty(color) || !color.StartsWith("#")) {
                return $"Foreground=\"{ParseNamedColor(color)}\"";
            }
            return $"Foreground=\"{color}\"";
        }

        #endregion

        private static string ParseNamedColor(string namedColor) {
            if (namedColor == null) return "{ThemeResource VKTextPrimaryBrush}";
            switch (namedColor) {
                case "secondary": return "{ThemeResource VKTextSecondaryBrush}";
                case "accent": return "{ThemeResource VKAccentBrush}";
                case "dynamic_blue": return "{ThemeResource VKDynamicBlueBrush}";
                case "dynamic_gray": return "{ThemeResource VKDynamicGrayBrush}";
                case "dynamic_red": return "{ThemeResource VKDynamicRedBrush}";
                case "dynamic_green": return "{ThemeResource VKDynamicGreenBrush}";
                case "dynamic_orange": return "{ThemeResource VKDynamicOrangeBrush}";
                case "dynamic_violet": return "{ThemeResource VKDynamicVioletBrush}";
                default: return "{ThemeResource VKTextPrimaryBrush}";
            }
        }
    }
}